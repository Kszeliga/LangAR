using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMenu : MonoBehaviour
{
    public void ChangeScene()
    {
        SceneManager.LoadScene("MainMenu"); // Nazwa sceny w cudzys³owach
    }
}