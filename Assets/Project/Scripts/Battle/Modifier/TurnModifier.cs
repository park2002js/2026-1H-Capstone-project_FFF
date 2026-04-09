using UnityEngine;

namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 전투 중 적용되는 버프/디버프의 종류를 정의합니다.
    /// </summary>
    public enum ModifierType
    {
        None = 0,
        
        MaxRerolls,    // 최대 리롤 횟수 증감
        DrawCount,     // 드로우 장수 증감


        Strength,  // 공격력(StrengthCal) 계산에만 합산됨
        Damage,    // 피해량(DamageCal) 계산에만 합산됨
        
        // 추후 기획에 따라 아래와 같은 항목들을 유연하게 추가할 수 있습니다.
        // DamageMultiplier, 
        // MaxSelectCount
    }

    /// <summary>
    /// N턴 동안 유지되거나 영구적으로 적용되는 수치 변경자(버프/디버프) 객체입니다.
    /// </summary>
    public class TurnModifier
    {
        /// <summary> 영구적 버프 상수로 정의 </summary>
        public const int PERMANENT_TURN = -1;
        /// <summary>적용할 효과의 종류</summary>
        public ModifierType Type { get; private set; }
        
        /// <summary>변화량 (+, -)</summary>
        public int Value { get; private set; }
        
        /// <summary>남은 턴 수</summary>
        public int TurnsRemaining { get; private set; }

        /// <summary>영구 적용 여부 (턴 수가 -1 이면 영구적)</summary>
        public bool IsPermanent => TurnsRemaining == PERMANENT_TURN;

        /// <summary>
        /// TurnModifier를 생성합니다.
        /// </summary>
        /// <param name="type">적용할 효과의 종류</param>
        /// <param name="value">변화량 (예: +1, -2)</param>
        /// <param name="turns">유지될 턴 수. -1을 넣으면 영구 적용됩니다.</param>
        public TurnModifier(ModifierType type, int value, int turns = PERMANENT_TURN)
        {
            Type = type;
            Value = value;
            TurnsRemaining = turns < 0 ? PERMANENT_TURN : turns;
        }

        /// <summary>
        /// 턴 종료 시 호출하여 남은 턴 수를 차감합니다.
        /// </summary>
        /// <returns>수명이 다하여 리스트에서 제거해야 한다면 true 반환</returns>
        public bool TickTurn()
        {
            // 영구 버프는 턴을 차감하지 않고 무조건 false(유지) 반환
            if (IsPermanent) return false;

            TurnsRemaining--;
            
            // 남은 턴이 0 이하가 되면 true(제거) 반환
            return TurnsRemaining <= 0;
        }
    }
}