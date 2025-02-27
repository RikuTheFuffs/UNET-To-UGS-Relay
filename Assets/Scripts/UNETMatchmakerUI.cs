using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

public class UNETMatchmakerUI : MonoBehaviour
{
    [SerializeField]
    NetworkManager networkManager;

    [SerializeField]
    Button buttonCreateMatchmakerSession;

    [SerializeField]
    Button buttonListMatchmakerSessions;

    void OnEnable()
    {
        buttonCreateMatchmakerSession.onClick.RemoveAllListeners();
        buttonCreateMatchmakerSession.onClick.AddListener(OnClickCreateMatchmakerSession);

        buttonListMatchmakerSessions.onClick.RemoveAllListeners();
        buttonListMatchmakerSessions.onClick.AddListener(OnClickListMatchmakerSessions);
    }

    void InitializeMatchmaker()
    {
        if (networkManager.matchMaker)
        {
            return;
        }
        networkManager.StartMatchMaker();
    }

    void OnClickCreateMatchmakerSession()
    {
        Debug.Log($"OnClickCreateMatchmakerSession");
        InitializeMatchmaker();
        networkManager.matchMaker.CreateMatch(
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

    void OnClickListMatchmakerSessions()
    {
        Debug.Log($"OnClickListMatchmakerSessions");
        InitializeMatchmaker();
        networkManager.matchMaker.ListMatches(
            startPageNumber: 0,
            resultPageSize: 20,
            matchNameFilter: "",
            filterOutPrivateMatchesFromResults: false,
            eloScoreTarget: 0,
            requestDomain: 0,
            callback: OnSessionsListRetrieved);
    }

    void OnSessionsListRetrieved(bool success, string extendedInfo, List<MatchInfoSnapshot> responseData)
    {
        Debug.Log($"OnSessionsListRetrieved: {success}; ExtendedInfo: {extendedInfo} | Response data: found {responseData.Count} matches");
    }
}