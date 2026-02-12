using UnityEngine;

public class VoiceInput : MonoBehaviour
{
    public bool autoStart = true;
    public int sampleRate = 16000;

    private AudioClip micClip;
    private string micDevice;

    void Start()
    {
        if (autoStart) StartMic();
    }

    public void StartMic()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("Brak uprawnieñ.");
            return;
        }

        micDevice = Microphone.devices[0];
        micClip = Microphone.Start(micDevice, true, 10, sampleRate);
        Debug.Log("MIC START: " + micDevice);
    }

    public void StopMic()
    {
        if (string.IsNullOrEmpty(micDevice)) return;
        Microphone.End(micDevice);
        Debug.Log("MIC STOP");
    }

    void OnDisable()
    {
        StopMic();
    }
}
