using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class MicRecorder : MonoBehaviour
{
    public string audioUrl = "https://us-central1-langar-ea840.cloudfunctions.net/voiceTurn";
    public int hz = 16000;
    public string micDevice = null;

    private AudioClip clip;
    private bool isRecording = false;
    private float recordStartTime;
    public GameObject LessonComplete;


    // do Button: OnPointerDown
    void Awake()
    {
        if (!GetComponent<AudioSource>()) gameObject.AddComponent<AudioSource>();
    }

    public void StartRecording()
    {
        if (isRecording) return;

#if UNITY_ANDROID && !UNITY_EDITOR
    if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
        UnityEngine.Android.Permission.Microphone))
    {
        UnityEngine.Android.Permission.RequestUserPermission(
            UnityEngine.Android.Permission.Microphone);
        Debug.Log("Requesting mic permission...");
        return;
    }
#endif

        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            Debug.LogError("No mic device");
            return;
        }

        micDevice = Microphone.devices[0];

        //  STAŁE 16 kHz – 
        hz = 16000;

        Debug.Log("MIC START hz=" + hz + " device=" + micDevice);

        clip = Microphone.Start(
            micDevice,
            false,   // loop OFF
            20,      // max length (sekundy)
            hz
        );

        recordStartTime = Time.time;
        isRecording = true;
    }


    // do Button: OnPointerUp
    public void StopAndSend()
    {
        if (!isRecording) return;

        int samplesRecorded = Microphone.GetPosition(micDevice);
        samplesRecorded = Mathf.Clamp(samplesRecorded, 0, clip.samples);

        Microphone.End(micDevice);
        isRecording = false;

        //
        float seconds = samplesRecorded / (float)hz;
        if (seconds < 1.0f) 
        {
            Debug.Log("Too short, skip send: " + seconds.ToString("0.00") + "s");
            return;
        }

        Debug.Log("MIC STOP. hz=" + hz + " samples=" + samplesRecorded);

        StartCoroutine(SendRecorded(samplesRecorded));
    }


    [System.Serializable]
    class VoiceResponse
    {
        public bool ok;
        public string transcript;
        public string replyText;
        public string audioBase64;
    }

    IEnumerator SendRecorded(int samplesRecorded)
    {
        float[] samples = new float[samplesRecorded];
        clip.GetData(samples, 0);

        byte[] pcm = new byte[samples.Length * 2];
        int index = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            short v = (short)Mathf.Clamp(samples[i] * short.MaxValue, short.MinValue, short.MaxValue);
            pcm[index++] = (byte)(v & 0xff);
            pcm[index++] = (byte)((v >> 8) & 0xff);
        }

        string sessionId = PlayerPrefs.GetString("sessionId", "");
        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogError("Brak sessionId");
            yield break;
        }

        string url = audioUrl + "?hz=" + hz + "&sessionId=" + UnityWebRequest.EscapeURL(sessionId);


        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(pcm);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/octet-stream");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"REQ FAIL err='{req.error}' result={req.result} code={req.responseCode} bodyLen={(req.downloadHandler?.text?.Length ?? 0)} body='{req.downloadHandler?.text}'");

                yield break;
            }

            var data = req.downloadHandler.data;
            var json = System.Text.Encoding.UTF8.GetString(data);
            var resp = JsonUtility.FromJson<VoiceResponse>(json);


            Debug.Log("AI TEXT: " + resp.replyText);
            Debug.Log("USER TEXT: " + resp.transcript);


            bool lessonComplete =
                !string.IsNullOrEmpty(resp.replyText) &&
                resp.replyText.ToLower().Contains("lesson complete");

            if (lessonComplete)
            {
                Debug.Log("LESSON COMPLETE DETECTED");
                if (LessonComplete != null)
                    LessonComplete.SetActive(true);

            }


            if (string.IsNullOrEmpty(resp.audioBase64))
            {
                Debug.Log("No audio returned");
                yield break;
            }

            byte[] mp3Data = System.Convert.FromBase64String(resp.audioBase64);
            StartCoroutine(PlayMp3(mp3Data));
        }
    }
    IEnumerator PlayMp3(byte[] mp3Data)
    {
        string path = Application.persistentDataPath + "/ai_reply.mp3";
        System.IO.File.WriteAllBytes(path, mp3Data);

        using (var www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            var clip = DownloadHandlerAudioClip.GetContent(www);
            AudioSource audio = GetComponent<AudioSource>();
            if (!audio) audio = gameObject.AddComponent<AudioSource>();

            audio.clip = clip;
            audio.Play();
            Debug.Log("TTS PLAY isPlaying=" + audio.isPlaying + " on " + gameObject.name);

        }
    }

}
