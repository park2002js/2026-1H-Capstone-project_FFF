using System.Collections.Generic;
using System.Linq;
using FFF.Data;
using FFF.Battle.Modifier;
using FFF.Battle.Damage;
namespace FFF.Battle.Damage
{
    public class DamageCal
    {
        /// <summary>
        /// (TurnEnd에서 사용될 껍데기) 플레이어와 적의 공격력을 비교하여 최종 데미지를 산출합니다.
        /// </summary>
        public int CalculateDamage(int playerStrength, int enemyStrength, IReadOnlyList<TurnModifier> activeModifiers)
        {
            // TODO: 다음 페이즈(TurnEnd)에서 구현
            return 0; 
        }
    }
}