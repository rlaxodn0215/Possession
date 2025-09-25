namespace cowsins
{
    // Implemented by PlayerStats and required by PlayerDependencies
    public interface IPlayerStatsProvider
    {
        float Health { get; }
        float MaxHealth { get; }
        float Shield { get; }
        float MaxShield { get; }
        bool IsDead { get; }
        void Heal(float amount);
        bool IsFullyHealed();
    }
}