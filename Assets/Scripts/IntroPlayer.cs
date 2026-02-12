using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class IntroPlayer : MonoBehaviour
{
    [SerializeField] private TMP_Text subtitleText;   // opcjonalnie
    [SerializeField] private AudioSource audioSource; // pod³¹cz z tego samego obiektu

    private const string PREF_INTRO_B64 = "introAudioBase64";
    private const string PREF_INTRO_TXT = "introText";

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        string introText = PlayerPrefs.GetString(PREF_INTRO_TXT, "");
        string b64 = PlayerPrefs.GetString(PREF_INTRO_B64, "");

        if (subtitleText != null) subtitleText.text = introText;

        if (string.IsNullOrEmpty(b64))
        {
            Debug.Log("IntroPlayer: brak introAudioBase64 (OK jeœli lekcja nie zwróci³a)");
            return;
        }

        try
        {
            byte[] mp3 = Convert.FromBase64String(b64);
            StartCoroutine(PlayMp3(mp3));
        }
        catch (Exception e)
        {
            Debug.LogError("IntroPlayer: base64 error: " + e.Message);
        }

        // Wyczyœæ ¿eby nie gra³o drugi raz po powrocie/reloadzie
        PlayerPrefs.DeleteKey(PREF_INTRO_B64);
        PlayerPrefs.DeleteKey(PREF_INTRO_TXT);
        PlayerPrefs.Save();
    }

    private IEnumerator PlayMp3(byte[] mp3Data)
    {
        string path = Application.persistentDataPath + "/intro.mp3";
        System.IO.File.WriteAllBytes(path, mp3Data);

        using (var www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("IntroPlayer: audio load error: " + www.error);
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}
