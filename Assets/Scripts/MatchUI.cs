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

    MatchInfoSnapshot m_MatchInfoSnapshot;
    NetworkMatch m_Matchmaker;

    public void Initialize(NetworkMatch matchmaker, MatchInfoSnapshot matchInfoSnapshot)
    {
        m_Matchmaker = matchmaker;
        m_MatchInfoSnapshot = matchInfoSnapshot;
        m_LabelInfo.text = $"Name: '{matchInfoSnapshot.name}' | Players: {matchInfoSnapshot.currentSize}/{matchInfoSnapshot.maxSize}";
        m_ButtonJoin.onClick.RemoveAllListeners();
        m_ButtonJoin.onClick.AddListener(OnClickJoinMatch);
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
    }
}
