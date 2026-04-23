using UnityEngine;

namespace FFF.Battle.Modifier
{
    /// <summary>
    /// 조건(Condition)과 효과(Effect) 부품을 조립하여 완성된 '최종 아이템/버프 객체'입니다.
    /// 구 TurnModifier를 완벽히 대체하며, 파이프라인(ModifierManager)에 등록되어 실질적인 가로채기를 수행합니다.
    /// </summary>
    public class ItemModifier
    {
        /// <summary> 영구 버프를 의미하는 매직 넘버 </summary>
        public const int PERMANENT_TURN = -1;

        /// <summary> 디버깅 및 장부 기록을 위한 고유 식별자 (예: "JKR_EVEN_DOUBLE_Effect") </summary>
        public string Id { get; private set; }

        /// <summary> 이 아이템이 파이프라인에서 가로챌 값의 목적지 (예: 데미지, 리롤 횟수) </summary>
        public ModifierValueType ValueType { get; private set; }

        // === 조립된 레고 부품들 ===
        private readonly IModifierCondition _condition;
        private readonly IModifierEffect _effect;

        // === 수명 관리 ===
        public int TurnsRemaining { get; private set; }
        public bool IsPermanent => TurnsRemaining == PERMANENT_TURN;

        /// <summary>
        /// 새로운 아이템/버프 객체를 조립합니다.
        /// </summary>
        /// <param name="id">디버깅용 ID</param>
        /// <param name="valueType">수정할 값의 종류</param>
        /// <param name="condition">발동 조건 부품 (null이면 항상 발동)</param>
        /// <param name="effect">수치 연산 부품 (null이면 값 변화 없음)</param>
        /// <param name="turns">유지될 턴 수 (-1이면 영구)</param>
        public ItemModifier(string id, ModifierValueType valueType, IModifierCondition condition, IModifierEffect effect, int turns = PERMANENT_TURN)
        {
            Id = id;
            ValueType = valueType;
            _condition = condition;
            _effect = effect;
            TurnsRemaining = turns < 0 ? PERMANENT_TURN : turns;
        }

        /// <summary>
        /// 현재 상황(Context)에서 이 아이템 효과가 켜져야 하는지 판별합니다.
        /// </summary>
        public bool IsActive(ModifierContext context = null)
        {
            // 조건 부품이 조립되지 않았다면, 무조건 발동하는 것으로 간주합니다. (예: 단순 스탯 증가 장신구)
            if (_condition == null) return true;
            
            // 판정 책임은 조건 부품에게 위임합니다.
            return _condition.IsMet(context);
        }

        /// <summary>
        /// 파이프라인의 이전 값을 받아 효과 부품의 연산을 거친 뒤 뱉어냅니다.
        /// </summary>
        public int ApplyEffect(int currentValue)
        {
            if (_effect == null) return currentValue;
            
            // 연산 책임은 효과 부품에게 위임합니다.
            return _effect.Apply(currentValue);
        }

        /// <summary>
        /// 턴 종료 시 파이프라인 매니저에 의해 호출되어 수명을 1 차감합니다.
        /// </summary>
        /// <returns>수명이 다하여 파이프라인 리스트에서 제거되어야 하면 true</returns>
        public bool TickTurn()
        {
            // 영구 버프는 턴을 차감하지 않고 무조건 생존(false)
            if (IsPermanent) return false;
            
            TurnsRemaining--;
            
            // 0 이하가 되면 사망(true) 판정
            return TurnsRemaining <= 0;
        }
    }
}