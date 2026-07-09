namespace FlatVenture.NUH.Player.Skills
{
    /// <summary>모든 직업 액티브 스킬이 공유하는 마우스 입력 방식입니다.</summary>
    public enum ActiveSkillInputMode
    {
        /// <summary>우클릭을 누르는 동안 조준하고 버튼을 뗄 때 발동합니다.</summary>
        HoldAndRelease,
        /// <summary>우클릭을 누르는 순간 현재 방향으로 즉시 발동합니다.</summary>
        QuickCast
    }
}
