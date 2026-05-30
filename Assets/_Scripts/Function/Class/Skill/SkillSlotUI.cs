using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class SkillSlotUI : MonoBehaviour
{
    [Header("스킬 슬롯")]
    public Image[] slotIcons;                   // SkillSlot1~3 아이콘
    public TextMeshProUGUI[] slotLevelTexts;    // SkillSlot1~3 레벨 텍스트
    public Image[] cooldownMasks;               // 쿨타임 Fill용 이미지
    public TextMeshProUGUI[] cooldownTexts;     // 쿨타임 카운트

    [Header("대쉬 슬롯")]
    public Image dashIcon;                      // dash 아이콘
    public Image dashCooldownMask;              // dash 쿨타임 Fill용 이미지

    private PlayerSkillController skillController;
    public PlayerMove playerMove;


    private void Start()
    {
        skillController = PlayerSkillController.Instance;
        if (skillController != null)
            skillController.OnSkillChanged += Refresh;

        Refresh();
        playerMove = Player.Instance.GetComponent<PlayerMove>();
    }

    private void Update()
    {
        UpdateSkillCooldownUI();
        UpdateDashCooldownUI();
    }

    /// <summary>
    /// 슬롯 UI 전체 갱신
    /// </summary>
    public void Refresh()
    {
        if (skillController == null)
        {
            Debug.LogError("스킬컨트롤러 없음");
            skillController = PlayerSkillController.Instance;
        }
        for (int i = 0; i < slotIcons.Length; i++)
        {
            var skillRuntime = skillController.GetSkillAtSlot(i);

            if (skillRuntime == null || skillRuntime.skillBaseData == null)
            {
                slotIcons[i].enabled = false;
                slotLevelTexts[i].gameObject.SetActive(false);
                cooldownMasks[i].gameObject.SetActive(false);
                cooldownTexts[i].gameObject.SetActive(false);
                continue;
            }
            else
            {
                slotIcons[i].enabled = true;
                slotIcons[i].sprite = skillRuntime.skillBaseData.icon;

                slotLevelTexts[i].gameObject.SetActive(true);
                slotLevelTexts[i].text = $"Lv.{skillRuntime.currentLevel}";
            }
        }
    }

    /// <summary>
    /// 스킬 사용시 쿨타임 적용
    /// </summary>
    void UpdateSkillCooldownUI()
    {
        for (int i = 0; i < cooldownMasks.Length; i++)
        {
            var skill = skillController.GetSkillAtSlot(i);
            if (skill == null || skill.skillBaseData == null)
            {
                cooldownMasks[i].fillAmount = 0f;
                cooldownTexts[i].gameObject.SetActive(false);
                continue;
            }

            float remain = skill.GetCooldownRemaining();
            float total = skill.GetFinalCooldown();

            if (remain > 0f)
            {
                cooldownMasks[i].gameObject.SetActive(true);
                cooldownTexts[i].gameObject.SetActive(true);

                float fill = remain / total;
                cooldownMasks[i].fillAmount = fill;
                cooldownTexts[i].text = Mathf.CeilToInt(remain).ToString();
            }
            else
            {
                cooldownMasks[i].gameObject.SetActive(false);
                cooldownTexts[i].gameObject.SetActive(false);
            }
        }
    }

    void UpdateDashCooldownUI()
    {
        float remain = playerMove.GetDashCooldownRemaining();
        float total = playerMove.dashCooldown;

        if (remain > 0f)
        {
            dashCooldownMask.gameObject.SetActive(true);
            dashCooldownMask.fillAmount = remain / total;
        }
        else
        {
            dashCooldownMask.gameObject.SetActive(false);
        }

    }


    private void OnDestroy()
    {
        if (skillController != null)
            skillController.OnSkillChanged -= Refresh;
    }
}
