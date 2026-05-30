using System;
using System.IO;
using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance;

    public UserData userData;                                       // 유저 데이터 저장용 클래스
    public SettingData settingData;                                 // 설정 데이터 저장용 클래스 (볼륨)

    private readonly string userDataFile = "userData.json";         // 유저 데이터 파일명
    private readonly string settingDataFile = "settingData.json";   // 설정 데이터 파일명

    private string userDataPath;                                    // 유저 데이터 저장 파일 경로
    private string settingDataPath;                                 // 설정 데이터 저장 파일 경로

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

        // 파일 경로 설정
        userDataPath = Path.Combine(Application.persistentDataPath, userDataFile);
        settingDataPath = Path.Combine(Application.persistentDataPath, settingDataFile);

        // 저장된 데이터 로드
        LoadUserData();
        LoadSettingData();
    }

    // 유저 데이터 저장
    public void SaveUserData()
    {
        try
        {
            // C# 클래스 -> Json 문자열로 변환
            string json = JsonUtility.ToJson(userData, true);

            // Json 문자열 => 파일로 저장
            File.WriteAllText(userDataPath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"저장 실패: {e.Message}");  
        }
    }

    // 유저 데이터 불러오기
    public bool LoadUserData()
    {
        // 파일이 있는지 확인
        if (!File.Exists(userDataPath))
        {
            Debug.Log("저장 파일 없음");
            return false;
        }

        try
        {
            // 파일에서 Json 문자열 읽기
            string json = File.ReadAllText(userDataPath);

            // Json 문자열 => 클래스로 변환
            userData = JsonUtility.FromJson<UserData>(json);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"불러오기 실패: {e.Message}");
            return false;
        }
    }

    // 설정 데이터 저장
    public void SaveSettingData()
    {
        try
        {
            string json = JsonUtility.ToJson(settingData, true);

            File.WriteAllText(settingDataPath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"저장 실패: {e.Message}");
        }
    }

    // 설정 데이터 로드
    public bool LoadSettingData()
    {
        if (!File.Exists(settingDataPath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(settingDataPath);

            settingData = JsonUtility.FromJson<SettingData>(json);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"불러오기 실패: {e.Message}");
            return false;
        }
    }

    // 유저 저장 데이터만 삭제
    public void DeletUserData()
    {
        if (File.Exists(userDataPath))
        {
            File.Delete(userDataPath);
        }
        userData = new UserData();
    }

    // 설정 저장 데이터만 삭제
    public void DeleteSettingData()
    {
        if (File.Exists(settingDataPath))
        {
            File.Delete(settingDataPath);
        }
        settingData = new SettingData();
    }

    // 저장 파일 모두 삭제
    public void DeleteAllSaveData()
    {
        if (File.Exists(userDataPath))
        {
            File.Delete(userDataPath);
        }

        if (File.Exists(settingDataPath))
        {
            File.Delete(settingDataPath);
        }

        InitSaveData();
    }

    // 저장 데이터 클래스 초기화
    public void InitSaveData()
    {
        userData = new UserData();
        settingData = new SettingData();
    }
}
