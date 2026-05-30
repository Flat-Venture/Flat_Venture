using UnityEngine;
using UnityEngine.UI;

public class PlayerHpBarUI : MonoBehaviour
{
    public Image hpBar;

    public float fillSpeed = 1f;

    private void Start()
    {
        Player.Instance.OnHpChanged += UpdateBar;
    }

    private void OnDisable()
    {
        Player.Instance.OnHpChanged -= UpdateBar;
    }

    void UpdateBar(float current, float max)
    {
        if (max <= 0f)
        {
            hpBar.fillAmount = 0f;
            return;
        }

        hpBar.fillAmount = current / max;
    }
}
