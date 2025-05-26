using System;

namespace InvaderInsider
{
    public interface IDamageable
    {
        void TakeDamage(float damage);
        float CurrentHealth { get; }
        float MaxHealth { get; }
        event Action<float> OnHealthChanged;
        event Action OnDeath;
    }
} 