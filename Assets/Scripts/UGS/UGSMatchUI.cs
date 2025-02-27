using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Networking.Match;
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
    Button m_ButtonToggleVisbility;

    Lobby m_MatchInfo;

    public void Initialize(Lobby matchInfo)
    {
        m_MatchInfo = matchInfo;
        m_LabelInfo.text = $"Name: '{matchInfo.Name}' | Players: {matchInfo.Players.Count}/{matchInfo.MaxPlayers}";
        m_ButtonJoin.onClick.RemoveAllListeners();
        m_ButtonJoin.onClick.AddListener(OnClickJoinMatch);

        m_ButtonDelete.onClick.RemoveAllListeners();
        m_ButtonDelete.onClick.AddListener(OnClickDeleteMatch);

        m_ButtonToggleVisbility.onClick.RemoveAllListeners();
        m_ButtonToggleVisbility.onClick.AddListener(OnClickToggleMatchVisibility);
    }

    void OnClickJoinMatch()
    {
        //m_Matchmaker.JoinMatch(netId: m_MatchInfoSnapshot.networkId,
        //    matchPassword: "Password",
        //    publicClientAddress: "",
        //    privateClientAddress: "",
        //    eloScoreForClient: 0,
        //    requestDomain: 0,
        //    callback: OnMatchJoined
        //);
    }

    void OnMatchJoined(bool success, string extendedInfo, MatchInfo responseData)
    {
        Debug.Log($"OnMatchJoined: {success}; ExtendedInfo: {extendedInfo} | Response data: IP: {responseData.address}");
        if (success)
        {
           //UNETMatchmakerUI.s_CurrentMatch = responseData;
        }
    }

    void OnClickDeleteMatch()
    {
        //m_Matchmaker.DestroyMatch(netId: m_MatchInfoSnapshot.networkId,
        //    requestDomain: 0,
        //    callback: OnMatchDeleted
        //);
    }

    void OnMatchDeleted(bool success, string extendedInfo)
    {
        Debug.Log($"OnMatchDeleted: {success}; ExtendedInfo: {extendedInfo}");
        if (success)
        {
            Destroy(gameObject);
        }
    }

    void OnClickToggleMatchVisibility()
    {
        //m_Matchmaker.SetMatchAttributes
        //(
        //    networkId: m_MatchInfoSnapshot.networkId,
        //    isListed: m_MatchInfoSnapshot.isPrivate,
        //    requestDomain: 0,
        //    callback: OnMatchVisibilityToggled
        //);
    }

    void OnMatchVisibilityToggled(bool success, string extendedInfo)
    {
        Debug.Log($"OnMatchVisibilityToggled: {success}; ExtendedInfo: {extendedInfo}");
    }
}
