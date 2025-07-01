using System;
using UnityEngine;

namespace InvaderInsider
{
    /// <summary>
    /// 데미지를 받을 수 있는 객체에 대한 인터페이스입니다.
    /// BaseCharacter의 확장된 기능을 지원합니다.
    /// </summary>
    public interface IDamageable
    {
        #region Core Properties
        
        /// <summary>현재 체력</summary>
        float CurrentHealth { get; }
        
        /// <summary>최대 체력</summary>
        float MaxHealth { get; }
        
        /// <summary>생존 여부</summary>
        bool IsAlive { get; }
        
        /// <summary>체력 비율 (0-1)</summary>
        float HealthRatio { get; }
        
        #endregion

        #region Core Methods
        
        /// <summary>데미지를 받습니다</summary>
        /// <param name="damage">받을 데미지량</param>
        void TakeDamage(float damage);
        
        /// <summary>특정 공격자로부터 데미지를 받습니다 (선택적)</summary>
        /// <param name="damage">받을 데미지량</param>
        /// <param name="attacker">공격자 (null 가능)</param>
        void TakeDamageFrom(float damage, GameObject attacker = null);
        
        /// <summary>체력을 회복합니다</summary>
        /// <param name="healAmount">회복량</param>
        void Heal(float healAmount);
        
        #endregion

        #region Events
        
        /// <summary>체력이 변경될 때 발생하는 이벤트 (체력 비율 0-1)</summary>
        event Action<float> OnHealthChanged;
        
        /// <summary>데미지를 받을 때 발생하는 이벤트 (데미지량, 현재 체력, 최대 체력)</summary>
        event Action<float, float, float> OnDamageReceived;
        
        /// <summary>사망 시 발생하는 이벤트</summary>
        event Action OnDeath;
        
        #endregion
    }
} 