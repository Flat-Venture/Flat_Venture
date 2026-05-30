using UnityEngine;
using static Enums;

[CreateAssetMenu(fileName = "ThemeData", menuName = "Scriptable Objects/ThemeData")]
public class ThemeData : ScriptableObject
{
    [Header("테마 타입 (Desert, Grass, Lava, Snow 등)")]
    public DungeonTheme theme;

    [Header("바닥 Material")]
    public Material floorMaterial;

    [Header("테마 프리팹들 (예: 나무, 선인장, 눈사람 등)")]
    public GameObject[] themeProps;

    [Header("프리팹이 배치될 위치들")]
    public Transform[] propSpawnPoints;
}
