using UnityEngine;
using UnityEngine.SceneManagement;

public class LessonButton : MonoBehaviour
{
    [SerializeField] private string lessonId;

    public void StartLesson()
    {
        PlayerPrefs.SetString("lessonId", lessonId);
        SceneManager.LoadScene("MainScene"); // scena z AR
    }
}
