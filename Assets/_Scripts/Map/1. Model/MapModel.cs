using System.Collections.Generic;

/// <summary>
/// 지도의 데이터와 현재 게임 상태를 관리하는 Model
/// </summary>
public class MapModel
{
    //현재 던전 안에 있는지 여부 확인
    public bool isInDungeon = false;

    //시드값과 전체 맵 데이터
    public int CurrentSeed { get; private set; }
    public List<List<MapNode>> EntireMap { get; private set; }

    public void SetMapData(int seed, List<List<MapNode>> mapData)
    {
        CurrentSeed = seed;
        EntireMap = mapData;
    }
}
