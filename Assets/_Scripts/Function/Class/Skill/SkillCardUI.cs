using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 카드 하나 UI
/// </summary>
public class SkillCardUI : MonoBehaviour, IPointerEnterHandler
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Button button;

    private SkillCardData cardData;
    private Action<SkillCardData> onClickAction;

    [Header("Hover 연출")]
    RectTransform rect;
    Coroutine hoverRoutine;
    public float hoverAngle = 3f;
    public float hoverTime = 0.1f;
    public Image background;
    public CanvasGroup canvasGroup;

    SkillSelectionUI parentUI;


    private void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    public void SetUp(
    SkillCardData data,
    Action<SkillCardData> onClick,
    SkillSelectionUI parent
    )
    {
        cardData = data;
        onClickAction = onClick;
        parentUI = parent;

        iconImage.sprite = data.icon;
        nameText.text = data.nameText;
        descriptionText.text = data.descriptionText;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        //클릭시 사운드


        parentUI.PlayerSelectEffect(this);

        StartCoroutine(DelayApply());
    }

    IEnumerator DelayApply()
    {
        yield return new WaitForSecondsRealtime(0.25f);
        onClickAction?.Invoke(cardData);
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        if(hoverRoutine != null)
        {
            StopCoroutine(hoverRoutine);
        }
        SoundManager.Instance.PlaySFX("cardPointed");

        hoverRoutine = StartCoroutine(CoHoverRotate());
    }

    IEnumerator CoHoverRotate()
    {
        yield return RotateTo(-hoverAngle);
        yield return RotateTo(hoverAngle);
        yield return RotateTo(0f);
    }

    IEnumerator RotateTo(float targetZ)
    {
        float elapsed = 0f;
        float startZ = rect.localEulerAngles.z;

        if (startZ > 180f) startZ -= 360f;
        
        while(elapsed < hoverTime)
        {
            elapsed += Time.unscaledDeltaTime;                              // time.scale == 0 이라서 
            float z = Mathf.Lerp(startZ, targetZ, elapsed / hoverTime);
            rect.localRotation = Quaternion.Euler(0, 0, z);
            yield return null;
        }

        rect.localRotation = Quaternion.Euler(0, 0, targetZ);
    }

    /// <summary>
    /// 카드 골랐을 때 선택된 카드 코루틴 실행
    /// </summary>
    public void PlaySelected()
    {
        SoundManager.Instance.PlaySFX("cardSelected");
        StopAllCoroutines();
        StartCoroutine(CoScaleTo(1.2f));
    }

    /// <summary>
    /// 카드 골랐을때 선택 안된 카드들 코루틴 실행
    /// </summary>
    public void PlayUnSelected()
    {
        StopAllCoroutines();
        StartCoroutine(CoScaleTo(0.8f));
        SetDimmed(true);
    }

    IEnumerator CoScaleTo(float target)
    {
        float elapsed = 0f;
        float duration = 0.2f;
        Vector3 start = rect.localScale;
        Vector3 end = Vector3.one * target;

        while(elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            rect.localScale = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        rect.localScale = end;
    }

    public void SetDimmed(bool dim)
    {
        canvasGroup.alpha = dim ? 0.5f : 1f;
        background.color = dim ? Color.black : Color.white;
    }
}
