using UnityEngine;
using UnityEngine.Android;

public class AndroidSTT : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject speechRecognizer;
    private AndroidJavaObject recognizerIntent;
    private AndroidJavaProxy listenerProxy;
#endif

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("STT: Start()");
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
#endif
    }

    public void StartListening()
    {
        Debug.Log("KLIK DZIALA - StartListening odpalone");

#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("STT: StartListening()");

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.Log("STT: brak permisji mikrofonu -> proszê o permisjê");
            Permission.RequestUserPermission(Permission.Microphone);
            return;
        }

        if (speechRecognizer == null)
        {
            Debug.Log("STT: speechRecognizer null -> init");
            InitSpeechRecognizer();
        }

        if (speechRecognizer == null)
        {
            Debug.LogError("STT: init nieudany, speechRecognizer dalej null");
            return;
        }

        var activity = GetActivity();
        activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            try
            {
                speechRecognizer.Call("startListening", recognizerIntent);
                Debug.Log("STT: startListening wys³ane");
            }
            catch (System.Exception e)
            {
                Debug.LogError("STT: startListening EXCEPTION: " + e);
            }
        }));
#else
        Debug.Log("STT: dzia³a tylko na Androidzie (Build & Run).");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void InitSpeechRecognizer()
    {
        try
        {
            var activity = GetActivity();

            // sprawdŸ czy w ogóle jest dostêpne rozpoznawanie mowy
            bool available = new AndroidJavaClass("android.speech.SpeechRecognizer")
                .CallStatic<bool>("isRecognitionAvailable", activity);

            Debug.Log("STT: isRecognitionAvailable = " + available);
            if (!available)
            {
                Debug.LogError("STT: Brak us³ugi rozpoznawania mowy na tym telefonie / wy³¹czona us³uga Google.");
                return;
            }

            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try
                {
                    speechRecognizer = new AndroidJavaClass("android.speech.SpeechRecognizer")
                        .CallStatic<AndroidJavaObject>("createSpeechRecognizer", activity);

                    recognizerIntent = new AndroidJavaObject("android.content.Intent",
                        "android.speech.action.RECOGNIZE_SPEECH");
                    recognizerIntent.Call<AndroidJavaObject>("putExtra",
                        "android.speech.extra.LANGUAGE_MODEL", "free_form");
                    recognizerIntent.Call<AndroidJavaObject>("putExtra",
                        "android.speech.extra.LANGUAGE", "pl-PL");
                    recognizerIntent.Call<AndroidJavaObject>("putExtra",
                        "android.speech.extra.PARTIAL_RESULTS", true);

                    listenerProxy = new RecognitionListenerProxy();
                    speechRecognizer.Call("setRecognitionListener", listenerProxy);

                    Debug.Log("STT: init OK (speechRecognizer != null = " + (speechRecognizer != null) + ")");
                }
                catch (System.Exception e)
                {
                    Debug.LogError("STT: INIT EXCEPTION: " + e);
                }
            }));
        }
        catch (System.Exception e)
        {
            Debug.LogError("STT: InitSpeechRecognizer outer EXCEPTION: " + e);
        }
    }

    private AndroidJavaObject GetActivity()
    {
        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
    }

    private class RecognitionListenerProxy : AndroidJavaProxy
    {
        public RecognitionListenerProxy() : base("android.speech.RecognitionListener") { }

        void onReadyForSpeech(AndroidJavaObject @params) { Debug.Log("STT: ready"); }
        void onBeginningOfSpeech() { Debug.Log("STT: begin"); }
        void onRmsChanged(float rmsdB) { }
        void onBufferReceived(byte[] buffer) { }
        void onEndOfSpeech() { Debug.Log("STT: end"); }

        void onError(int error) { Debug.LogError("STT: ERROR = " + error); }

        void onResults(AndroidJavaObject results)
        {
            string text = ExtractBest(results);
            Debug.Log("STT: RESULT = " + text);
        }

        void onPartialResults(AndroidJavaObject partialResults)
        {
            string text = ExtractBest(partialResults);
            if (!string.IsNullOrEmpty(text))
                Debug.Log("STT: PARTIAL = " + text);
        }

        void onEvent(int eventType, AndroidJavaObject @params) { }

        private string ExtractBest(AndroidJavaObject bundle)
        {
            var list = bundle.Call<AndroidJavaObject>("getStringArrayList", "results_recognition");
            if (list == null) return "";
            int size = list.Call<int>("size");
            if (size <= 0) return "";
            return list.Call<string>("get", 0);
        }
    }
#endif

    void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (speechRecognizer != null)
            {
                speechRecognizer.Call("destroy");
                speechRecognizer = null;
            }
        }
        catch { }
#endif
    }
}
