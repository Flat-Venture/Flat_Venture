namespace FlatVenture.Enums
{
    /// <summary>
    /// 맵에 등장하는 방(노드)의 종류를 정의
    /// </summary>
    public enum RoomType
    {
        Start,      //시작 방
        Normal,     //일반 몬스터
        Elite,      //엘리트 몬스터
        Rest,       //휴식
        Forge,      //대장간
        Shop,       //상점
        Unknown,    //이벤트(물음표/미지)
        Boss,       //보스   
    }

    /// <summary>
    /// 개별 몬스터의 역할군
    /// </summary>
    public enum MonsterRole
    {
        Melee,      //근접
        Ranged,     //원거리
        Tanker,     //탱커
        Support     //지원
    }
}
