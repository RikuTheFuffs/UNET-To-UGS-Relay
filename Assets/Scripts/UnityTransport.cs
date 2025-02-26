using System;
using System.Net;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using NetworkConnection = Unity.Networking.Transport.NetworkConnection;

class UnityTransport : INetworkTransport
{
    NetworkDriver m_Driver;
    NativeList<NetworkConnection> m_Connections;
    bool m_IsServer;
    bool m_IsClient;

    public bool UseRelay { get; set; }
    public RelayServerData RelayServerData { get; set; }

    public void Init()
    {
        if (m_Driver.IsCreated)
        {
            Debug.Log("UnityTransport driver already initialized.");
            return;
        }

        if (UseRelay)
        {
            Debug.Log($"UnityTransport Init With Relay Server {RelayServerData.ConnectionData.ToString()}");
            NetworkSettings networkSettings = new NetworkSettings();
            var relayServerData = RelayServerData;
            RelayParameterExtensions.WithRelayParameters(ref networkSettings, ref relayServerData);
            m_Driver = NetworkDriver.Create(networkSettings);
        }
        else
        {
            Debug.Log("UnityTransport Init");
            m_Driver = NetworkDriver.Create();
        }
        m_Connections = new NativeList<NetworkConnection>(Allocator.Persistent);
        // Must be initialized because the editor profiler expects to be able to access NetworkTransport profiler APIs
        NetworkTransport.Init();
    }

    public void Init(GlobalConfig config)
    {
        // TODO: Use NetworkSettings instead of GlobalConfig
        Debug.Log("Ignoring global config");
        Init();
    }
    public bool IsStarted => m_Driver.IsCreated;

