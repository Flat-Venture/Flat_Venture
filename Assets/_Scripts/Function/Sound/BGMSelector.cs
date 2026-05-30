using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BGMSelector : MonoBehaviour
{
    [System.Serializable]
    public struct RoomMapping
    {
        public Enums.RoomType type;
        public List<SoundData> soundData;
    }

    [Header("이 씬에서 재생할 BGM 데이터")]
    [SerializeField]
    private List<RoomMapping> roomMappings;

    private Dictionary<Enums.RoomType, List<SoundData>> bgmDictionary 
        = new Dictionary<Enums.RoomType, List<SoundData>>();

    private void Awake()
    {
        foreach (var mapping in roomMappings)
        {
            if (mapping.soundData != null && !bgmDictionary.ContainsKey(mapping.type))
            {
                List<SoundData> soundDatas = new List<SoundData>();
                foreach(var soundData in mapping.soundData)
                {
                    soundDatas.Add(soundData);
                }
                bgmDictionary.Add(mapping.type, soundDatas);
            }
        }
    }

    private void Start()
    {
        PlaySceneBGM();
    }

    void PlaySceneBGM()
    {
        if (bgmDictionary.Count == 0)
        {
            return;
        }

        if (SoundManager.Instance != null && DungeonManager.Instance != null)
        {
            List<SoundData> sounds = new List<SoundData>();

            sounds = bgmDictionary[DungeonManager.Instance.currentRoomType];
            int randomIndex = Random.Range(0, sounds.Count);

            SoundManager.Instance.PlayBGM(sounds[randomIndex].soundName);
        }
    }

    private void OnDisable()
    {
        SoundManager.Instance.StopBGM();
    }
}
