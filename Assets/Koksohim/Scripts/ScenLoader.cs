using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenLoader : MonoBehaviour
{
    private int sceneIndex;

    public void SetSceneIndex(int index)
    {
        sceneIndex = index;
    }

    public void LoadSceneByIndex()
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