    public void Shutdown()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }
    public int AddHost(HostTopology topology, int port, string ip)
    {
        NetworkEndPoint endpoint;
        if (string.IsNullOrEmpty(ip))
        {
            endpoint = NetworkEndPoint.AnyIpv4.WithPort((ushort)port);
        }
        else if (!NetworkEndPoint.TryParse(ip, (ushort)port, out endpoint))
        {
            Debug.LogError($"Failed to parse ip address: {ip}:{port}");
            return -1;
        }
        if (m_Driver.Bind(endpoint) != 0)
        {
            Debug.LogError($"Failed to bind to endpoint {endpoint.ToString()}");
            return -1;
        }
        var ret = m_Driver.Listen();
        if (ret != 0)
        {
            Debug.LogError($"Failed to listen: {ret}");
            return -1;
        }
        m_IsServer = true;
        return 0;
    }
    public int AddWebsocketHost(HostTopology topology, int port, string ip)
    {
        throw new System.NotImplementedException();
    }
    public int ConnectWithSimulator(int hostId, string address, int port, int specialConnectionId, out byte error, ConnectionSimulatorConfig conf)
    {
        throw new System.NotImplementedException();
    }
    public int Connect(int hostId, string address, int port, int specialConnectionId, out byte error)
    {
        error = 0;
        // TODO: Ignoring hostId
        // TODO: Ignoring specialConnectionId
        if (NetworkEndPoint.TryParse(address, (ushort)port, out NetworkEndPoint endpoint))
        {
            m_Connections.Add(m_Driver.Connect(endpoint));
            m_IsClient = true;
            return 0;
        }
        error = 1;
        return -1;
    }
    public void ConnectAsNetworkHost(int hostId, string address, int port, NetworkID network, SourceID source, NodeID node, out byte error)
    {
        throw new System.NotImplementedException();
    }
    public int ConnectToNetworkPeer(int hostId, string address, int port, int specialConnectionId, int relaySlotId, NetworkID network, SourceID source, NodeID node, out byte error)
    {
        throw new System.NotImplementedException();
    }
    public int ConnectEndPoint(int hostId, EndPoint endPoint, int specialConnectionId, out byte error)
    {
        throw new System.NotImplementedException();
    }
    public bool DoesEndPointUsePlatformProtocols(EndPoint endPoint)
    {
        throw new System.NotImplementedException();
    }
    public int AddHostWithSimulator(HostTopology topology, int minTimeout, int maxTimeout, int port)
    {
        throw new System.NotImplementedException();
    }
    public bool RemoveHost(int hostId)
    {
        throw new System.NotImplementedException();
    }
    public bool Send(int hostId, int connectionId, int channelId, byte[] buffer, int size, out byte error)
    {
        error = 0;
        if (connectionId >= m_Connections.Length)
        {
            Debug.LogError($"[UnityTransport] Invalid connection Id {connectionId} connection count={m_Connections.Length}");
            error = 1;
            return false;
        }
        m_Driver.BeginSend(NetworkPipeline.Null, m_Connections[connectionId], out var writer);
        unsafe
        {
            fixed (byte* ptr = buffer)
            {
                writer.WriteBytes(ptr, size);
            }
        }
        m_Driver.EndSend(writer);
        if (writer.HasFailedWrites)
        {
            error = 1;
            Debug.LogError($"[UnityTransport] Failed to write data ({size} bytes) to send buffer");
            return false;
        }
        Debug.Log($"[UnityTransport] Sending data hostId={hostId} connectionId={connectionId} channelId={channelId} size={size}");
        var byteString = BitConverter.ToString(buffer, 0, size);
        Debug.Log($"[UnityTransport] Buffer data={byteString}");
        return true;
    }
    public NetworkEventType Receive(out int hostId, out int connectionId, out int channelId, byte[] buffer, int bufferSize, out int receivedSize, out byte error)
    {
        throw new System.NotImplementedException();
    }
    public NetworkEventType ReceiveFromHost(int hostId, out int connectionId, out int channelId, byte[] buffer, int bufferSize, out int receivedSize, out byte error)
    {
        //Debug.Log($"[{Time.frameCount}][{Time.realtimeSinceStartup}] UnityTransport ReceiveFromHost");
        connectionId = -1;
        channelId = -1;
        receivedSize = -1;
        error = 0;
        m_Driver.ScheduleUpdate().Complete();

        // CleanUpConnections
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        if (m_IsServer)
        {
            // AcceptNewConnections
            NetworkConnection c;
            while ((c = m_Driver.Accept()) != default(NetworkConnection))
            {
                // TODO: Add to empty slot
                connectionId = m_Connections.Length;
                m_Connections.Add(c);
                Debug.Log($"[UnityTransport] Accepted a connection Id:{connectionId} InternalId:{c.InternalId}");
                return NetworkEventType.ConnectEvent;
            }
        }

        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            NetworkEvent.Type cmd;
            while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Connect)
                {
                    Debug.Log($"[UnityTransport] Connect event on connection ID {i}");
                    return NetworkEventType.ConnectEvent;
                }
                else if (cmd == NetworkEvent.Type.Data)
                {
                    connectionId = i;
                    channelId = 0;

                    if (stream.Length > bufferSize)
                    {
                        Debug.LogError($"[UnityTransport] Received {stream.Length} bytes but buffer size is {bufferSize}");
                        error = 1;
                    }
                    else
                    {
                        receivedSize = stream.Length;
                        unsafe
                        {
                            fixed (byte* ptr = buffer)
                            {
                                stream.ReadBytes(ptr, stream.Length);
                            }
                        }
                        Debug.Log($"[UnityTransport] Received {stream.Length} bytes on connection {i} buffer size={bufferSize}");
                        return NetworkEventType.DataEvent;
                    }
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("[UnityTransport] Client disconnected from server");
                    m_Connections[i] = default(NetworkConnection);
                    return NetworkEventType.DisconnectEvent;
                }
            }
        }
        return NetworkEventType.Nothing;
    }
    public NetworkEventType ReceiveRelayEventFromHost(int hostId, out byte error)
    {
        throw new System.NotImplementedException();
    }
    public int GetCurrentRTT(int hostId, int connectionId, out byte error)
    {
        // TODO: Figure out how to fetch the RTT
        error = 0;
        return 10;
    }
    public void GetConnectionInfo(int hostId, int connectionId, out string address, out int port, out NetworkID network, out NodeID dstNode, out byte error)
    {
        address = "";
        port = 0;
        network = NetworkID.Invalid;
        dstNode = 0;
        error = 0;
        if (connectionId >= m_Connections.Length)
        {
            Debug.Log($"[UnityTransport] Failed to lookup connection info for {connectionId}");
            return;
        }
        var connection = m_Connections[connectionId];
        var endpoint = m_Driver.RemoteEndPoint(connection);
        address = endpoint.Address;
        port = endpoint.Port;
        Debug.Log($"[UnityTransport] GetConnectionInfo: {endpoint.ToString()}");
    }
    public bool Disconnect(int hostId, int connectionId, out byte error)
    {
        if (connectionId >= m_Connections.Length)
        {
            Debug.Log($"[UnityTransport] Failed to initiate a disconnect from connection ID {connectionId} (conneciton list length = {m_Connections.Length})");
            error = 1;
            return false;
        }
        var connection = m_Connections[connectionId];
        m_Driver.Disconnect(connection);
        error = 0;
        return true;
    }
    public void SetBroadcastCredentials(int hostId, int key, int version, int subversion, out byte error)
    {
        throw new System.NotImplementedException();
    }
    public bool StartBroadcastDiscovery(int hostId, int broadcastPort, int key, int version, int subversion, byte[] buffer, int size, int timeout, out byte error)
    {
        throw new System.NotImplementedException();
    }
    public void GetBroadcastConnectionInfo(int hostId, out string address, out int port, out byte error)
    {
        throw new System.NotImplementedException();
    }
    public void GetBroadcastConnectionMessage(int hostId, byte[] buffer, int bufferSize, out int receivedSize, out byte error)
    {
        throw new System.NotImplementedException();
    }
    public void StopBroadcastDiscovery()
    {
        throw new System.NotImplementedException();
    }
    public void SetPacketStat(int direction, int packetStatId, int numMsgs, int numBytes)
    {
        // not implemented
    }
}
