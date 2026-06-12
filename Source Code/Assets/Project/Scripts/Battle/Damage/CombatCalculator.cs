using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using FFF.Data;
using FFF.Battle.Modifier;

namespace FFF.Battle.Damage
{
    /// <summary>
    /// 외부 매니저들이 계산을 의뢰하는 단일 창구(Facade)
    /// </summary>
    public class CombatCalculator
    {
        #region === 하위 시스템 ===
        public StrengthCal Strength { get; private set; }
        public DamageCal Damage { get; private set; }
        #endregion

        public CombatCalculator()
        {
            Strength = new StrengthCal();
            Damage = new DamageCal();
        }
    }
}