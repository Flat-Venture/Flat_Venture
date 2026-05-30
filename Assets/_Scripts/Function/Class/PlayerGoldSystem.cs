using System;
using UnityEngine;

public class PlayerGoldSystem : MonoBehaviour
{
    public float currentGold;

    public Action<float> OnGoldChanged;

    public void AddGold(float amount)
    {
        currentGold += amount;
        OnGoldChanged?.Invoke(currentGold);
    }

    public void ResetGold()
    {
        currentGold = 0;
        OnGoldChanged?.Invoke(currentGold);
    }
}
