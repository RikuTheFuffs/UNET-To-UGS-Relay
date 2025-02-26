using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;
using Unity.Samples.Multiplayer.UNET.Runtime;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(NetworkManager))]
public class CustomConnectUI : MonoBehaviour
{
    static class LobbyKeys
    {
        public const string RelayJoinCode = "relay.joinCode";
        public const string RelayHost = "relay.host";
    }

    public NetworkManager manager;

    public string ConnectionType = "dtls";
    public int MaxPlayers = 50;
    public string RelayJoinCode;

    const int k_HeartbeatIntervalSeconds = 10;

    Lobby m_CurrentLobby;
    string m_LobbyName = "2dshooter";
    Coroutine m_Heartbeat;

    void Awake()
    {
        manager = GetComponent<NetworkManager>();
    }

    void OnGUI()
    {
        bool noConnection = (manager.client == null || manager.client.connection == null ||
            manager.client.connection.connectionId == -1);

        if (!manager.IsClientConnected() && !NetworkServer.active)
        {
            if (noConnection)
            {
                if (GUILayout.Button("LAN Host(H)"))
                {
                    StartHostWithRelay();
                }
                if (GUILayout.Button("LAN Client(C)"))
                {
                    StartClientWithRelay();
                }

                manager.networkAddress = GUILayout.TextField(manager.networkAddress);
                if (GUILayout.Button("LAN Server Only(S)"))
                {
                    manager.StartServer();
                }
            }
            else
            {
                GUILayout.Label("Connecting to " + manager.networkAddress + ":" + manager.networkPort + "..");
                if (GUILayout.Button("Cancel Connection Attempt"))
                {
                    manager.StopClient();
                }
            }
        }
        else
        {
            if (NetworkServer.active)
            {
                string serverMsg = "Server: port=" + manager.networkPort;
                if (manager.useWebSockets)
                {
                    serverMsg += " (Using WebSockets)";
                }
                GUILayout.Label(serverMsg);
            }
            if (manager.IsClientConnected())
            {
                GUILayout.Label("Client: address=" + manager.networkAddress + " port=" + manager.networkPort);
            }
        }

        if (manager.IsClientConnected() && !ClientScene.ready)
        {
            if (GUILayout.Button("Client Ready"))
            {
                ClientScene.Ready(manager.client.connection);

                if (ClientScene.localPlayers.Count == 0)
                {
                    ClientScene.AddPlayer(0);
                }
            }
        }
        if (NetworkServer.active || manager.IsClientConnected())
        {
            if (GUILayout.Button("Stop (X)"))
            {
                manager.StopHost();
            }
        }
    }

    IEnumerator HeartbeatLobbyCoroutine()
    {
        while (true)
        {
            if (m_CurrentLobby == null)
            {
                yield return null;
            }

            try
            {
                LobbyService.Instance.SendHeartbeatPingAsync(m_CurrentLobby.Id);
            }
            catch (LobbyServiceException ex)
            {
                if (ex.Reason == LobbyExceptionReason.RateLimited)
                {
                    Debug.LogWarning($"Hit lobby heartbeat rate limit, will try again in {k_HeartbeatIntervalSeconds} seconds.");
                }
                else
                {
                    Debug.LogError($"Lobby exception while sending heartbeat: {ex.Message}.");
                }
            }
            yield return new WaitForSecondsRealtime(k_HeartbeatIntervalSeconds);
        }
    }

    async void StartHostWithRelay()
    {
        await UnityServicesInitializer.Instance.Initialize(false);
        Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        Debug.Log($"Created allocation {allocation.AllocationId} with join code {joinCode}");

        // Start hosting via the given relay allocation
        var unityTransport = (UnityTransport)NetworkManager.activeTransport;
        unityTransport.UseRelay = true;
        unityTransport.RelayServerData = allocation.ToRelayServerData(ConnectionType);
        manager.StartHost();

        Debug.Log($"Creating lobby with name {m_LobbyName}");
        await CreateLobbyAsync(joinCode, allocation.AllocationId.ToString());
        await SubscribeToLobbyEvents();
    }

