using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Backend")]
    [SerializeField]
    private string startLessonUrl =
        "https://us-central1-langar-ea840.cloudfunctions.net/startLesson";

    private const string PREF_SESSION_ID = "sessionId";
    private const string PREF_INTRO_B64 = "introAudioBase64";
    private const string PREF_INTRO_TXT = "introText";

    [Serializable]
    private class Req
    {
        public string lessonId;
    }

    [Serializable]
    private class Res
    {
        public bool ok;
        public string sessionId;
        public string introText;
        public string introAudioBase64;
        public string error;
        public string details;
    }

   
    public void StartLesson(string lessonId)
    {
        StartCoroutine(StartLessonRoutine(lessonId));
    }

    private IEnumerator StartLessonRoutine(string lessonId)
    {
        var json = JsonUtility.ToJson(new Req { lessonId = lessonId });
        var bytes = Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(startLessonUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("startLesson HTTP error: " + req.error + " | " + req.downloadHandler.text);
                yield break;
            }

            var raw = req.downloadHandler.text;
            var res = JsonUtility.FromJson<Res>(raw);

            if (res == null || !res.ok || string.IsNullOrEmpty(res.sessionId))
            {
                Debug.LogError("startLesson failed: " + raw);
                yield break;
            }

            // Zapis sesji
            PlayerPrefs.SetString(PREF_SESSION_ID, res.sessionId);

            // Zapis intro 
            PlayerPrefs.SetString(PREF_INTRO_TXT, res.introText ?? "");
            PlayerPrefs.SetString(PREF_INTRO_B64, res.introAudioBase64 ?? "");
            PlayerPrefs.Save();

            Debug.Log("Saved sessionId=" + res.sessionId + " introB64.len=" + (res.introAudioBase64 == null ? 0 : res.introAudioBase64.Length));

            //ladujemy scene rozmowy
            SceneManager.LoadScene("MainScene");
        }
    }

    // X - wyjscie
    public void ExitApp()
    {
        Application.Quit();
    }
}
