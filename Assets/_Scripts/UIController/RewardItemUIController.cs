using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class RewardItemUIController : MonoBehaviour
{
    [Header("UI 관련 오브젝트")]
    public ItemDescriptionUIController itemDescriptionUIController;
    public Image itemImage;

    Enums.ItemRank randRank;
    public ItemData randItem;

    CinemachineCamera pCam;

    ItemManager itemManager;
    InventoryManager inventoryManager;
    SpawnManager spawnManager;
    Player player;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        itemManager = ItemManager.Instance;
        inventoryManager = InventoryManager.Instance;
        spawnManager = SpawnManager.Instance;
        player = Player.Instance;

        pCam = player.GetComponentInChildren<CinemachineCamera>();

        randRank = itemManager.GetRandomItemRank();
        randItem = itemManager.GetRandomItemDataByRank(randRank);

        itemDescriptionUIController.ShowItemDescription(randItem);
        itemImage.sprite = randItem.iIcon;
    }

    // Update is called once per frame
    void Update()
    {
        LookAtCam();
    }

    void LookAtCam()
    {
        Vector3 lookDir = pCam.transform.forward;

        transform.rotation = Quaternion.LookRotation(lookDir);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            if (!inventoryManager.CheckInventoryIsFull() && !inventoryManager.CheckDuplicateItems(randItem.iId))
            {
                SoundManager.Instance.PlaySFX("itemPickUp");
                inventoryManager.AddItemToInventory(randItem);
                spawnManager.DestroyAllRewards();
            }
        }
    }
}
