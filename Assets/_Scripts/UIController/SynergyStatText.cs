using TMPro;
using UnityEngine;

public class SynergyStatText : MonoBehaviour
{
    public TextMeshProUGUI statText;    // 스탯을 표시해 줄 텍스트

    public void Setup(int currentSynergyLevel, int synergyLevel, SynergyData synergyData)
    {
        statText.text = "(" + synergyLevel.ToString() + ")" +
            ItemDataHelper.GetSynergyStatsDescription(synergyData, synergyLevel);
        if (currentSynergyLevel >= synergyLevel)
        {
            statText.color = Color.green;
        }
        else
        {
            statText.color = Color.white;
        }
    }
}
