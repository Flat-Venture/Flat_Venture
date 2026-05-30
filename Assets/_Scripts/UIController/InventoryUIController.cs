using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIController : MonoBehaviour
{
    [Header("인벤토리 UI 정보")]
    public GameObject InventoryPanel;               // 인벤토리 패널 오브젝트
    public GameObject slotPrefab;                   // 슬롯 프리팹
    public List<InventorySlotUI> inventorySlots;    // 인벤토리 슬롯 UI 리스트
    public Sprite emptyItemIconImage;               // 아이템 슬롯이 비었을 때, 넣을 이미지

    InventoryManager inventoryManager;
    UIManager uiManager;

    // InventoryUIController 초기화
    public void InitInventoryUIController()
    {
        inventoryManager = InventoryManager.Instance;
        uiManager = UIManager.Instance;

        ClearAndCreateSlots();
        UpdateItemIcon();
    }

    // 슬롯 생성 및 초기화 (콜백 함수 연결)
    void ClearAndCreateSlots()
    {
        foreach(Transform child in InventoryPanel.transform)
        {
            Destroy(child.gameObject);
        }
        inventorySlots.Clear();

        // 슬롯 생성
        for (int i = 0; i < inventoryManager.inventory.Length; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, InventoryPanel.transform);
            newSlot.name = $"Slot_{i}";

            InventorySlotUI slotUI = newSlot.GetComponent<InventorySlotUI>();

            slotUI.SetUp(i);
            inventorySlots.Add(slotUI);
        }
    }

    // 인벤토리 슬롯 아이콘 업데이트
    public void UpdateItemIcon()
    {
        for (int i = 0; i < inventoryManager.inventory.Length; i++)
        {
            // 나중에 reinforcedSlots을 얻는 함수로 바꿔도 될듯
            inventorySlots[i].reinforceLevel.text = "+" + inventoryManager.reinforcedSlots[i].ToString();
            if (inventoryManager.inventory[i] != null)
            {
                inventorySlots[i].itemIcon.sprite = inventoryManager.inventory[i].iIcon;
            }
            else
            {
                inventorySlots[i].itemIcon.sprite = emptyItemIconImage;
            }
        }
    }

    // 모든 인벤토리 슬롯 체크(클릭 했을 때 슬롯을 덮는 이미지) 해제
    public void UnCheckAllSlot()
    {
        foreach(InventorySlotUI slotUI in inventorySlots)
        {
            slotUI.clickedCheck.enabled = false;
        }
    }
}