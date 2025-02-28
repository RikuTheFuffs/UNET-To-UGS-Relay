using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.UI;


using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;
using Unity.Services.Core;
using System.Collections;

public class UGSLobbyAndRelayUI : MonoBehaviour
{
    public static Lobby s_CurrentMatch;
    const int k_HeartbeatIntervalSeconds = 10;

    [SerializeField]
    NetworkManager m_NetworkManager;

    [SerializeField]
    Button m_ButtonCreateMatch;

    [SerializeField]
    Button m_ButtonListMatches;

    [SerializeField]
    Button m_ButtonLeaveCurrentMatche;

    [SerializeField]
    RectTransform m_MatchesList;

    [SerializeField]
    UGSMatchUI m_MatchUIPrefab;
    Coroutine m_Heartbeat;
    Lobby m_CurrentLobby;

    void OnEnable()
    {
        m_ButtonCreateMatch.onClick.RemoveAllListeners();
        m_ButtonCreateMatch.onClick.AddListener(OnClickCreateMatch);

        m_ButtonListMatches.onClick.RemoveAllListeners();
        m_ButtonListMatches.onClick.AddListener(OnClickListMatches);

        //m_ButtonLeaveCurrentMatche.onClick.RemoveAllListeners();
        //m_ButtonLeaveCurrentMatche.onClick.AddListener(OnClickLeaveCurrentMatch);
    }

    async void Start()
    {
        await InitializeUnityServices();
    }

    async Task InitializeUnityServices()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Initialized Unity Gaming Services");
        }
        //var player = new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject>());
    }

    void OnClickCreateMatch()
    {
        CreateMatch();
    }

    async Task CreateMatch()
    {
        Debug.Log($"OnClickCreateMatch");
        await InitializeUnityServices();
        Debug.Log($"Creating lobby");

        var lobbyData = new Dictionary<string, DataObject>()
        {
            ["Domain"] = new DataObject(DataObject.VisibilityOptions.Public, "MyDomain", DataObject.IndexOptions.S1),
            ["EloScore"] = new DataObject(DataObject.VisibilityOptions.Public, "123", DataObject.IndexOptions.N1),
        };

        var player = new Player(AuthenticationService.Instance.PlayerId, "MyWanIp",
            new Dictionary<string, PlayerDataObject>()
            {
                ["EloScore"] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "123")
            });

        m_CurrentLobby = await Lobbies.Instance.CreateLobbyAsync(lobbyName: "My Lobby Name", maxPlayers: 16,
            options: new CreateLobbyOptions()
            {
                Data = lobbyData,
                IsPrivate = false,
                Player = player
            });
        Debug.Log($"Created lobby {m_CurrentLobby.Id} with name '{m_CurrentLobby.Name}'");
        OnMatchCreated(m_CurrentLobby);
    }

    IEnumerator HeartbeatLobbyCoroutine()
    {
        var waitingInterval = new WaitForSecondsRealtime(k_HeartbeatIntervalSeconds);
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
            yield return waitingInterval;
        }
    }


    void OnMatchCreated(Lobby lobby)
    {
        Debug.Log($"Match created: {lobby.Created}; Code: {lobby.LobbyCode}");
        // Host is responsible for heartbeating the lobby to keep it alive
        m_Heartbeat = StartCoroutine(HeartbeatLobbyCoroutine());
    }

    void OnClickListMatches()
    {
        ListMatches();
    }

    async Task ListMatches()
    {
        Debug.Log($"OnClickListMatches");
        await InitializeUnityServices();

        var queryFilters = new List<QueryFilter>
        {
            // Search for games with open slots (AvailableSlots greater than 0)
            new QueryFilter(
                field: QueryFilter.FieldOptions.AvailableSlots,
                op: QueryFilter.OpOptions.GT,
                value: "0"),

            // Search for games with domain = a specific value
            new QueryFilter(
                field: QueryFilter.FieldOptions.S1,
                op: QueryFilter.OpOptions.EQ,
                value: "MyDomain"),

            // EloSkill >= 10
            new QueryFilter(
                field: QueryFilter.FieldOptions.N1,
                op: QueryFilter.OpOptions.GE,
                value: "10"),

            // EloSkill <= 200
            new QueryFilter(
                field: QueryFilter.FieldOptions.N1,
                op: QueryFilter.OpOptions.LE,
                value: "200"),
        };

        // Query results can also be ordered
        var queryOrdering = new List<QueryOrder>
        {
            new QueryOrder(true, QueryOrder.FieldOptions.AvailableSlots),
            new QueryOrder(false, QueryOrder.FieldOptions.Created),
        };

        // Call the Query API
        Debug.Log($"Querying...");
        QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync(new QueryLobbiesOptions()
        {
            Count = 20, // Override default number of results to return
            Filters = queryFilters,
            Order = queryOrdering,
            SampleResults = false,
            Skip = 0
        });

        List<Lobby> foundLobbies = response.Results;
        OnMatchesListRetrieved(foundLobbies);
    }

    void OnMatchesListRetrieved(List<Lobby> responseData)
    {
        Debug.Log($"OnMatchesListRetrieved: Response data: found {responseData.Count} matches");
        for (int i = m_MatchesList.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(m_MatchesList.transform.GetChild(i).gameObject);
        }

        foreach (var item in responseData)
        {
            UGSMatchUI matchUIInstance = Instantiate(m_MatchUIPrefab, m_MatchesList);
            matchUIInstance.Initialize(item);
        }
    }

    async Task OnClickLeaveCurrentMatch()
    {
        Debug.Log($"OnClickLeaveCurrentMatch");
        await InitializeUnityServices();
        //if (s_CurrentMatch == null)
        //{
        //    Debug.LogError("Can't leave match as I'm not in a match.");
        //    return;
        //}
        //m_NetworkManager.matchMaker.DropConnection(netId: s_CurrentMatch.networkId,
        //    dropNodeId: s_CurrentMatch.nodeId,
        //    requestDomain: s_CurrentMatch.domain,
        //    callback: OnCurrentMatchLeft);
    }

    void OnCurrentMatchLeft(bool success, string extendedInfo)
    {
        Debug.Log($"OnCurrentMatchLeft: {success}; ExtendedInfo: {extendedInfo}");
    }
}