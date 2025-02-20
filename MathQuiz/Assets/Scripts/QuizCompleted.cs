using UnityEngine;
using UnityEngine.SceneManagement;

public class QuizCompleted : MonoBehaviour
{
    public void BoraBill() => SceneManager.LoadScene("LevelSelection");
}
