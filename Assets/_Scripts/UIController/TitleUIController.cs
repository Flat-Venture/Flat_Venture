using UnityEngine;
using UnityEngine.UI;

public class TitleUIController : MonoBehaviour
{
    public Button startButton;
    public Button settingButton;
    public Button exitButton;

    SceneLoaderManager sceneLoaderManager;
    SoundManager soundManager;
    UIManager uiManager;

    void Start()
    {
        sceneLoaderManager = SceneLoaderManager.Instance;
        soundManager = SoundManager.Instance;
        uiManager = UIManager.Instance;

        startButton.onClick.AddListener(startGame);
        settingButton.onClick.AddListener(settingButtonClick);
        exitButton.onClick.AddListener(QuitGame);
    }

    public void startGame()
    {
        soundManager.PlaySFX("buttonClick");
        sceneLoaderManager.LoadScene(SceneNames.Town);
    }

    public void settingButtonClick()
    {
        soundManager.PlaySFX("buttonClick");
        uiManager.settingUIController.ShowSettingPanel();
    }

    public void QuitGame()
    {
        soundManager.PlaySFX("buttonClick");
        UnityEditor.EditorApplication.isPlaying = false;
    }

}
