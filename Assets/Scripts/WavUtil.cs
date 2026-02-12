using System;
using System.IO;
using UnityEngine;

public static class WavUtil
{
    public static byte[] FromAudioClip(AudioClip clip, string fileName)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        // 16-bit PCM
        byte[] pcm = new byte[samples.Length * 2];
        int offset = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            short s = (short)Mathf.Clamp(samples[i] * 32767f, short.MinValue, short.MaxValue);
            pcm[offset++] = (byte)(s & 0xff);
            pcm[offset++] = (byte)((s >> 8) & 0xff);
        }

        int hz = clip.frequency;
        short channels = (short)clip.channels;
        short bitsPerSample = 16;
        int byteRate = hz * channels * (bitsPerSample / 8);

        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            // RIFF header
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + pcm.Length);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1); // PCM
            bw.Write(channels);
            bw.Write(hz);
            bw.Write(byteRate);
            bw.Write((short)(channels * (bitsPerSample / 8))); // block align
            bw.Write(bitsPerSample);

            // data chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(pcm.Length);
            bw.Write(pcm);

            return ms.ToArray();
        }
    }
}
