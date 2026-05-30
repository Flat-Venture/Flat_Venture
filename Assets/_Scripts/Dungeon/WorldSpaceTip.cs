using UnityEngine;
using TMPro;

public class WorldSpaceTip : MonoBehaviour
{
    private TextMeshPro textMesh;
    private Transform mainCamera;

    void Awake()
    {
        // 기존 TMP 가져오기만 함
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
            Debug.LogError("WorldSpaceTip: TextMeshPro 컴포넌트가 필요합니다!");
    }

    void Start()
    {
        mainCamera = Camera.main.transform;
    }

    void Update()
    {
        // 카메라를 바라보도록 고정
        if (mainCamera != null)
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.position);
    }

    // 필요하면 외부에서 직접 TMP에 접근할 수 있게 프로퍼티 제공
    public TextMeshPro GetTMP()
    {
        return textMesh;
    }
}
