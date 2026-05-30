using UnityEngine;

public class UserDataManager : MonoBehaviour
{
    public static UserDataManager Instance;

    // SaveLoadManager의 userData를 참조
    public UserData userData => SaveLoadManager.Instance.userData;
    public const int MAX_LEVEL = 40;

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

    // 유저 경험치 획득
    public void AddUserExp(float exp)
    {
        if (userData.level >= MAX_LEVEL)
        {
            return;
        }

        userData.currentExp += exp;

        while(userData.currentExp >= userData.maxExp)
        {
            if (userData.level < MAX_LEVEL)
            {
                CheckUserLevelUp();
            }
            else
            {
                userData.currentExp = 0;
                break;
            }
        }
        // 이건 어차피 던전 나올 때, 경험치 획득하고 저장할 것이기 때문에 없어도 됨
        SaveLoadManager.Instance.SaveUserData();        
    }

    // 유저 레벨 업
    public void CheckUserLevelUp()
    {
        userData.currentExp -= userData.maxExp;
        userData.currentExp = (float)System.Math.Round(userData.currentExp, 2);
        userData.level++;

        // 최대 경험치 20% 증가
        userData.maxExp *= 1.2f;
        userData.maxExp = (float)System.Math.Round(userData.maxExp, 2);
    }
}