    async void StartClientWithRelay()
    {
        await UnityServicesInitializer.Instance.Initialize(false);
        Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");

        await JoinLobbyByNameAsync(m_LobbyName);
        await SubscribeToLobbyEvents();

        if (m_CurrentLobby != null && m_CurrentLobby.Players != null)
        {
            var relayJoinCode = m_CurrentLobby.Data[LobbyKeys.RelayJoinCode].Value;
            Debug.Log($"Using relay join code {relayJoinCode} from lobby data");
            RelayJoinCode = relayJoinCode;
            var allocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            // Start hosting via the given relay allocation
            var unityTransport = (UnityTransport)NetworkManager.activeTransport;
            unityTransport.UseRelay = true;
            unityTransport.RelayServerData = allocation.ToRelayServerData(ConnectionType);
            manager.StartClient();

            await UpdatePlayerAllocationId(allocation.AllocationId);
        }
    }

    public async Task CreateLobbyAsync(string joinCode, string allocationId)
    {
        RelayJoinCode = joinCode;
        var playerId = AuthenticationService.Instance.PlayerId;

        CreateLobbyOptions options = new CreateLobbyOptions();
        options.Data = new Dictionary<string, DataObject>()
        {
            {LobbyKeys.RelayHost, new DataObject(DataObject.VisibilityOptions.Member, playerId)},
            {LobbyKeys.RelayJoinCode, new DataObject(DataObject.VisibilityOptions.Member, joinCode)}
        };
        options.Player = new Player(id: playerId, allocationId: allocationId);

        m_CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(m_LobbyName, MaxPlayers, options);
        Debug.Log($"Created lobby {m_CurrentLobby.Id} with name '{m_LobbyName}'");

        // Host is responsible for heartbeating the lobby to keep it alive
        m_Heartbeat = StartCoroutine(HeartbeatLobbyCoroutine());
    }

    public async Task JoinLobbyByNameAsync(string lobbyName)
    {
        var queryLobbiesOptions = new QueryLobbiesOptions();
        queryLobbiesOptions.Filters = new List<QueryFilter>() { new QueryFilter(QueryFilter.FieldOptions.Name, lobbyName, QueryFilter.OpOptions.EQ) };
        try
        {
            QueryResponse lobbies = await LobbyService.Instance.QueryLobbiesAsync();
            if (lobbies.Results.Count == 0)
            {
                Debug.LogError($"Lobby not found: '{lobbyName}'");
                return;
            }

            var foundLobby = lobbies.Results.FirstOrDefault(x => x.Name.Equals(lobbyName));
            if (foundLobby != null)
            {
                Debug.Log($"Joining lobby name:{lobbyName} id:{foundLobby.Id} HostId:{foundLobby.HostId}");
            }
            else
            {
                Debug.LogError($"Lobby not found: '{lobbyName}'.");
                foreach (var lobby in lobbies.Results)
                    Debug.LogWarning($"Name:{lobby.Name} ID:{lobby.Id} HostID:{lobby.HostId}");
                return;
            }
            m_CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(foundLobby.Id);
        }
        catch (LobbyServiceException ex)
        {
            if (ex.Reason == LobbyExceptionReason.LobbyFull)
                Debug.LogError("Failed to join lobby because it is full");
            if (ex.Reason == LobbyExceptionReason.RateLimited)
                Debug.LogWarning($"Hit lobby query rate limit while trying to join lobby '{lobbyName}', try again.");
            return;
        }
        Debug.Log($"Joined lobby ID:{m_CurrentLobby.Id} HostID:{m_CurrentLobby.HostId}");
    }

