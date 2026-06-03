using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Modifier;
using FFF.Battle.FSM; // BattleManager 참조용
// ItemFactoy 및 ItemBase 기반 코드 + IClickable을 사용함
namespace FFF.Data
{
    /// <summary>
    /// 조커 객체 생성 및 사용(클릭) 명령 중계 관리자.
    /// 임시 버프 관리는 전적으로 ModifierManager에 위임함.
    /// </summary>
    public class JokerManager : MonoBehaviour
    {
        /// <summary>현재 보유 중인 조커 C# 로직 객체 목록</summary>
        private readonly List<ItemBase> _heldJokers = new();
        public IReadOnlyList<ItemBase> HeldJokers => _heldJokers;

        /// <summary>
        /// 전투 시작 시 ID 리스트를 기반으로 조커 객체 생성.
        /// </summary>
        public void Initialize(List<string> jokerIds)
        {
            _heldJokers.Clear();
            foreach (var id in jokerIds)
            {
                var item = ItemFactory.CreateItem(id);
                if (item != null)
                {
                    _heldJokers.Add(item);
                }
            }
            Debug.Log($"[JokerManager] 조커 {jokerIds.Count}개 로드 및 생성 완료.");
        }

        /// <summary>
        /// 외부(UI) 요청에 의한 조커 사용 처리.
        /// </summary>
        public bool UseJoker(int jokerIndex, ModifierContext context)
        {
            if (jokerIndex < 0 || jokerIndex >= _heldJokers.Count)
            {
                Debug.LogWarning($"[JokerManager] 잘못된 조커 인덱스: {jokerIndex}");
                return false;
            }

            var joker = _heldJokers[jokerIndex];

            // IClickable 인터페이스 확인으로 사용 가능한 아이템인지 판별
            if (joker is IClickable clickableJoker)
            {
                // 성공적으로 효과가 등록된 직후, 매니저 및 플레이어 데이터에서 영구 삭제하기 위한 콜백
                Action consumeAction = () =>
                {
                    string consumedId = joker.Id;
                    _heldJokers.RemoveAt(jokerIndex);
                    
                    // 중앙 데이터 동기화 (PlayerDataBattle 리스트에서 제거)
                    BattleManager.Instance.Context.PlayerData.ConsumeJoker(consumedId);
                    
                    Debug.Log($"[JokerManager] 조커 소모 처리 완료: {consumedId}");
                };

                return clickableJoker.Use(context, consumeAction);
            }

            Debug.LogWarning($"[JokerManager] 클릭 불가능한 아이템에 대한 조커 사용 시도: {joker.Id}");
            return false;
        }
    }
}

