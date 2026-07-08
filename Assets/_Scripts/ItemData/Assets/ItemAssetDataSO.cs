using UnityEngine;

namespace FlatVenture.ItemData
{
    // item_id와 연결되는 Unity 에셋 참조 데이터입니다.
    // 아이템의 수치/효과는 CSV에서 관리하고, 아이콘/사운드/프리팹 같은 에셋만 여기에서 연결합니다.
    [CreateAssetMenu(fileName = "ItemAssetData", menuName = "Flat Venture/Item Data/Item Asset Data")]
    public sealed class ItemAssetDataSO : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private Sprite icon;
        [SerializeField] private AudioClip pickupSfx;
        [SerializeField] private AudioClip useSfx;
        [SerializeField] private GameObject prefab;
        [SerializeField] private GameObject vfxPrefab;

        public string ItemId
        {
            get { return itemId; }
        }

        public Sprite Icon
        {
            get { return icon; }
        }

        public AudioClip PickupSfx
        {
            get { return pickupSfx; }
        }

        public AudioClip UseSfx
        {
            get { return useSfx; }
        }

        public GameObject Prefab
        {
            get { return prefab; }
        }

        public GameObject VfxPrefab
        {
            get { return vfxPrefab; }
        }

        // Editor 자동 생성 도구에서 item_id를 채울 때 사용합니다.
        public void SetItemIdForEditor(string value)
        {
            itemId = value;
        }

        // Editor 자동 연결 도구에서 아이콘을 채울 때 사용합니다.
        public void SetIconForEditor(Sprite value)
        {
            icon = value;
        }
    }
}
