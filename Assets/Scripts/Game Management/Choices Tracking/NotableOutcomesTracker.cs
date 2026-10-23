using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotableOutcomesTracker : MonoBehaviour
{
    [ES3Serializable]
    [System.NonSerialized]
    public Dictionary<string, string> outcomes = new Dictionary<string, string>();

    private OutcomesDatabaseInterface dbInterface;

    [System.NonSerialized]
    public static NotableOutcomesTracker instance;

    void Awake()
    {
        if (instance == null && this != instance)
            instance = this;
        else
            Destroy(gameObject);

        dbInterface = GetComponent<OutcomesDatabaseInterface>();
    }

    public void recordOutcome(string outcomeId, string outcomeValue)
    {
        Debug.Log("Recording outcome: " + outcomeId + " = " + outcomeValue);
        if (outcomes.ContainsKey(outcomeId)) return;

        outcomes[outcomeId] = outcomeValue;
        GameEvents.current.outcomeRecorded(outcomeId, outcomeValue);
    }

    public string getOutcome(string outcomeId)
    {
        return outcomes.ContainsKey(outcomeId) ? outcomes[outcomeId] : "";
    }

    public bool hasOutcome(string outcomeId)
    {
        return outcomes.ContainsKey(outcomeId);
    }

    public bool checkOutcome(string outcomeId, string expectedValue)
    {
        return outcomes.ContainsKey(outcomeId) && outcomes[outcomeId] == expectedValue;
    }

    public void pushAllOutcomesToDB()
    {
        // to do - filter by act? don't push duplicates?
        foreach (var kvp in outcomes)
            dbInterface.submitOutcome(kvp.Key, kvp.Value);
    }

    // call by UI when player clicks "view stats"
    public void fetchAllOutcomeStats(Action<Dictionary<string, Dictionary<string, float>>> onComplete)
    {
        StartCoroutine(fetchAllStatsCoroutine(onComplete));
    }

    private IEnumerator fetchAllStatsCoroutine(Action<Dictionary<string, Dictionary<string, float>>> onComplete)
    {
        // to do - filter by act?
        var allStats = new Dictionary<string, Dictionary<string, float>>();
        int remaining = outcomes.Count;

        if (remaining == 0)
        {
            onComplete?.Invoke(allStats);
            yield break;
        }

        foreach (var kvp in outcomes)
        {
            dbInterface.fetchStats(kvp.Key, kvp.Value, (stats) =>
            {
                allStats[kvp.Key] = stats;
                remaining--;
            });
        }

        while (remaining > 0)
            yield return null;

        onComplete?.Invoke(allStats);
    }
}
