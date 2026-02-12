using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class BackendPing : MonoBehaviour
{
    public string url = "https://us-central1-langar-ea840.cloudfunctions.net/audioTest";

    public void Ping()
    {
        StartCoroutine(PingCoroutine());
    }

    IEnumerator PingCoroutine()
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("PING ERROR: " + req.error);
            }
            else
            {
                Debug.Log("PING OK: " + req.downloadHandler.text);
            }
        }
    }
}
