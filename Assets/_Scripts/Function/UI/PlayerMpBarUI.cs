using UnityEngine;
using UnityEngine.UI;

public class PlayerMpBarUI : MonoBehaviour
{
    public Image mpBar;

    public float fillSpeed = 1f;

    private void Start()
    {
        Player.Instance.OnMpChanged += UpdateBar;
    }

    private void OnDisable()
    {
        Player.Instance.OnMpChanged -= UpdateBar;
    }

    void UpdateBar(float current, float max)
    {
        if (max <= 0f)
        {
            mpBar.fillAmount = 0f;
            return;
        }

        mpBar.fillAmount = current / max;
    }
}
