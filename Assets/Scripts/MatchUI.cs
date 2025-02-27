using TMPro;
using UnityEngine;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

public class MatchUI : MonoBehaviour
{
    [SerializeField]
    TMP_Text m_LabelInfo;

    [SerializeField]
    Button m_ButtonJoin;

    [SerializeField]
    Button m_ButtonDelete;

    [SerializeField]
    Button m_ButtonToggleVisbility;

    MatchInfoSnapshot m_MatchInfoSnapshot;
    NetworkMatch m_Matchmaker;

    public void Initialize(NetworkMatch matchmaker, MatchInfoSnapshot matchInfoSnapshot)
    {
        m_Matchmaker = matchmaker;
        m_MatchInfoSnapshot = matchInfoSnapshot;
        m_LabelInfo.text = $"Name: '{matchInfoSnapshot.name}' | Players: {matchInfoSnapshot.currentSize}/{matchInfoSnapshot.maxSize}";
        m_ButtonJoin.onClick.RemoveAllListeners();
        m_ButtonJoin.onClick.AddListener(OnClickJoinMatch);

        m_ButtonDelete.onClick.RemoveAllListeners();
        m_ButtonDelete.onClick.AddListener(OnClickDeleteMatch);

        m_ButtonToggleVisbility.onClick.RemoveAllListeners();
        m_ButtonToggleVisbility.onClick.AddListener(OnClickToggleMatchVisibility);
    }

    void OnClickJoinMatch()
    {
        m_Matchmaker.JoinMatch(netId: m_MatchInfoSnapshot.networkId,
            matchPassword: "Password",
            publicClientAddress: "",
            privateClientAddress: "",
            eloScoreForClient: 0,
            requestDomain: 0,
            callback: OnMatchJoined
        );
    }

    void OnMatchJoined(bool success, string extendedInfo, MatchInfo responseData)
    {
        Debug.Log($"OnMatchJoined: {success}; ExtendedInfo: {extendedInfo} | Response data: IP: {responseData.address}");
        if (success)
        {
           UNETMatchmakerUI.s_CurrentMatch = responseData;
        }
    }

    void OnClickDeleteMatch()
    {
        m_Matchmaker.DestroyMatch(netId: m_MatchInfoSnapshot.networkId,
            requestDomain: 0,
            callback: OnMatchDeleted
        );
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
        m_Matchmaker.SetMatchAttributes
        (
            networkId: m_MatchInfoSnapshot.networkId,
            isListed: m_MatchInfoSnapshot.isPrivate,
            requestDomain: 0,
            callback: OnMatchVisibilityToggled
        );
    }

    void OnMatchVisibilityToggled(bool success, string extendedInfo)
    {
        Debug.Log($"OnMatchVisibilityToggled: {success}; ExtendedInfo: {extendedInfo}");
    }
}
