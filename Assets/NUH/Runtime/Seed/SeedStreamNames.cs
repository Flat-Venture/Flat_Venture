/// <summary>
/// 팀에서 공통으로 사용하는 기본 스트림 이름입니다.
/// 별도 기능은 새 문자열 이름을 사용해 독립 스트림을 만들 수 있습니다.
/// </summary>
public static class SeedStreamNames
{
    // 팀 공통 이름은 오타와 대소문자 차이를 막기 위해 상수로 사용합니다.
    // 각 스트림은 같은 RunSeed에서도 서로 독립적인 MT19937 상태를 가집니다.
    public const string Map = "Map";
    public const string Monster = "Monster";
    public const string Item = "Item";
    public const string Shop = "Shop";
    public const string Forge = "Forge";
    public const string Event = "Event";
}
