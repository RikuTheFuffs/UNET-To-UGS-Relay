using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class UGSMatchUI : MonoBehaviour
{
    [SerializeField]
    TMP_Text m_LabelInfo;

    [SerializeField]
    Button m_ButtonJoin;

    [SerializeField]
    Button m_ButtonDelete;

    [SerializeField]
    Button m_ButtonEditLobby;

    Lobby m_MatchInfo;

    public void Initialize(Lobby matchInfo)
    {
        m_MatchInfo = matchInfo;
        m_LabelInfo.text = $"Name: '{matchInfo.Name}' | Players: {matchInfo.Players.Count}/{matchInfo.MaxPlayers}";
        m_ButtonJoin.onClick.RemoveAllListeners();
        m_ButtonJoin.onClick.AddListener(OnClickJoinMatch);

        m_ButtonDelete.onClick.RemoveAllListeners();
        m_ButtonDelete.onClick.AddListener(OnClickDeleteMatch);

        m_ButtonEditLobby.onClick.RemoveAllListeners();
        m_ButtonEditLobby.onClick.AddListener(OnClickRenameLobby);
    }

    void OnClickJoinMatch()
    {
        JoinMatch();
    }

    async Task JoinMatch()
    {
        var player = new Player(
            id: AuthenticationService.Instance.PlayerId,
            connectionInfo: "MyWanIp",
            data: new Dictionary<string, PlayerDataObject>()
            {
                ["EloScore"] = new PlayerDataObject(
                    visibility: PlayerDataObject.VisibilityOptions.Public,
                    value: "123")
            });

        /* Using the JoinLobbyByCodeAsync API
        Lobby joinedLobbyByCode = await Lobbies.Instance.JoinLobbyByCodeAsync(
            lobbyCode: "myLobbyCode",
            options: new JoinLobbyByCodeOptions()
            {
                Player = player
            });
        */

        // Using the JoinLobbyByIdAsync API
        m_MatchInfo = await Lobbies.Instance.JoinLobbyByIdAsync(
            lobbyId: m_MatchInfo.Id,
            options: new JoinLobbyByIdOptions()
            {
                Player = player
            });

        /* Using the QuickJoinLobbyAsync API
        var queryFilters = new List<QueryFilter>
        {
                // Search for games with domain = a specific value
                new QueryFilter(
                    field: QueryFilter.FieldOptions.S1,
                    op: QueryFilter.OpOptions.EQ,
                    value: "MyDomain"),
                };

        var options = new QuickJoinLobbyOptions()
        {
            Filter = queryFilters,
            Player = player
        };

        Lobby joinedLobby = await Lobbies.Instance.QuickJoinLobbyAsync(options);
        */
        OnMatchJoined(m_MatchInfo);
    }

    void OnMatchJoined(Lobby lobby)
    {
        Debug.Log($"OnMatchJoined: {lobby.Id}");
        UGSLobbyAndRelayUI.s_CurrentMatch = lobby;
    }

    void OnClickDeleteMatch()
    {
        DeleteMatch();
    }

    async Task DeleteMatch()
    {
        await Lobbies.Instance.DeleteLobbyAsync(m_MatchInfo.Id);
        OnMatchDeleted();
    }

    void OnMatchDeleted()
    {
        Debug.Log($"OnMatchDeleted");
        Destroy(gameObject);
    }

    void OnClickRenameLobby()
    {
        RenameLobby();
    }

    async Task RenameLobby()
    {
        // Lobby custom data
        var lobbyData = new Dictionary<string, DataObject>()
        {
            ["AverageEloScore"] = new DataObject(
                visibility: DataObject.VisibilityOptions.Public,
                value: "123"),
        };

        try
        {
            var updatedOptions = new UpdateLobbyOptions()
            {
                Data = lobbyData,
                HostId = AuthenticationService.Instance.PlayerId,
                MaxPlayers = 8,
                Name = "new lobby name"
            };

            // Update lobby custom data and metadata
            m_MatchInfo = await Lobbies.Instance.UpdateLobbyAsync(m_MatchInfo.Id, updatedOptions);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
        OnMatchVisibilityToggled(m_MatchInfo);
    }

    void OnMatchVisibilityToggled(Lobby match)
    {
        Debug.Log($"OnMatchVisibilityToggled: Private: {match.IsPrivate}");
        m_LabelInfo.text = $"Name: '{match.Name}' | Players: {match.Players.Count}/{match.MaxPlayers}";

    }
}
