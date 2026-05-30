using UnityEngine;

public class ModeManager : MonoBehaviour
{
    public static ModeManager Instance;

    Enums.InventoryMode currentMode = Enums.InventoryMode.None;

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

    public Enums.InventoryMode GetCurrentMode()
    {
        return currentMode;
    }

    public void SetMode(Enums.InventoryMode newMode)
    {
        if(currentMode == Enums.InventoryMode.RankUp && newMode == Enums.InventoryMode.RanKUpWithSynergy)
        {
            Debug.Log("현재 등급업 모드 중. 시너지 효과 유지 불가");
            return;
        }

        if (currentMode == Enums.InventoryMode.RanKUpWithSynergy && newMode == Enums.InventoryMode.RankUp)
        {
            Debug.Log("현재 시너지 유지 등급업 모드중. 일반 등급업 모드로 변경 불가");
            return;
        }

        if (currentMode != newMode)
        {
            currentMode = newMode;
        }
    }
}
