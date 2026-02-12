using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class AIClient : MonoBehaviour
{
    [Header("Backend endpoint (NOWY)")]
    [SerializeField] private string endpoint = "https://startlesson-d37rl4bnda-uc.a.run.app"; 

    [SerializeField] private TMP_Text outputText;

    private const string PREF_SESSION_ID = "sessionId";

    [Serializable]
    private class Req
    {
        public string sessionId;
        public string message;
    }

    [Serializable]
    private class Res
    {
        public bool ok;
        public string reply;
        public string error;
        public string details;
    }

    public void AskAI(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        var sessionId = PlayerPrefs.GetString(PREF_SESSION_ID, "");
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            if (outputText != null) outputText.text = "Brak sessionId. Najpierw wybierz lekcjê w menu.";
            Debug.LogError("AIClient: missing sessionId (startLesson nie by³ uruchomiony).");
            return;
        }

        StartCoroutine(CallAI(sessionId, message));
    }

    private IEnumerator CallAI(string sessionId, string message)
    {
        if (outputText != null) outputText.text = "AI: ...";

        var payload = JsonUtility.ToJson(new Req { sessionId = sessionId, message = message });
        var bytes = Encoding.UTF8.GetBytes(payload);

        using (var req = new UnityWebRequest(endpoint, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                if (outputText != null) outputText.text = "AI error: " + req.error;
                Debug.LogError("AI HTTP error: " + req.error + " | " + req.downloadHandler.text);
                yield break;
            }

            var json = req.downloadHandler.text;
            Debug.Log("AI raw: " + json);

            Res res = null;
            try { res = JsonUtility.FromJson<Res>(json); }
            catch
            {
                if (outputText != null) outputText.text = "AI parse error";
                Debug.LogError("AI JSON parse error: " + json);
                yield break;
            }

            if (res != null && res.ok)
            {
                if (outputText != null) outputText.text = string.IsNullOrWhiteSpace(res.reply) ? "(pusto)" : res.reply;
            }
            else
            {
                if (outputText != null) outputText.text = "AI failed";
                Debug.LogError("AI failed details: " + json);
            }
        }
    }
}
