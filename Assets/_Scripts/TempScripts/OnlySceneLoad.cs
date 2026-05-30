using UnityEngine;

public class OnlySceneLoad : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SceneLoaderManager.Instance.LoadScene(SceneNames.Title);
    }
}