/*
        /// <summary>
        /// 조커 카드를 획득한다 (상점/보상 등에서).
        /// </summary>
        public void AddJoker(JokerBase joker)
        {
            if (joker == null) return;
            if (_heldJokers.Count >= PlayerDataSO.MaxHeldJokerCount)
            {
                Debug.LogWarning($"[JokerManager] 조커는 최대 {PlayerDataSO.MaxHeldJokerCount}장까지만 보유할 수 있습니다.");
                return;
            }

            _heldJokers.Add(joker);
            Debug.Log($"[JokerManager] 조커 획득: {joker.DisplayName}");
        }

        public void SetJokersFromIds(IReadOnlyList<string> jokerIds)
        {
            _heldJokers.Clear();

            List<JokerBase> jokers = JokerFactory.CreateMany(jokerIds, PlayerDataSO.MaxHeldJokerCount);
            foreach (JokerBase joker in jokers)
                AddJoker(joker);

            Debug.Log($"[JokerManager] 보유 조커 로드 완료: {_heldJokers.Count}개");
        }

        /// <summary>
        /// 플레이어가 조커를 사용한다.
        /// TurnProceed 상태에서 카드 선택 완료 전에 호출.
        /// 사용 후 조커는 소멸되어 목록에서 제거된다.
        /// </summary>
        /// <param name="jokerIndex">사용할 조커의 인덱스</param>
        /// <returns>사용 성공 여부</returns>
        public bool UseJoker(int jokerIndex)
        {
            if (jokerIndex < 0 || jokerIndex >= _heldJokers.Count)
            {
                Debug.LogWarning($"[JokerManager] 잘못된 조커 인덱스: {jokerIndex}");
                return false;
            }

            var joker = _heldJokers[jokerIndex];

            var context = new JokerContext
            {
                DeckSystem = _deckSystem,
                ModifierManager = _modifierManager 
            };

            bool success = joker.Use(context);

            if (success)
            {
                _heldJokers.RemoveAt(jokerIndex);
                SyncCachedDeckBonuses();
                Debug.Log($"[JokerManager] 조커 사용 완료 및 소멸: {joker.DisplayName}. 남은 조커: {_heldJokers.Count}개");
            }

            return success;
        }

        public void SyncCachedDeckBonuses()
        {
            ModifierManager modifierManager = _modifierManager != null ? _modifierManager : ModifierManager.Instance;
            if (modifierManager != null)
                modifierManager.SyncCachedValues(BattleManager.Instance != null ? BattleManager.Instance.CurrentModifierContext : null);
        }

        #endregion

        #region === Getter: 외부에서 효과 값 조회 (알빠노 패턴) ===

        /// <summary>
        /// 현재 턴의 데미지 배율을 반환한다.
        /// 
        /// 데미지 계산 코드는 이 함수만 호출하면 된다.
        /// 어떤 조커가 발동되었는지, 조건이 뭔지 알 필요 없다.
        /// 
        /// 사용 예:
        ///   int finalDamage = (int)(basePower * jokerManager.GetDamageMultiplier(seotdaResult));
        /// </summary>
        /// <param name="result">현재 족보 판정 결과 (조건부 배율 판단용)</param>
        /// <returns>데미지 배율. 기본 1.0.</returns>
        public float GetDamageMultiplier(SeotdaResult result)
        {
            // 조건 함수가 없으면 무조건 배율 적용
            if (_damageMultiplierCondition == null)
            {
                return _damageMultiplier;
            }

            // 조건 충족 시에만 배율 적용
            if (_damageMultiplierCondition(result))
            {
                return _damageMultiplier;
            }

            return 1.0f;
        }

        #endregion

        #region === 조커 효과가 호출하는 내부 setter ===

        /// <summary>
        /// 데미지 배율을 설정한다.
        /// 구체 조커의 Activate()에서 호출.
        /// </summary>
        /// <param name="multiplier">배율 값</param>
        /// <param name="condition">조건 함수. null이면 무조건 적용.</param>
        public void SetDamageMultiplier(float multiplier, Func<SeotdaResult, bool> condition = null)
        {
            _damageMultiplier = multiplier;
            _damageMultiplierCondition = condition;

            Debug.Log($"[JokerManager] 데미지 배율 설정: x{multiplier}");
        }

        #endregion

        #region === 턴 종료 시 임시 효과 초기화 ===

        /// <summary>
        /// 턴 종료 시 호출 (BattleManager.OnTurnEnd).
        /// 한 턴짜리 효과들을 초기화한다.
        /// </summary>
        private void ResetTurnEffects()
        {
            _damageMultiplier = 1.0f;
            _damageMultiplierCondition = null;

            Debug.Log("[JokerManager] 턴 종료 → 임시 효과 초기화");
        }

        #endregion
    }

    public static class JokerFactory
    {
        public static JokerBase Create(string jokerId)
        {
            return jokerId switch
            {
                RerollBurstJoker.JokerId => new RerollBurstJoker(),
                HighCardJoker.JokerId => new HighCardJoker(),
                DoublePipJoker.JokerId => new DoublePipJoker(),
                LuckyCharmJoker.JokerId => new LuckyCharmJoker(),
                _ => null
            };
        }

        public static List<JokerBase> CreateMany(IReadOnlyList<string> jokerIds, int limit)
        {
            var jokers = new List<JokerBase>();
            if (jokerIds == null)
                return jokers;

            int count = Mathf.Min(jokerIds.Count, limit);
            for (int i = 0; i < count; i++)
            {
                JokerBase joker = Create(jokerIds[i]);
                if (joker != null)
                {
                    jokers.Add(joker);
                    continue;
                }

                Debug.LogWarning($"[JokerFactory] 알 수 없는 조커 ID입니다: {jokerIds[i]}");
            }

            return jokers;
        }
    }

    public abstract class JokerBaseWithHelpers : JokerBase
    {
        protected static ModifierManager ResolveModifierManager(JokerContext context)
        {
            ModifierManager modifierManager = context?.ModifierManager != null
                ? context.ModifierManager
                : ModifierManager.Instance;

            if (modifierManager == null)
                Debug.LogWarning("[Joker] ModifierManager가 없어 조커 효과를 적용할 수 없습니다.");

            return modifierManager;
        }
    }

    public sealed class RerollBurstJoker : JokerBaseWithHelpers
    {
        public const string JokerId = "JKR_REROLL_BURST";

        public override string Id => JokerId;
        public override string DisplayName => "리롤 폭죽 조커";
        public override string Description => "이번 턴 리롤 횟수를 4회 늘립니다.";

        protected override void Activate(JokerContext context)
        {
            ModifierManager modifierManager = ResolveModifierManager(context);
            if (modifierManager == null)
                return;

            modifierManager.AddModifier(new BattleModifier(
                "JKR_REROLL_BURST_MaxRerolls",
                ModifierValueType.MaxRerolls,
                new AlwaysTrueCondition(),
                new ExtraRerollCountEffect(4),
                turns: 1));
        }
    }

    public sealed class HighCardJoker : JokerBaseWithHelpers
    {
        public const string JokerId = "JKR_HIGH_CARD";

        public override string Id => JokerId;
        public override string DisplayName => "광패 조커";
        public override string Description => "이번 턴 광 카드가 포함된 패의 공격력을 60 올립니다.";

        protected override void Activate(JokerContext context)
        {
            ModifierManager modifierManager = ResolveModifierManager(context);
            if (modifierManager == null)
                return;

            modifierManager.AddModifier(new BattleModifier(
                "JKR_HIGH_CARD_Strength",
                ModifierValueType.Strength,
                new HandIncludesCardTypeCondition(CardType.Gwang),
                new StrengthConstantEffect(60),
                turns: 1));
        }
    }

    public sealed class DoublePipJoker : JokerBaseWithHelpers
    {
        public const string JokerId = "JKR_DOUBLE_PIP";

        public override string Id => JokerId;
        public override string DisplayName => "쌍피 조커";
        public override string Description => "이번 턴 피 카드 2장으로 낸 패의 공격력을 45 올립니다.";

        protected override void Activate(JokerContext context)
        {
            ModifierManager modifierManager = ResolveModifierManager(context);
            if (modifierManager == null)
                return;

            modifierManager.AddModifier(new BattleModifier(
                "JKR_DOUBLE_PIP_Strength",
                ModifierValueType.Strength,
                new HandCardTypeCountCondition(CardType.Pi, 2),
                new StrengthConstantEffect(45),
                turns: 1));
        }
    }

    public sealed class LuckyCharmJoker : JokerBaseWithHelpers
    {
        public const string JokerId = "JKR_LUCKY_CHARM";

        public override string Id => JokerId;
        public override string DisplayName => "행운 부적 조커";
        public override string Description => "이번 턴 낸 패의 공격력을 25 올립니다.";

        protected override void Activate(JokerContext context)
        {
            ModifierManager modifierManager = ResolveModifierManager(context);
            if (modifierManager == null)
                return;

            modifierManager.AddModifier(new BattleModifier(
                "JKR_LUCKY_CHARM_Strength",
                ModifierValueType.Strength,
                new AlwaysTrueCondition(),
                new StrengthConstantEffect(25),
                turns: 1));
        }
    }

    internal sealed class HandIncludesCardTypeCondition : IModifierCondition
    {
        private readonly CardType _cardType;

        public HandIncludesCardTypeCondition(CardType cardType)
        {
            _cardType = cardType;
        }

        public bool IsMet(ModifierContext context = null)
        {
            if (context?.ActionHandResult == null)
                return false;

            SeotdaResult result = context.ActionHandResult.Value;
            return result.Card1.Type == _cardType || result.Card2.Type == _cardType;
        }
    }

    internal sealed class HandCardTypeCountCondition : IModifierCondition
    {
        private readonly CardType _cardType;
        private readonly int _requiredCount;

        public HandCardTypeCountCondition(CardType cardType, int requiredCount)
        {
            _cardType = cardType;
            _requiredCount = Mathf.Max(1, requiredCount);
        }

        public bool IsMet(ModifierContext context = null)
        {
            if (context?.ActionHandResult == null)
                return false;

            SeotdaResult result = context.ActionHandResult.Value;
            int count = 0;
            if (result.Card1.Type == _cardType)
                count++;
            if (result.Card2.Type == _cardType)
                count++;

            return count >= _requiredCount;
        }
    }
*/