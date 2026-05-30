using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
// 유저 데이터
public class UserData
{

    public int level;                   // 유저 레벨
    public string nickName;             // 유저 닉네임
    public float currentExp;            // 현재 경험치
    public float maxExp;                // 다음 레벨업까지 필요한 경험치

    public SerializedDictionary<Enums.TraitType, int> traitPoints
        = new SerializedDictionary<Enums.TraitType, int>();

    public int GetRemainPoints()
    {
        int usedPoints = 0;
        foreach (var p in traitPoints.Values)
        {
            usedPoints += p;
        }
        return level - usedPoints;
    }

    // 특정 퀘스트 완료 특정 아이템 획득 등에 추가

    public UserData()
    {
        level = 1;
        nickName = "임시 닉네임";
        currentExp = 0;
        maxExp = 100;
    }
}
