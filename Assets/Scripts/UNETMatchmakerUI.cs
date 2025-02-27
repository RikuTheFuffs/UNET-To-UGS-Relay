using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

public class UNETMatchmakerUI : MonoBehaviour
{
    [SerializeField]
    NetworkManager m_NetworkManager;

    [SerializeField]
    Button m_ButtonCreateMatch;

    [SerializeField]
    Button m_ButtonListMatches;

    [SerializeField]
    RectTransform m_MatchesList;

    [SerializeField]
    MatchUI m_MatchUIPrefab;

    void OnEnable()
    {
        m_ButtonCreateMatch.onClick.RemoveAllListeners();
        m_ButtonCreateMatch.onClick.AddListener(OnClickCreateMatch);

        m_ButtonListMatches.onClick.RemoveAllListeners();
        m_ButtonListMatches.onClick.AddListener(OnClickListMatches);
    }

    void InitializeMatchmaker()
    {
        if (m_NetworkManager.matchMaker)
        {
            return;
        }
        m_NetworkManager.StartMatchMaker();
    }

    void OnClickCreateMatch()
    {
        Debug.Log($"OnClickCreateMatch");
        InitializeMatchmaker();
        m_NetworkManager.matchMaker.CreateMatch(
                    matchName: "My Match Name",
                    matchSize: 16,
                    matchAdvertise: true,
                    matchPassword: "Password",
                    publicClientAddress: "",
                    privateClientAddress: "",
                    eloScoreForMatch: 0,
                    requestDomain: 0,
                    callback: OnMatchCreated);
    }

    void OnMatchCreated(bool success, string extendedInfo, MatchInfo responseData)
    {
        Debug.Log($"Match created: {success}; ExtendedInfo: {extendedInfo} | Response data: {responseData}");
    }

    void OnClickListMatches()
    {
        Debug.Log($"OnClickListMatches");
        InitializeMatchmaker();
        m_NetworkManager.matchMaker.ListMatches(
            startPageNumber: 0,
            resultPageSize: 20,
            matchNameFilter: "",
            filterOutPrivateMatchesFromResults: false,
            eloScoreTarget: 0,
            requestDomain: 0,
            callback: OnMatchesListRetrieved);
    }

    void OnMatchesListRetrieved(bool success, string extendedInfo, List<MatchInfoSnapshot> responseData)
    {
        Debug.Log($"OnMatchesListRetrieved: {success}; ExtendedInfo: {extendedInfo} | Response data: found {responseData.Count} matches");
        for (int i = m_MatchesList.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(m_MatchesList.transform.GetChild(i).gameObject);
        }

        foreach (var item in responseData)
        {
            MatchUI matchUIInstance = Instantiate(m_MatchUIPrefab, m_MatchesList);
            matchUIInstance.Initialize(m_NetworkManager.matchMaker, item);
        }
    }
}