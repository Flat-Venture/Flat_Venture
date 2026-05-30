using UnityEngine;
using UnityEngine.UI;

public class GroundTelegraph : MonoBehaviour
{
    [Header("UI - 바깥(최종 범위 표시)")]
    public Image outer;                 
    public RectTransform outerRect;

    [Header("UI - 안쪽(차징 진행 표시)")]
    public Image inner;               
    public RectTransform innerRect;

    [Header("차징")]
    public float chargeTime = 1.5f;     // 표식 생성 후 폭발까지 시간
    public float radius = 3f;           // 실제 폭발 반경

    //월드 1유닛당 UI 크기 보정값 (캔버스 스케일에 맞게 조절)"
    public float worldToUIScale = 1f;

    //안쪽 원 시작 크기 비율(0~1)
    [Range(0f, 1f)]
    public float innerStartRatio = 0f;

    float timer;
    bool charging;

    Vector2 outerTargetSize;   // 최종 외곽 크기
    Vector2 innerStartSize;    // 안쪽 시작 크기
    Vector2 innerTargetSize;   // 안쪽 최종 크기

    void Awake()
    {
        if (outerRect == null && outer != null) outerRect = outer.rectTransform;
        if (innerRect == null && inner != null) innerRect = inner.rectTransform;
    }

    public void Setup(float radius, float chargeTime)
    {
        this.radius = radius;
        this.chargeTime = chargeTime;

        float diameter = radius * 2f * worldToUIScale;
        outerTargetSize = new Vector2(diameter, diameter);
        innerTargetSize = outerTargetSize;

        innerStartSize = innerTargetSize * innerStartRatio;

        // 바깥 원은 처음부터 최종 크기로 고정
        if (outerRect != null) outerRect.sizeDelta = outerTargetSize;

        // 안쪽 원은 시작 크기
        if (innerRect != null) innerRect.sizeDelta = innerStartSize;
    }

    public void StartCharge()
    {
        charging = true;
        timer = 0f;

        if (outer != null) outer.gameObject.SetActive(true);
        if (inner != null) inner.gameObject.SetActive(true);
    }

    void Update()
    {
        if (!charging || innerRect == null) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / chargeTime);

        // 안쪽 원만 점점 커짐
        innerRect.sizeDelta = Vector2.Lerp(innerStartSize, innerTargetSize, t);

        // fillAmount 방식도 가능:
        // if (inner != null) inner.fillAmount = t;
    }

    public bool IsDone() => charging && timer >= chargeTime;

    public void StopAndHide()
    {
        charging = false;
        timer = 0f;

        if (outer != null) outer.gameObject.SetActive(false);
        if (inner != null) inner.gameObject.SetActive(false);

        // 리셋(다음 사용 대비)
        if (innerRect != null) innerRect.sizeDelta = innerStartSize;
    }
}
