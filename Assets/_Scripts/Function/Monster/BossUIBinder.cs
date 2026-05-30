using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossUIBinder : MonoBehaviour
{
    public static BossUIBinder Instance;

    [Header("Root")]
    public GameObject root;

    [Header("Texts")]
    public TextMeshProUGUI bossNameText;

    [Header("HP")]
    public Image hpFill;
    public Image hpDelayFill;   

    [Header("Stun")]
    public Image stunFill;        
    public Image stunDelayFill;   

    [Header("Smoothing")]
    public float hpFrontSpeed = 10f;
    public float hpDelaySpeed = 2.5f;

    public float stunFrontSpeed = 8f;
    public float stunDelaySpeed = 6f;

    BossBase boss;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (root != null) root.SetActive(false);
    }


    void Update()
    {
        if (boss == null || !boss.IsAlive)
        {
            if (root != null) root.SetActive(false);
            boss = null;
            return;
        }

        if (root != null && !root.activeSelf) root.SetActive(true);

        float hpTarget = boss.Hp01;
        float stunTarget = boss.Stun01;

        // HP 앞바: 부드럽게
        if (hpFill != null) hpFill.fillAmount = Mathf.Lerp(hpFill.fillAmount, hpTarget, Time.deltaTime * hpFrontSpeed);

        // HP 딜레이: 내려갈 때 느리게 / 올라갈 때 빠르게
        if (hpDelayFill != null)
        {
            float cur = hpDelayFill.fillAmount;
            float spd = (cur > hpTarget) ? hpDelaySpeed : hpFrontSpeed;
            hpDelayFill.fillAmount = Mathf.Lerp(cur, hpTarget, Time.deltaTime * spd);
        }

        // 스턴: 채우기/0으로 떨어지기 둘 다 부드럽게
        if (stunFill != null) stunFill.fillAmount = Mathf.Lerp(stunFill.fillAmount, stunTarget, Time.deltaTime * stunFrontSpeed);

        if (stunDelayFill != null) stunDelayFill.fillAmount = Mathf.Lerp(stunDelayFill.fillAmount, stunTarget, Time.deltaTime * stunDelaySpeed);
    }

    public void Bind(BossBase b)
    {
        boss = b;

        if (root != null) root.SetActive(true);

        if (bossNameText != null) bossNameText.text = b.BossName;

        float hp = b.Hp01;
        float stun = b.Stun01;

        if (hpFill != null) hpFill.fillAmount = hp;
        if (hpDelayFill != null) hpDelayFill.fillAmount = hp;

        if (stunFill != null) stunFill.fillAmount = stun;
        if (stunDelayFill != null) stunDelayFill.fillAmount = stun;
    }

    public void Unbind()
    {
        boss = null;
        if (root != null) root.SetActive(false);
    }
}
