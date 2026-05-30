using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SynergyManager : MonoBehaviour
{
    public static SynergyManager Instance;

    // 모든 시너지 정보를 가지고 있는 SO 이용
    [SerializeField]
    AllSynergiesData synergyData;

    // 시너지 정보를 시너지 타입으로 편하기 찾기위한 Dictionary
    Dictionary<Enums.ItemSynergy, SynergyData> synergyDictionary =
        new Dictionary<Enums.ItemSynergy, SynergyData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        synergyDictionary = synergyData.allSyneries.ToDictionary(x => x.synergyType, x => x);
    }

    // 시너지 타입을 통한 시너지 데이터 반환
    public SynergyData GetSynergyData(Enums.ItemSynergy synergy)
    {
        if (!synergyDictionary.TryGetValue(synergy, out var data))
        {
            Debug.LogError("SynergyData를 찾을 수 없음");
            return null;
        }
        return data;
    }
}
