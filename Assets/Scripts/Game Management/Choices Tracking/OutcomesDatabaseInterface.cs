using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class OutcomesDatabaseInterface : MonoBehaviour
{
    private const string SUPABASE_URL = "https://ohertincmhfngikmccpc.supabase.co";
    private const string SUPABASE_ANON_KEY = "sb_publishable_MP0wKenvg5HGeW333UgdpA_y_lVCMIM";
    private const int REQUEST_TIMEOUT = 10;

    private HashSet<string> _submitted = new HashSet<string>();

    [Serializable] private struct OutcomePayload
    {
        public string choice_id;
        public string choice_value;
    }

    [Serializable] private struct StatRow
    {
        public string choice_value;
        public int count;
        public float pct;
    }

    [Serializable] private struct StatArray
    {
        public StatRow[] items;
    }

/// Call this when the player locks in an outcome.
    /// outcomeId:    unique string per decision point, e.g. "level2_door"
    /// outcomeValue: whatever the player picked, e.g. "left" / "right"
    public void submitOutcome(string outcomeId, string outcomeValue)
    {
        if (_submitted.Contains(outcomeId)) return;

        StartCoroutine(attemptPOST(outcomeId, outcomeValue));
    }

    public void fetchStats(string outcomeId, string outcomeValue,
                            Action<Dictionary<string, float>> onComplete = null)
    {
        StartCoroutine(attemptGET(outcomeId, outcomeValue, onComplete));
    }

    private IEnumerator attemptPOST(string outcomeId, string outcomeValue)
    {
        var payload = new OutcomePayload
        {
            choice_id = outcomeId,
            choice_value = outcomeValue
        };
        string body = JsonUtility.ToJson(payload);

        using (var post = new UnityWebRequest(SUPABASE_URL + "/rest/v1/choices", "POST"))
        {
            post.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            post.downloadHandler = new DownloadHandlerBuffer();
            post.timeout = REQUEST_TIMEOUT;
            post.SetRequestHeader("Content-Type",  "application/json");
            post.SetRequestHeader("apikey",        SUPABASE_ANON_KEY);
            post.SetRequestHeader("Authorization", "Bearer " + SUPABASE_ANON_KEY);
            post.SetRequestHeader("Prefer",       "return=minimal");

            yield return post.SendWebRequest();
            
            if (post.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("OutcomeTracker POST failed: " + post.error);
                yield break;
            }
            else
            {
                _submitted.Add(outcomeId);
            }
        }      
    }

    private IEnumerator attemptGET(string outcomeId, string outcomeValue,
                                    Action<Dictionary<string, float>> onComplete = null)
    {
        string url = SUPABASE_URL
            + "/rest/v1/rpc/get_choice_stats?p_choice_id="
            + UnityWebRequest.EscapeURL(outcomeId);

        using (var get = UnityWebRequest.Get(url))
        {
            get.timeout = REQUEST_TIMEOUT;
            get.SetRequestHeader("apikey",        SUPABASE_ANON_KEY);
            get.SetRequestHeader("Authorization", "Bearer " + SUPABASE_ANON_KEY);
            yield return get.SendWebRequest();

            if (get.result == UnityWebRequest.Result.Success)
            {
                var stats = parseStats(get.downloadHandler.text);
                onStatsReady(outcomeId, outcomeValue, stats);
                onComplete?.Invoke(stats);
            }
            else
            {
                Debug.LogWarning("OutcomeTracker GET failed: " + get.error);
                onComplete?.Invoke(new Dictionary<string, float>());
            }
        }
    }

    // DB returns: [{"outcome_value":"left","count":312,"pct":62.4}, ...]
    private Dictionary<string, float> parseStats(string json)
    {
        var pct = new Dictionary<string, float>();
        var wrapped = "{\"items\":" + json + "}";
        var array = JsonUtility.FromJson<StatArray>(wrapped);
        if (array.items != null)
        {
            foreach (var row in array.items)
                pct[row.choice_value] = row.pct;
        }
        return pct;
    }

    // Hook this up to UI to show the stats
    private void onStatsReady(string outcomeId, string outcomeValue,
                               Dictionary<string, float> pct)
    {
        if (pct.ContainsKey(outcomeValue))
            Debug.Log($"{pct[outcomeValue]:F0}% of players also chose '{outcomeValue}'");

        // Replace the line above with  actual UI call, e.g.:
        // StatsPopup.Show(outcomeValue, pct);
    }
}
