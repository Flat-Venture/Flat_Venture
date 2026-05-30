using UnityEngine;

public class DungeonOutCheck : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DungeonManager.Instance.ExitDungeon();
    }
}
