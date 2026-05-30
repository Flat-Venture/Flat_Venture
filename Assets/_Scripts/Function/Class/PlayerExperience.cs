using UnityEngine;

public class PlayerExperience : MonoBehaviour
{
    public float totalFinalExpGained;

    public float baseExpMultiplier = 1f;        //기본 경험치 획득 배율 (1 = 100%)
    public float bonusExpMultiplier = 1f;       //보너스 경험치 획득 배율

    PlayerLevelSystem levelSystem;
    Player player;

    private void Start()
    {
        levelSystem = GetComponent<PlayerLevelSystem>();
        player = Player.Instance;
    }

    public void ReceiveRawExp(float rawExp)
    {
        float finalExp = CalculateFinalExp(rawExp);
        AddFinalExp(finalExp);
    }

    float CalculateFinalExp(float rawExp)
    {
        float result = rawExp;

        result *= baseExpMultiplier;
        result *= bonusExpMultiplier;

        //플레이어 스탯 보너스 적용
        result *= (1f + player.finalStats.bonusExpRate);

        return result;
    }

    private void AddFinalExp(float finalExp)
    {
        totalFinalExpGained += finalExp;
        levelSystem.AddExp(finalExp);
    }
}
