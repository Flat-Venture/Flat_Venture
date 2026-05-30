using UnityEngine;

public class PlayerGold : MonoBehaviour
{
    public float totalFinalGoldGained;

    public float baseGoldMultiplier = 1f;
    public float bonusGoldMultiplier = 1f;

    PlayerGoldSystem goldSystem;
    Player player;

    private void Start()
    {
        goldSystem = GetComponent<PlayerGoldSystem>();
        player = Player.Instance;
    }

    public void ReceiveRawGold(float rawGold)
    {
        float finalGold = CalculateFinalGold(rawGold);
        AddFinalGold(finalGold);
    }

    float CalculateFinalGold(float rawGold)
    {
        float result = rawGold;

        result *= baseGoldMultiplier;
        result *= bonusGoldMultiplier;

        //100 % + 추가 골드량 해서 적용
        result *= (1f + player.finalStats.bonusGoldRate);

        return result;
    }
    
    void AddFinalGold(float finalGold)
    {
        totalFinalGoldGained += finalGold;
        goldSystem.AddGold(finalGold);
    }

}


