using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class ChatBackend : MonoBehaviour
{
    [Header("Cloud Function URL /chat")]
    public string chatUrl = "https://us-central1-langar-ea840.cloudfunctions.net/chat";

    [Header("UI")]
    public TMP_Text outputText;

    [Serializable]
    private class ChatRequest
    {
        public string message;
    }

    [Serializable]
    private class ChatResponse
    {
        public string reply;
        public string echo;
        public string error;
    }

    public void SendChat(string message)
    {
        GetComponent<AIClient>().AskAI(message);
        
    }


    private IEnumerator SendChatCoroutine(string message)
    {
        if (outputText != null) outputText.text = "Wysy³am do backendu...";

        var payload = new ChatRequest { message = message };
        string json = JsonUtility.ToJson(payload);

        using (var req = new UnityWebRequest(chatUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("CHAT HTTP ERROR: " + req.error);
                Debug.LogError("BODY: " + req.downloadHandler.text);
                if (outputText != null) outputText.text = "B³¹d: " + req.error;
                yield break;
            }

            string respText = req.downloadHandler.text;
            Debug.Log("CHAT RESP: " + respText);

            ChatResponse resp = null;
            try { resp = JsonUtility.FromJson<ChatResponse>(respText); } catch { }

            if (resp != null && !string.IsNullOrEmpty(resp.reply))
            {
                if (outputText != null) outputText.text = resp.reply;
            }
            else
            {
                if (outputText != null) outputText.text = respText;
            }
        }
    }
}