    public async Task SubscribeToLobbyEvents()
    {
        if (m_CurrentLobby == null || string.IsNullOrEmpty(m_CurrentLobby.Id))
        {
            return;
        }
        var callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += OnLobbyChanged;
        callbacks.KickedFromLobby += OnKickedFromLobby;
        try
        {
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(m_CurrentLobby.Id, callbacks);
            Debug.Log($"Subscribed to lobby events lobbyId:{m_CurrentLobby.Id}");
        }
        catch (LobbyServiceException ex)
        {
            switch (ex.Reason)
            {
                case LobbyExceptionReason.AlreadySubscribedToLobby: Debug.LogWarning($"Already subscribed to lobby[{m_CurrentLobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); break;
                case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Debug.LogError($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); throw;
                case LobbyExceptionReason.LobbyEventServiceConnectionError: Debug.LogError($"Failed to connect to lobby events. Exception Message: {ex.Message}"); throw;
                default: throw;
            }
        }
    }

    public async Task UpdatePlayerAllocationId(Guid allocationId)
    {
        var updatePlayerOptions = new UpdatePlayerOptions() { AllocationId = allocationId.ToString() };
        m_CurrentLobby = await LobbyService.Instance.UpdatePlayerAsync(m_CurrentLobby.Id, AuthenticationService.Instance.PlayerId, updatePlayerOptions);
        await LobbyService.Instance.ReconnectToLobbyAsync(m_CurrentLobby.Id);
    }

    void OnKickedFromLobby()
    {
        Debug.Log("Left lobby");
    }

    void OnLobbyChanged(ILobbyChanges obj)
    {
        Debug.Log("OnLobbyChanged");
    }
}

public static class AllocationUtils
{
    /// <summary>
    /// Convert an allocation to Transport's RelayServerData model
    /// </summary>
    /// <param name="allocation">Allocation from which to create the server data.</param>
    /// <param name="connectionType">Type of connection to use ("udp", "dtls", "ws", or "wss").</param>
    /// <returns>Relay server data model for Transport</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if allocation is null, if the connection type is invalid or if no endpoint match the connection type
    /// </exception>
    public static RelayServerData ToRelayServerData(this Allocation allocation, string connectionType)
    {
        if (allocation == null)
        {
            throw new ArgumentException($"Invalid allocation.");
        }

        ValidateRelayConnectionType(connectionType);
        RelayServerEndpoint endpoint = GetEndpoint(allocation.ServerEndpoints, connectionType);

        return new RelayServerData(
            host: endpoint.Host,
            port: (ushort)endpoint.Port,
            allocationId: allocation.AllocationIdBytes,
            connectionData: allocation.ConnectionData,
            hostConnectionData: allocation.ConnectionData,
            key: allocation.Key,
            isSecure: endpoint.Secure);
    }

    /// <summary>
    /// Convert an allocation to Transport's RelayServerData model
    /// </summary>
    /// <param name="allocation">Allocation from which to create the server data.</param>
    /// <param name="connectionType">Type of connection to use ("udp", "dtls", "ws", or "wss").</param>
    /// <returns>Relay server data model for Transport</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if allocation is null, if the connection type is invalid or if no endpoint match the connection type
    /// </exception>
    public static RelayServerData ToRelayServerData(this JoinAllocation allocation, string connectionType)
    {
        if (allocation == null)
        {
            throw new ArgumentException($"Invalid allocation.");
        }

        ValidateRelayConnectionType(connectionType);
        RelayServerEndpoint endpoint = GetEndpoint(allocation.ServerEndpoints, connectionType);

        return new RelayServerData(
            host: endpoint.Host,
            port: (ushort)endpoint.Port,
            allocationId: allocation.AllocationIdBytes,
            connectionData: allocation.ConnectionData,
            hostConnectionData: allocation.HostConnectionData,
            key: allocation.Key,
            isSecure: endpoint.Secure);
    }

    static RelayServerEndpoint GetEndpoint(List<RelayServerEndpoint> endpoints, string connectionType)
    {
        if (endpoints != null)
        {
            foreach (var serverEndpoint in endpoints)
            {
                if (serverEndpoint.ConnectionType == connectionType)
                {
                    return serverEndpoint;
                }
            }
        }

        throw new ArgumentException($"No endpoint for connection type '{connectionType}' in allocation.");
    }

    static void ValidateRelayConnectionType(string connectionType)
    {
        // We check against a hardcoded list of strings instead of just trying to find the
        // connection type in the endpoints since it may contains things we don't support
        // (e.g. they provide a "tcp" endpoint which we don't support).
        if (connectionType != "udp" && connectionType != "dtls" && connectionType != "ws" && connectionType != "wss")
        {
            throw new ArgumentException($"Invalid connection type: {connectionType}. Must be udp, dtls, ws or wss.");
        }

#if UNITY_WEBGL
        if (connectionType == "udp" || connectionType == "dtls")
        {
            Multiplayer.Logger.LogWarning($"Relay connection type is set to \"{connectionType}\" which is not valid on WebGL. Use \"wss\" instead.");
        }
#endif
    }
}