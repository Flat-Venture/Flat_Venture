using UnityEngine;

namespace FlatVenture.ItemData
{
    // 씬에서 CSV 로딩을 빠르게 확인하기 위한 테스트용 컴포넌트입니다.
    // 빈 GameObject에 붙여두면 Awake에서 StreamingAssets의 CSV를 읽습니다.
    public sealed class GameDataLoaderBehaviour : MonoBehaviour
    {
        public GameDataCatalog Catalog { get; private set; }

        // 씬 시작 시 CSV 전체를 로드하고 아이템 개수를 로그로 출력합니다.
        private void Awake()
        {
            Catalog = GameDataCsvLoader.LoadFromStreamingAssets();
            Debug.Log("Game data loaded. Item count: " + Catalog.items.Count);
        }
    }
}
