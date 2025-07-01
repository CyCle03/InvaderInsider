namespace InvaderInsider
{
    /// <summary>
    /// 공격할 수 있는 객체에 대한 인터페이스입니다.
    /// BaseCharacter의 확장된 기능을 지원합니다.
    /// </summary>
    public interface IAttacker
    {
        #region Properties
        
        /// <summary>공격력</summary>
        float AttackDamage { get; }
        
        /// <summary>공격 사거리</summary>
        float AttackRange { get; }
        
        #endregion

        #region Methods
        
        /// <summary>대상을 공격합니다</summary>
        /// <param name="target">공격할 대상</param>
        void Attack(IDamageable target);
        
        /// <summary>공격 가능 여부를 확인합니다</summary>
        /// <returns>공격 가능하면 true</returns>
        bool CanAttack();
        
        #endregion
    }
} 