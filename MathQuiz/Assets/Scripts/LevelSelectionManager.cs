using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectionManager : MonoBehaviour
{
    public void LoadLevel(int levelIndex) => SceneManager.LoadScene("Level_" + levelIndex);
}
