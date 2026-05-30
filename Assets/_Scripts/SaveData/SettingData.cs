using UnityEngine;

[System.Serializable]
// 설정 데이터
public class SettingData
{
    // 볼륨 설정
    public float masterVolume;              // 마스터 볼륨
    public float bgmVolume;                 // BGM 볼륨
    public float sfxVolume;                 // SFX 볼륨

    public SettingData()
    {
        masterVolume = 0.7f;
        bgmVolume = 0.5f;
        sfxVolume = 0.5f;
    }
}
