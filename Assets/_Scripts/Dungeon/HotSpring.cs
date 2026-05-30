using UnityEngine;
using System.Collections;

public class HotSpring : MonoBehaviour
{
    [Header("회복 설정")]
    [Tooltip("한 번 회복할 때 채워줄 체력 양")]
    public float healAmount = 5f;
    [Tooltip("회복 주기 (초)")]
    public float healInterval = 1.0f;

    [Header("인벤토리 설정")]
    [Tooltip("연결할 인벤토리 UI 패널 (Canvas 내의 오브젝트)")]
    public GameObject inventoryPanel;

    private Coroutine healCoroutine;
    private bool isPlayerInside = false; // 플레이어가 온천 안에 있는지 여부

    bool isSwaped = false;

    void Update()
    {
        // 플레이어가 온천 안에 있고 F 키를 눌렀을 때
        if (isPlayerInside && Input.GetKeyDown(KeyCode.F))
        {
            if (!isSwaped)
            {
                isSwaped = true;
                ModeManager.Instance.SetMode(Enums.InventoryMode.Swap);
                if (!UIManager.Instance.inventoryUIController.gameObject.activeSelf)
                {
                    UIManager.Instance.ToggleInventory();
                }
                else
                {
                    UIManager.Instance.ToggleInventory();
                    UIManager.Instance.ToggleInventory();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("<color=cyan><b>[온천]</b> 플레이어가 진입했습니다. (F키로 인벤토리 가능)</color>");
            isPlayerInside = true;

            if (healCoroutine == null)
            {
                healCoroutine = StartCoroutine(CoHealPlayer());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("<color=yellow><b>[온천]</b> 플레이어가 나갔습니다. 인벤토리를 닫습니다.</color>");
            isPlayerInside = false;

            if (healCoroutine != null)
            {
                StopCoroutine(healCoroutine);
                healCoroutine = null;
            }
        }
    }

    private IEnumerator CoHealPlayer()
    {
        while (true)
        {
            if (Player.Instance != null)
            {
                Player.Instance.Heal(healAmount);
                Debug.Log($"<color=green>[온천 회복 중]</color> 현재 체력: {Player.Instance.currentHp}");
            }
            yield return new WaitForSeconds(healInterval);
        }
    }
}