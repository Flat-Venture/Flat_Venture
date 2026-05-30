using UnityEngine;
using UnityEngine.UI;

public class PlayerExpBarUI : MonoBehaviour
{
    public Image expBar;
    public float fillSpeed = 1f;

    PlayerLevelSystem levelSystem;


    private void Start()
    {
        if (Player.Instance == null)
        {
            Debug.LogError("Player인스턴스 없음");
        }
        levelSystem = Player.Instance.levelSystem;
        levelSystem.OnExpChanged += UpdateBar;
        UpdateBar(levelSystem.currentExp, levelSystem.GetRequiredExp(levelSystem.currentLevel));
    }

    private void OnDisable()
    {
        levelSystem.OnExpChanged -= UpdateBar;
    }

    void UpdateBar(float current, float required)
    {
        if (required <= 0f)
        {
            expBar.fillAmount = 0f;
            return;
        }

        expBar.fillAmount = current / required;
    }
}
