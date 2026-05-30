public interface BossStatus
{
    int BossId { get; }
    string BossName { get; }

    float CurrentHp { get; }
    float MaxHp { get; }

    float CurrentStun { get; }
    float MaxStun { get; }

    bool IsAlive { get; }

    bool IsStunned { get; }
}
