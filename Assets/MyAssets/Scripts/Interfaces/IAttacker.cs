namespace InvaderInsider
{
    public interface IAttacker
    {
        float AttackDamage { get; }
        float AttackRange { get; }
        void Attack(IDamageable target);
    }
} 