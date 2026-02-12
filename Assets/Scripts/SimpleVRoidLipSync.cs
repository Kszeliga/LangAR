using UnityEngine;
using UniVRM10;

public class SimpleVRoidLipSync : MonoBehaviour
{
    [Header("VRM 1.0")]
    public Vrm10Instance vrm;              // auto znajdzie jak puste

    [Header("AUDIO (TO MUSI BYÆ TEN CO GRA!)")]
    public AudioSource audioSource;        //  AudioSource z BackendPingRunner 

    [Header("LIPSYNC")]
    public float sensitivity = 60f;        // jak usta ma³o ruszaj¹ -> zwiêksz (np 120)
    public float smoothing = 14f;          // jak dr¿y -> zwiêksz (np 20)

    [Header("BLINK TEST")]
    public bool blinkAlways = true;        //  twarz dzia³a
    public float blinkEvery = 3.0f;        // co ile sekund mruga

    float t;
    float mouthW;

    void Awake()
    {
        if (!vrm) vrm = GetComponent<Vrm10Instance>();
        if (!vrm) vrm = GetComponentInChildren<Vrm10Instance>(true);

        if (!vrm)
        {
            Debug.LogError("SimpleVRoidLipSync: BRAK Vrm10Instance na tym obiekcie.");
            enabled = false;
            return;
        }

        
        if (!audioSource)
        {
            audioSource = FindAnyObjectByType<AudioSource>();
        }

        Debug.Log("SimpleVRoidLipSync: VRM=" + vrm.gameObject.name +
                  " | Audio=" + (audioSource ? audioSource.gameObject.name : "NULL"));
    }

    void LateUpdate()
    {
        // --- BLINK (dzia³a zawsze) ---
        if (blinkAlways)
        {
            t += Time.deltaTime;
            float phase = Mathf.Repeat(t, blinkEvery);

            float bw = 0f;
            if (phase < 0.08f) bw = Mathf.InverseLerp(0f, 0.08f, phase);          // close
            else if (phase < 0.16f) bw = Mathf.InverseLerp(0.16f, 0.08f, phase);  // open

            vrm.Runtime.Expression.SetWeight(ExpressionKey.Blink, bw);
        }

        float v = 0f;

        if (audioSource && audioSource.isPlaying)
        {
            float rms = GetRms(audioSource);
            v = Mathf.Clamp01(rms * sensitivity);
        }

        // wyg³adzenie
        mouthW = Mathf.Lerp(mouthW, v, 1f - Mathf.Exp(-smoothing * Time.deltaTime));

        // FAKE PHONEM MIX (anime-style)
        vrm.Runtime.Expression.SetWeight(ExpressionKey.Aa, mouthW);
        vrm.Runtime.Expression.SetWeight(ExpressionKey.Ih, mouthW * 0.35f);
        vrm.Runtime.Expression.SetWeight(ExpressionKey.Ou, mouthW * 0.25f);
        vrm.Runtime.Expression.SetWeight(ExpressionKey.Ee, mouthW * 0.15f);
        vrm.Runtime.Expression.SetWeight(ExpressionKey.Oh, mouthW * 0.2f);

    }

    float GetRms(AudioSource a)
    {
        float[] s = new float[256];
        a.GetOutputData(s, 0);
        double sum = 0;
        for (int i = 0; i < s.Length; i++) sum += s[i] * s[i];
        return Mathf.Sqrt((float)(sum / s.Length));
    }
}
