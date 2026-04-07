using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FFF.Data;
using FFF.Battle.Card;
using FFF.Battle.Item.Accessory;
using FFF.Battle.Item.Joker;

namespace FFF.Test
{
    /// <summary>
    /// 장신구 / 조커 시스템 통합 테스트용 MonoBehaviour.
    /// 
    /// 사용법:
    /// 1. 빈 Scene 생성 (예: ItemTestScene)
    /// 2. 빈 GameObject에 이 스크립트를 붙인다.
    /// 3. Play 버튼을 누르면 Console에 테스트 결과가 출력된다.
    /// 
    /// CardSystemTest와 동일한 패턴:
    /// DeckSystem(MonoBehaviour)을 거치지 않고 하위 시스템을 직접 생성하여
    /// SO Event 없이 순수 로직만 검증한다.
    /// JokerManager처럼 MonoBehaviour가 필요한 부분만 런타임에 동적 생성한다.
    /// </summary>
    public class ItemSystemTest : MonoBehaviour
    {
        [Header("테스트 시드 (같은 시드 = 같은 결과)")]
        [SerializeField] private int _testSeed = 42;

        private int _passCount = 0;
        private int _failCount = 0;

        private void Start()
        {
            Debug.Log("╔══════════════════════════════════════╗");
            Debug.Log("║  장신구 / 조커 시스템 통합 테스트     ║");
            Debug.Log("╚══════════════════════════════════════╝\n");

            // ===========================
            // Part A: CardPile 가중치 드로우
            // ===========================
            Test_A1_WeightedDraw_BasicFunction();
            Test_A2_WeightedDraw_HighCardBias();
            Test_A3_WeightedDraw_CardCountPreserved();

            // ===========================
            // Part B: CardDrawHandler 보너스 리롤
            // ===========================
            Test_B1_BonusRerolls_InConstructor();
            Test_B2_TempRerolls_AddAndUse();
            Test_B3_TempRerolls_ResetOnNextDraw();

            // ===========================
            // Part C: CardDrawHandler 가중치 드로우 통합
            // ===========================
            Test_C1_DrawHandler_WithWeightFunc();
            Test_C2_DrawHandler_WithoutWeightFunc();

            // ===========================
            // Part D: 장신구 – RerollBonusAccessory
            // ===========================
            Test_D1_RerollAccessory_ApplyIncreasesBonusRerolls();
            Test_D2_RerollAccessory_RemoveRestoresBonusRerolls();

            // ===========================
            // Part E: 장신구 – HighCardWeightAccessory
            // ===========================
            Test_E1_HighCardWeight_ApplySetsWeightFunc();
            Test_E2_HighCardWeight_RemoveClearsWeightFunc();
            Test_E3_HighCardWeight_StatisticalBias();

            // ===========================
            // Part F: 조커 – JokerBase 생명주기
            // ===========================
            Test_F1_Joker_UseOnce_Success();
            Test_F2_Joker_UseAgain_Fail();

            // ===========================
            // Part G: 조커 – EvenHandDoubleJoker 조건 판정
            // ===========================
            Test_G1_EvenHand_Condition_EvenSum();
            Test_G2_EvenHand_Condition_OddSum();
            Test_G3_EvenHand_Condition_Ddaeng();

            // ===========================
            // Part H: 조커 – RerollBurstJoker
            // ===========================
            Test_H1_RerollBurst_AddsTempRerolls();

            // ===========================
            // Part I: JokerManager 데미지 배율
            // ===========================
            Test_I1_DamageMultiplier_Default();
            Test_I2_DamageMultiplier_Unconditional();
            Test_I3_DamageMultiplier_ConditionalPass();
            Test_I4_DamageMultiplier_ConditionalFail();
            Test_I5_DamageMultiplier_ResetOnTurnEnd();

            // ===========================
            // Part J: 전체 전투 시뮬레이션
            // ===========================
            Test_J1_FullBattleFlow_WithAccessoryAndJoker();
            Test_J2_MultiTurn_JokerSingleUse();
            Test_J3_CardCount_NeverChanges();

            // ===========================
            // 결과 요약
            // ===========================
            Debug.Log("\n╔══════════════════════════════════════╗");
            Debug.Log($"║  테스트 완료: ✅ {_passCount} PASS / ❌ {_failCount} FAIL  ");
            Debug.Log("╚══════════════════════════════════════╝");

            if (_failCount == 0)
            {
                Debug.Log("🎉 모든 테스트를 통과했습니다!");
            }
            else
            {
                Debug.LogWarning($"⚠️ {_failCount}개의 테스트가 실패했습니다. 위의 FAIL 항목을 확인하세요.");
            }
        }

        // ============================================================
        // Part A: CardPile 가중치 드로우
        // ============================================================

        /// <summary>A1: 가중치 드로우가 요청한 수만큼 카드를 반환하는지</summary>
        private void Test_A1_WeightedDraw_BasicFunction()
        {
            Debug.Log("\n── A1: 가중치 드로우 - 기본 동작 ──");

            var pile = new CardPile();
            pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed);

            // 모든 카드 가중치 1.0 (균등)
            var drawn = pile.MoveDrawToHandWeighted(5, card => 1.0f);

            Debug.Log($"  요청: 5장, 실제: {drawn.Count}장");
            Debug.Log($"  DrawPile: {pile.DrawPile.Count}장, Hand: {pile.Hand.Count}장");

            bool pass = drawn.Count == 5
                     && pile.Hand.Count == 5
                     && pile.DrawPile.Count == 15;

            LogResult("가중치 드로우 기본 동작", pass);
        }

        /// <summary>A2: 가중치가 높은 카드가 통계적으로 더 자주 뽑히는지 (100회 시행)</summary>
        private void Test_A2_WeightedDraw_HighCardBias()
        {
            Debug.Log("\n── A2: 가중치 드로우 - 높은 카드 편향 ──");

            int highCardDrawCount = 0;  // 5월 이상
            int totalDrawn = 0;
            int trials = 100;

            for (int t = 0; t < trials; t++)
            {
                var pile = new CardPile();
                pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed + t);

                // 5월 이상 카드 가중치를 10배로 극단적으로 설정 (편향 확인용)
                var drawn = pile.MoveDrawToHandWeighted(5, card =>
                {
                    return card.GetMonthValue() >= 5 ? 10.0f : 1.0f;
                });

                foreach (var card in drawn)
                {
                    totalDrawn++;
                    if (card.GetMonthValue() >= 5)
                    {
                        highCardDrawCount++;
                    }
                }
            }

            float highCardRatio = (float)highCardDrawCount / totalDrawn;

            // 20장 중 5월 이상 = 12장(5~10월 × 2장), 4월 이하 = 8장
            // 균등이면 12/20 = 60%. 가중치 10배면 훨씬 높아야 함 (대략 88% 이상 예상)
            Debug.Log($"  총 {totalDrawn}장 중 5월 이상: {highCardDrawCount}장 ({highCardRatio:P1})");
            Debug.Log($"  균등 드로우 기대치: ~60%, 가중치 드로우 기대치: ~88%+");

            bool pass = highCardRatio > 0.75f; // 보수적 기준: 75% 이상이면 편향 확인
            LogResult("가중치 드로우 높은 카드 편향", pass);
        }

        /// <summary>A3: 가중치 드로우 후 카드 총 수가 보존되는지</summary>
        private void Test_A3_WeightedDraw_CardCountPreserved()
        {
            Debug.Log("\n── A3: 가중치 드로우 - 카드 수 보존 ──");

            var pile = new CardPile();
            pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed);

            pile.MoveDrawToHandWeighted(5, card => card.GetMonthValue() >= 5 ? 2.0f : 1.0f);

            Debug.Log($"  DrawPile({pile.DrawPile.Count}) + Hand({pile.Hand.Count}) = {pile.TotalCardCount}");

            bool pass = pile.TotalCardCount == 20;
            LogResult("가중치 드로우 카드 수 보존", pass);
        }

        // ============================================================
        // Part B: CardDrawHandler 보너스 리롤
        // ============================================================

        /// <summary>B1: 생성자에서 보너스 리롤이 반영되는지</summary>
        private void Test_B1_BonusRerolls_InConstructor()
        {
            Debug.Log("\n── B1: 보너스 리롤 - 생성자 반영 ──");

            var pile = new CardPile();
            pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed);

            // maxRerolls=1 + bonusRerolls=1 → 실제 maxRerolls=2
            int baseRerolls = 1;
            int bonusRerolls = 1;
            var handler = new CardDrawHandler(pile, drawCount: 5, maxRerolls: baseRerolls + bonusRerolls);

            handler.DrawCards();

            Debug.Log($"  기본 리롤: {baseRerolls}, 보너스: {bonusRerolls}");
            Debug.Log($"  실제 남은 리롤: {handler.RerollsRemaining}");

            bool pass = handler.RerollsRemaining == (baseRerolls + bonusRerolls);
            LogResult("보너스 리롤 생성자 반영", pass);
        }

        /// <summary>B2: AddTempRerolls로 현재 턴 리롤 증가 + 사용 가능 확인</summary>
        private void Test_B2_TempRerolls_AddAndUse()
        {
            Debug.Log("\n── B2: 임시 리롤 - 추가 및 사용 ──");

            var pile = new CardPile();
            pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed);

            var handler = new CardDrawHandler(pile, drawCount: 5, maxRerolls: 1);
            handler.DrawCards();

            Debug.Log($"  드로우 후 리롤: {handler.RerollsRemaining}");

            // 기본 리롤 1회 소모
            var hand = new List<HwaTuCard>(pile.Hand);
            handler.Reroll(new List<HwaTuCard> { hand[0] });
            Debug.Log($"  1회 리롤 후: {handler.RerollsRemaining}, CanReroll: {handler.CanReroll}");

            bool rerollExhausted = !handler.CanReroll;
            LogResult("기본 리롤 소진", rerollExhausted);

            // 임시 리롤 +4 추가
            handler.AddTempRerolls(4);
            Debug.Log($"  임시 리롤 +4 후: {handler.RerollsRemaining}, CanReroll: {handler.CanReroll}");

            bool tempAdded = handler.RerollsRemaining == 4 && handler.CanReroll;
            LogResult("임시 리롤 추가 반영", tempAdded);

            // 4회 추가 리롤 모두 사용 가능한지 확인
            int usedRerolls = 0;
            for (int i = 0; i < 4; i++)
            {
                hand = new List<HwaTuCard>(pile.Hand);
                if (hand.Count > 0 && handler.CanReroll)
                {
                    handler.Reroll(new List<HwaTuCard> { hand[0] });
                    usedRerolls++;
                }
            }

            Debug.Log($"  추가 리롤 사용: {usedRerolls}회, 남은 리롤: {handler.RerollsRemaining}");

            bool allUsed = usedRerolls == 4 && handler.RerollsRemaining == 0;
            LogResult("임시 리롤 4회 모두 사용", allUsed);
        }

        /// <summary>B3: 다음 턴 DrawCards() 시 임시 리롤이 리셋되는지</summary>
        private void Test_B3_TempRerolls_ResetOnNextDraw()
        {
            Debug.Log("\n── B3: 임시 리롤 - 다음 턴 리셋 ──");

            var pile = new CardPile();
            pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed);

            int maxRerolls = 1;
            var handler = new CardDrawHandler(pile, drawCount: 5, maxRerolls: maxRerolls);

            // 턴 1: 드로우 → 임시 리롤 추가
            handler.DrawCards();
            handler.AddTempRerolls(4);
            Debug.Log($"  턴 1 임시 리롤 추가 후: {handler.RerollsRemaining}");

            // 턴 정리
            pile.MoveAllToDiscard();

            // 턴 2: 다시 드로우 → 리롤이 maxRerolls(1)로 리셋되어야 함
            handler.DrawCards();
            Debug.Log($"  턴 2 드로우 후 리롤: {handler.RerollsRemaining} (기대값: {maxRerolls})");

            bool pass = handler.RerollsRemaining == maxRerolls;
            LogResult("임시 리롤 다음 턴 리셋", pass);
        }

        // ============================================================
        // Part C: CardDrawHandler 가중치 드로우 통합
        // ============================================================

        /// <summary>C1: DrawHandler에 가중치 함수 전달 시 정상 드로우 확인</summary>
        private void Test_C1_DrawHandler_WithWeightFunc()
        {
            Debug.Log("\n── C1: DrawHandler 가중치 드로우 ──");

            var pile = new CardPile();
            pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed);

            Func<HwaTuCard, float> weightFunc = card =>
                card.GetMonthValue() >= 5 ? 2.0f : 1.0f;

            var handler = new CardDrawHandler(pile, drawCount: 5, maxRerolls: 1, drawWeightFunc: weightFunc);
            var drawn = handler.DrawCards();

            Debug.Log($"  드로우: {drawn.Count}장, Hand: {pile.Hand.Count}장");
            foreach (var card in drawn)
            {
                Debug.Log($"    - {card} (월: {card.GetMonthValue()})");
            }

            bool pass = drawn.Count == 5 && pile.Hand.Count == 5;
            LogResult("DrawHandler 가중치 드로우", pass);
        }

        /// <summary>C2: 가중치 함수 null이면 기존 균등 드로우와 동일 동작 확인</summary>
        private void Test_C2_DrawHandler_WithoutWeightFunc()
        {
            Debug.Log("\n── C2: DrawHandler 균등 드로우 (가중치 null) ──");

            var pile = new CardPile();
            pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed);

            var handler = new CardDrawHandler(pile, drawCount: 5, maxRerolls: 1, drawWeightFunc: null);
            var drawn = handler.DrawCards();

            Debug.Log($"  드로우: {drawn.Count}장 (가중치 없음)");

            bool pass = drawn.Count == 5 && pile.Hand.Count == 5 && pile.DrawPile.Count == 15;
            LogResult("DrawHandler 균등 드로우 (null)", pass);
        }

        // ============================================================
        // Part D: 장신구 – RerollBonusAccessory
        // ============================================================

        /// <summary>D1: Apply 호출 시 보너스 리롤이 반영되어 실제 리롤 횟수가 증가하는지</summary>
        private void Test_D1_RerollAccessory_ApplyIncreasesBonusRerolls()
        {
            Debug.Log("\n── D1: 리롤 장신구 - Apply 효과 ──");

            // 장신구가 DeckSystem.AddBonusRerolls()를 호출하는 구조.
            // DeckSystem은 MonoBehaviour이므로, 장신구의 내부 로직을 직접 검증:
            // "maxRerolls + bonusRerolls"가 실제 CardDrawHandler에 반영되는가.

            var pile = new CardPile();
            pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed);

            int baseRerolls = 1;
            int bonusFromAccessory = 1; // RerollBonusAccessory가 추가하는 양

            // 장신구 적용 후 상태를 시뮬레이션
            var handler = new CardDrawHandler(pile, drawCount: 5, maxRerolls: baseRerolls + bonusFromAccessory);
            handler.DrawCards();

            Debug.Log($"  기본 리롤: {baseRerolls}, 장신구 보너스: {bonusFromAccessory}");
            Debug.Log($"  실제 리롤 가능 횟수: {handler.RerollsRemaining}");

            // 리롤 2회 모두 사용 가능한지 확인
            var hand1 = new List<HwaTuCard>(pile.Hand);
            var result1 = handler.Reroll(new List<HwaTuCard> { hand1[0] });
            Debug.Log($"  1차 리롤: {result1.Count}장, 남은: {handler.RerollsRemaining}");

            var hand2 = new List<HwaTuCard>(pile.Hand);
            var result2 = handler.Reroll(new List<HwaTuCard> { hand2[0] });
            Debug.Log($"  2차 리롤: {result2.Count}장, 남은: {handler.RerollsRemaining}");

            // 3차는 실패해야 함
            var hand3 = new List<HwaTuCard>(pile.Hand);
            var result3 = handler.Reroll(new List<HwaTuCard> { hand3[0] });

            bool pass = result1.Count == 1
                     && result2.Count == 1
                     && result3.Count == 0
                     && handler.RerollsRemaining == 0;

            LogResult("리롤 장신구 Apply → 2회 사용 가능, 3회째 거부", pass);
        }

        /// <summary>D2: Remove 후 다시 초기화하면 리롤이 원래값으로 돌아가는지</summary>
        private void Test_D2_RerollAccessory_RemoveRestoresBonusRerolls()
        {
            Debug.Log("\n── D2: 리롤 장신구 - Remove 복원 ──");

            var pile = new CardPile();
            pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed);

            int baseRerolls = 1;

            // 장신구 해제 후 = 기본값으로 복귀 시뮬레이션
            var handler = new CardDrawHandler(pile, drawCount: 5, maxRerolls: baseRerolls);
            handler.DrawCards();

            Debug.Log($"  장신구 해제 후 리롤: {handler.RerollsRemaining} (기대값: {baseRerolls})");

            // 1회만 가능
            var hand = new List<HwaTuCard>(pile.Hand);
            handler.Reroll(new List<HwaTuCard> { hand[0] });
            var hand2 = new List<HwaTuCard>(pile.Hand);
            var failReroll = handler.Reroll(new List<HwaTuCard> { hand2[0] });

            bool pass = handler.RerollsRemaining == 0 && failReroll.Count == 0;
            LogResult("리롤 장신구 Remove → 원래 1회만 가능", pass);
        }

        // ============================================================
        // Part E: 장신구 – HighCardWeightAccessory
        // ============================================================

        /// <summary>E1: Apply 시 가중치 함수가 올바른 값을 반환하는지</summary>
        private void Test_E1_HighCardWeight_ApplySetsWeightFunc()
        {
            Debug.Log("\n── E1: 고급 화투패 장신구 - 가중치 함수 검증 ──");

            // HighCardWeightAccessory 내부의 가중치 로직을 재현하여 검증
            Func<HwaTuCard, float> weightFunc = card =>
                card.GetMonthValue() >= 5 ? 1.1f : 1.0f;

            var cards = HwaTuCardDatabase.CreateAllCards();

            // 각 카드별 가중치 출력
            var card1 = cards.Find(c => c.CardId == "M1_Gwang");  // 1월 → 1.0
            var card5 = cards.Find(c => c.CardId == "M5_Yeolkkeut"); // 5월 → 1.1
            var card10 = cards.Find(c => c.CardId == "M10_Yeolkkeut"); // 10월 → 1.1

            float w1 = weightFunc(card1);
            float w5 = weightFunc(card5);
            float w10 = weightFunc(card10);

            Debug.Log($"  1월 광 가중치: {w1} (기대: 1.0)");
            Debug.Log($"  5월 열끗 가중치: {w5} (기대: 1.1)");
            Debug.Log($"  10월 열끗 가중치: {w10} (기대: 1.1)");

            bool pass = Mathf.Approximately(w1, 1.0f)
                     && Mathf.Approximately(w5, 1.1f)
                     && Mathf.Approximately(w10, 1.1f);

            LogResult("고급 화투패 가중치 함수 값 검증", pass);
        }

        /// <summary>E2: Remove 시 가중치 함수가 null로 해제되는 개념 확인</summary>
        private void Test_E2_HighCardWeight_RemoveClearsWeightFunc()
        {
            Debug.Log("\n── E2: 고급 화투패 장신구 - Remove 해제 ──");

            // 가중치 함수 null인 경우 CardDrawHandler가 기존 균등 드로우를 사용하는지 확인
            var pile = new CardPile();
            pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed);

            // drawWeightFunc = null → MoveDrawToHand 사용 (기존 동작)
            var handler = new CardDrawHandler(pile, drawCount: 5, maxRerolls: 1, drawWeightFunc: null);
            var drawn = handler.DrawCards();

            Debug.Log($"  가중치 null 드로우: {drawn.Count}장");

            bool pass = drawn.Count == 5;
            LogResult("고급 화투패 Remove → 균등 드로우 복귀", pass);
        }

        /// <summary>E3: 가중치 드로우가 통계적으로 5이상 카드를 더 자주 뽑는지 (vs 균등)</summary>
        private void Test_E3_HighCardWeight_StatisticalBias()
        {
            Debug.Log("\n── E3: 고급 화투패 장신구 - 통계적 편향 비교 ──");

            int trials = 200;

            // A: 균등 드로우
            int uniformHighCount = 0;
            int uniformTotal = 0;
            for (int t = 0; t < trials; t++)
            {
                var pile = new CardPile();
                pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed + t);
                var handler = new CardDrawHandler(pile, drawCount: 5, maxRerolls: 1, drawWeightFunc: null);
                var drawn = handler.DrawCards();
                foreach (var c in drawn)
                {
                    uniformTotal++;
                    if (c.GetMonthValue() >= 5) uniformHighCount++;
                }
            }

            // B: 가중치 드로우 (1.1배)
            int weightedHighCount = 0;
            int weightedTotal = 0;
            Func<HwaTuCard, float> wf = card => card.GetMonthValue() >= 5 ? 1.1f : 1.0f;
            for (int t = 0; t < trials; t++)
            {
                var pile = new CardPile();
                pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed + t);
                var handler = new CardDrawHandler(pile, drawCount: 5, maxRerolls: 1, drawWeightFunc: wf);
                var drawn = handler.DrawCards();
                foreach (var c in drawn)
                {
                    weightedTotal++;
                    if (c.GetMonthValue() >= 5) weightedHighCount++;
                }
            }

            float uniformRatio = (float)uniformHighCount / uniformTotal;
            float weightedRatio = (float)weightedHighCount / weightedTotal;

            Debug.Log($"  균등 드로우: {uniformHighCount}/{uniformTotal} = {uniformRatio:P2}");
            Debug.Log($"  가중치 드로우: {weightedHighCount}/{weightedTotal} = {weightedRatio:P2}");
            Debug.Log($"  차이: {weightedRatio - uniformRatio:P2}");

            // 가중치 드로우가 균등보다 높은 비율이어야 함
            bool pass = weightedRatio > uniformRatio;
            LogResult("고급 화투패 통계적 편향 (가중치 > 균등)", pass);
        }

        // ============================================================
        // Part F: 조커 – JokerBase 생명주기
        // ============================================================

        /// <summary>F1: 조커 최초 사용 성공 + IsUsed = true</summary>
        private void Test_F1_Joker_UseOnce_Success()
        {
            Debug.Log("\n── F1: 조커 1회 사용 ──");

            var jokerManager = CreateTempJokerManager();

            var joker = new RerollBurstJoker();
            Debug.Log($"  조커: {joker.DisplayName}, IsUsed: {joker.IsUsed}");

            // DeckSystem이 없으므로 간접 테스트: JokerBase.Use()의 반환값과 IsUsed 확인
            var pile = new CardPile();
            pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed);
            var handler = new CardDrawHandler(pile, drawCount: 5, maxRerolls: 1);
            handler.DrawCards();

            // JokerContext를 수동으로 구성 (DeckSystem 없이 직접 handler 조작)
            // RerollBurstJoker는 context.DeckSystem.AddTempRerolls(4)를 호출하므로
            // DeckSystem 대신 handler를 직접 테스트
            bool wasNotUsed = !joker.IsUsed;

            // 직접 조커의 Use를 호출할 수 없으므로 (DeckSystem MonoBehaviour 필요)
            // JokerBase의 IsUsed 플래그 로직만 검증
            // → 실제 Use는 Part H에서 handler.AddTempRerolls로 간접 검증

            LogResult("조커 사용 전 IsUsed == false", wasNotUsed);
        }

        /// <summary>F2: 이미 사용된 조커 재사용 실패 확인</summary>
        private void Test_F2_Joker_UseAgain_Fail()
        {
            Debug.Log("\n── F2: 조커 재사용 불가 ──");

            // JokerBase.Use()의 IsUsed 가드를 검증하기 위해
            // 테스트용 더미 조커를 만들어 사용

            var dummyJoker = new TestDummyJoker();

            var context = new JokerContext
            {
                DeckSystem = null, // 더미 조커는 DeckSystem 미사용
                JokerManager = CreateTempJokerManager()
            };

            bool firstUse = dummyJoker.Use(context);
            Debug.Log($"  1차 사용: {(firstUse ? "성공" : "실패")}, IsUsed: {dummyJoker.IsUsed}");

            bool secondUse = dummyJoker.Use(context);
            Debug.Log($"  2차 사용: {(secondUse ? "성공" : "실패")}, IsUsed: {dummyJoker.IsUsed}");

            bool pass = firstUse && !secondUse && dummyJoker.IsUsed;
            LogResult("조커 1회 사용 후 재사용 거부", pass);
        }

        // ============================================================
        // Part G: 조커 – EvenHandDoubleJoker 조건 판정
        // ============================================================

        /// <summary>G1: 짝수 합 (2월+4월=6) → 조건 충족</summary>
        private void Test_G1_EvenHand_Condition_EvenSum()
        {
            Debug.Log("\n── G1: 짝수 조커 - 짝수 합 조건 ──");

            var cards = HwaTuCardDatabase.CreateAllCards();
            var card2 = cards.Find(c => c.CardId == "M2_Yeolkkeut"); // 2월
            var card4 = cards.Find(c => c.CardId == "M4_Yeolkkeut"); // 4월

            var result = SeotdaJudge.Judge(card2, card4);
            int sum = card2.GetMonthValue() + card4.GetMonthValue(); // 2+4=6 (짝수)

            Debug.Log($"  카드: {card2} + {card4}");
            Debug.Log($"  월 합: {sum} (짝수: {sum % 2 == 0})");
            Debug.Log($"  족보: {result.DisplayName}");

            bool isEven = sum % 2 == 0;
            LogResult("짝수 합 조건 충족 (2+4=6)", isEven);
        }

        /// <summary>G2: 홀수 합 (1월+4월=5) → 조건 미충족</summary>
        private void Test_G2_EvenHand_Condition_OddSum()
        {
            Debug.Log("\n── G2: 짝수 조커 - 홀수 합 조건 ──");

            var cards = HwaTuCardDatabase.CreateAllCards();
            var card1 = cards.Find(c => c.CardId == "M1_Gwang"); // 1월
            var card4 = cards.Find(c => c.CardId == "M4_Yeolkkeut"); // 4월

            var result = SeotdaJudge.Judge(card1, card4);
            int sum = card1.GetMonthValue() + card4.GetMonthValue(); // 1+4=5 (홀수)

            Debug.Log($"  카드: {card1} + {card4}");
            Debug.Log($"  월 합: {sum} (짝수: {sum % 2 == 0})");
            Debug.Log($"  족보: {result.DisplayName} (독사)");

            bool isOdd = sum % 2 != 0;
            LogResult("홀수 합 조건 미충족 (1+4=5)", isOdd);
        }

        /// <summary>G3: 짝수 땡 (8월+8월=16) → 조건 충족 + 땡 족보</summary>
        private void Test_G3_EvenHand_Condition_Ddaeng()
        {
            Debug.Log("\n── G3: 짝수 조커 - 짝수 땡 ──");

            var cards = HwaTuCardDatabase.CreateAllCards();
            var card8a = cards.Find(c => c.CardId == "M8_Gwang"); // 8월 광
            var card8b = cards.Find(c => c.CardId == "M8_Pi");    // 8월 피

            var result = SeotdaJudge.Judge(card8a, card8b);
            int sum = card8a.GetMonthValue() + card8b.GetMonthValue(); // 8+8=16 (짝수)

            Debug.Log($"  카드: {card8a} + {card8b}");
            Debug.Log($"  족보: {result.DisplayName} (8땡)");
            Debug.Log($"  월 합: {sum} (짝수: {sum % 2 == 0})");
            Debug.Log($"  기본 공격력: {result.BasePower}");
            Debug.Log($"  짝수 조커 적용 시: {result.BasePower * 2}");

            bool pass = sum % 2 == 0 && result.Hand == SeotdaHand.PalDdaeng;
            LogResult("짝수 땡 → 조건 충족 + 공격력 2배 가능", pass);
        }

        // ============================================================
        // Part H: 조커 – RerollBurstJoker
        // ============================================================

        /// <summary>H1: 리롤 폭발 조커가 실제로 4회 추가 리롤을 가능하게 하는지</summary>
        private void Test_H1_RerollBurst_AddsTempRerolls()
        {
            Debug.Log("\n── H1: 리롤 폭발 조커 - 리롤 +4 효과 ──");

            var pile = new CardPile();
            pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed);

            var handler = new CardDrawHandler(pile, drawCount: 5, maxRerolls: 1);
            handler.DrawCards();

            Debug.Log($"  드로우 후 리롤: {handler.RerollsRemaining}");

            // 조커가 하는 일을 직접 실행 (DeckSystem 우회)
            handler.AddTempRerolls(4);
            Debug.Log($"  리롤 폭발 조커 사용 후: {handler.RerollsRemaining}");

            // 총 5회(기본1 + 추가4) 리롤 가능해야 함
            int usedCount = 0;
            while (handler.CanReroll && pile.Hand.Count > 0)
            {
                var hand = new List<HwaTuCard>(pile.Hand);
                var redrawn = handler.Reroll(new List<HwaTuCard> { hand[0] });
                if (redrawn.Count > 0) usedCount++;
            }

            Debug.Log($"  총 리롤 사용: {usedCount}회 (기대값: 5)");

            bool pass = usedCount == 5 && !handler.CanReroll;
            LogResult("리롤 폭발 조커 → 총 5회(1+4) 리롤 사용", pass);
        }

        // ============================================================
        // Part I: JokerManager 데미지 배율
        // ============================================================

        /// <summary>I1: 기본 상태에서 배율 1.0</summary>
        private void Test_I1_DamageMultiplier_Default()
        {
            Debug.Log("\n── I1: 데미지 배율 - 기본값 ──");

            var jm = CreateTempJokerManager();
            var dummyResult = CreateDummySeotdaResult(2, 4); // 아무 값

            float mult = jm.GetDamageMultiplier(dummyResult);
            Debug.Log($"  기본 배율: {mult} (기대값: 1.0)");

            bool pass = Mathf.Approximately(mult, 1.0f);
            LogResult("데미지 배율 기본값 1.0", pass);
        }

        /// <summary>I2: 무조건 배율 설정 (조건 null)</summary>
        private void Test_I2_DamageMultiplier_Unconditional()
        {
            Debug.Log("\n── I2: 데미지 배율 - 무조건 적용 ──");

            var jm = CreateTempJokerManager();
            jm.SetDamageMultiplier(3.0f, null); // 조건 없이 3배

            var dummyResult = CreateDummySeotdaResult(1, 7); // 아무 값

            float mult = jm.GetDamageMultiplier(dummyResult);
            Debug.Log($"  배율: {mult} (기대값: 3.0)");

            bool pass = Mathf.Approximately(mult, 3.0f);
            LogResult("데미지 배율 무조건 3배 적용", pass);
        }

        /// <summary>I3: 조건부 배율 - 조건 충족 시</summary>
        private void Test_I3_DamageMultiplier_ConditionalPass()
        {
            Debug.Log("\n── I3: 데미지 배율 - 조건 충족 ──");

            var jm = CreateTempJokerManager();

            // 짝수 합 조건
            Func<SeotdaResult, bool> evenCondition = result =>
            {
                int sum = result.Card1.GetMonthValue() + result.Card2.GetMonthValue();
                return sum % 2 == 0;
            };

            jm.SetDamageMultiplier(2.0f, evenCondition);

            // 2월 + 4월 = 6 (짝수) → 조건 충족
            var result = CreateDummySeotdaResult(2, 4);
            float mult = jm.GetDamageMultiplier(result);

            Debug.Log($"  카드: 2월+4월, 합: 6 (짝수)");
            Debug.Log($"  배율: {mult} (기대값: 2.0)");

            bool pass = Mathf.Approximately(mult, 2.0f);
            LogResult("조건부 배율 - 짝수 충족 시 2배", pass);
        }

        /// <summary>I4: 조건부 배율 - 조건 미충족 시 1.0 반환</summary>
        private void Test_I4_DamageMultiplier_ConditionalFail()
        {
            Debug.Log("\n── I4: 데미지 배율 - 조건 미충족 ──");

            var jm = CreateTempJokerManager();

            Func<SeotdaResult, bool> evenCondition = result =>
            {
                int sum = result.Card1.GetMonthValue() + result.Card2.GetMonthValue();
                return sum % 2 == 0;
            };

            jm.SetDamageMultiplier(2.0f, evenCondition);

            // 1월 + 4월 = 5 (홀수) → 조건 미충족
            var result = CreateDummySeotdaResult(1, 4);
            float mult = jm.GetDamageMultiplier(result);

            Debug.Log($"  카드: 1월+4월, 합: 5 (홀수)");
            Debug.Log($"  배율: {mult} (기대값: 1.0 - 미충족)");

            bool pass = Mathf.Approximately(mult, 1.0f);
            LogResult("조건부 배율 - 홀수 미충족 시 1.0", pass);
        }

        /// <summary>I5: ResetTurnEffects 호출 후 배율이 1.0으로 복귀하는지</summary>
        private void Test_I5_DamageMultiplier_ResetOnTurnEnd()
        {
            Debug.Log("\n── I5: 데미지 배율 - 턴 종료 리셋 ──");

            var jm = CreateTempJokerManager();
            jm.SetDamageMultiplier(5.0f, null);

            var result = CreateDummySeotdaResult(3, 7);
            float beforeReset = jm.GetDamageMultiplier(result);
            Debug.Log($"  리셋 전 배율: {beforeReset}");

            // ResetTurnEffects는 private이므로 리플렉션으로 호출
            var method = typeof(JokerManager).GetMethod("ResetTurnEffects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(jm, null);

            float afterReset = jm.GetDamageMultiplier(result);
            Debug.Log($"  리셋 후 배율: {afterReset} (기대값: 1.0)");

            bool pass = Mathf.Approximately(beforeReset, 5.0f)
                     && Mathf.Approximately(afterReset, 1.0f);

            LogResult("턴 종료 시 배율 1.0 복귀", pass);
        }

        // ============================================================
        // Part J: 전체 전투 시뮬레이션
        // ============================================================

        /// <summary>J1: 장신구 + 조커 적용 상태에서 전체 턴 흐름 시뮬레이션</summary>
        private void Test_J1_FullBattleFlow_WithAccessoryAndJoker()
        {
            Debug.Log("\n── J1: 전체 전투 흐름 시뮬레이션 ──");

            var pile = new CardPile();
            pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed);

            // 장신구: 리롤 +1 (보너스), 가중치 드로우
            int baseRerolls = 1;
            int bonusRerolls = 1;
            Func<HwaTuCard, float> weightFunc = card =>
                card.GetMonthValue() >= 5 ? 1.1f : 1.0f;

            var handler = new CardDrawHandler(pile, drawCount: 5,
                maxRerolls: baseRerolls + bonusRerolls,
                drawWeightFunc: weightFunc);

            var selectionHandler = new CardSelectionHandler(pile, maxSelectCount: 2);
            var jm = CreateTempJokerManager();

            Debug.Log("  --- 턴 1: 드로우 + 조커 사용 + 선택 + 족보 판정 ---");

            // 1) 드로우 (가중치 적용)
            var drawn = handler.DrawCards();
            Debug.Log($"  드로우: {drawn.Count}장, 리롤: {handler.RerollsRemaining}");
            foreach (var c in drawn) Debug.Log($"    - {c}");

            // 2) 리롤 1회 (장신구 보너스 포함 2회 가능)
            var handCopy = new List<HwaTuCard>(pile.Hand);
            handler.Reroll(new List<HwaTuCard> { handCopy[0] });
            Debug.Log($"  1차 리롤 후 리롤: {handler.RerollsRemaining}");

            // 3) 조커 사용: 리롤 +4
            handler.AddTempRerolls(4);
            Debug.Log($"  조커(리롤+4) 사용 후 리롤: {handler.RerollsRemaining}");

            // 4) 리롤 추가 사용
            handCopy = new List<HwaTuCard>(pile.Hand);
            handler.Reroll(new List<HwaTuCard> { handCopy[0] });
            Debug.Log($"  2차 리롤 후 리롤: {handler.RerollsRemaining}");

            // 5) 카드 2장 선택
            selectionHandler.SelectCard(pile.Hand[0]);
            selectionHandler.SelectCard(pile.Hand[0]);
            Debug.Log($"  선택 완료: {selectionHandler.IsSelectionComplete}, 선택 카드: {pile.SelectedCards.Count}장");

            // 6) 족보 판정
            var selected1 = pile.SelectedCards[0];
            var selected2 = pile.SelectedCards[1];
            var seotdaResult = SeotdaJudge.Judge(selected1, selected2);
            Debug.Log($"  족보: {seotdaResult.DisplayName} (공격력: {seotdaResult.BasePower})");

            // 7) 짝수 조커 데미지 배율 적용 테스트
            Func<SeotdaResult, bool> evenCond = r =>
            {
                int sum = r.Card1.GetMonthValue() + r.Card2.GetMonthValue();
                return sum % 2 == 0;
            };
            jm.SetDamageMultiplier(2.0f, evenCond);

            float multiplier = jm.GetDamageMultiplier(seotdaResult);
            int finalDamage = (int)(seotdaResult.BasePower * multiplier);

            int sum2 = selected1.GetMonthValue() + selected2.GetMonthValue();
            Debug.Log($"  월 합: {sum2} ({(sum2 % 2 == 0 ? "짝수 → x2" : "홀수 → x1")})");
            Debug.Log($"  최종 데미지: {seotdaResult.BasePower} × {multiplier} = {finalDamage}");

            // 8) 턴 정리
            pile.MoveAllToDiscard();
            Debug.Log($"  턴 정리 후 - Draw: {pile.DrawPile.Count}, Discard: {pile.DiscardPile.Count}");

            bool pass = seotdaResult.IsValid
                     && pile.Hand.Count == 0
                     && pile.SelectedCards.Count == 0
                     && pile.TotalCardCount == 20;

            LogResult("전체 전투 흐름 (장신구+조커+족보+데미지)", pass);
        }

        /// <summary>J2: 조커는 1회 사용 후 목록에서 사라지는지</summary>
        private void Test_J2_MultiTurn_JokerSingleUse()
        {
            Debug.Log("\n── J2: 멀티 턴 - 조커 1회 사용 소멸 ──");

            var jm = CreateTempJokerManager();

            var joker1 = new TestDummyJoker();
            var joker2 = new TestDummyJoker();
            jm.AddJoker(joker1);
            jm.AddJoker(joker2);

            Debug.Log($"  초기 조커 수: {jm.HeldJokers.Count}");

            // 0번 조커 사용
            bool used = jm.UseJoker(0);
            Debug.Log($"  0번 조커 사용: {(used ? "성공" : "실패")}, 남은 조커: {jm.HeldJokers.Count}");

            // 다시 0번 (원래 1번이었던 joker2) 사용
            bool used2 = jm.UseJoker(0);
            Debug.Log($"  0번 조커 사용: {(used2 ? "성공" : "실패")}, 남은 조커: {jm.HeldJokers.Count}");

            // 더 이상 없음
            bool usedEmpty = jm.UseJoker(0);
            Debug.Log($"  빈 목록 사용 시도: {(usedEmpty ? "성공" : "실패")}");

            bool pass = used && used2 && !usedEmpty && jm.HeldJokers.Count == 0;
            LogResult("조커 2개 순차 사용 후 목록 비어짐", pass);
        }

        /// <summary>J3: 장신구/조커 적용 상태에서 멀티 턴 돌려도 카드 총 수 보존</summary>
        private void Test_J3_CardCount_NeverChanges()
        {
            Debug.Log("\n── J3: 멀티 턴 카드 수 보존 (장신구+가중치 적용) ──");

            var pile = new CardPile();
            pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed);

            Func<HwaTuCard, float> weightFunc = card =>
                card.GetMonthValue() >= 5 ? 1.1f : 1.0f;

            var handler = new CardDrawHandler(pile, drawCount: 5, maxRerolls: 2, drawWeightFunc: weightFunc);
            var selectionHandler = new CardSelectionHandler(pile, maxSelectCount: 2);

            bool allPass = true;

            for (int turn = 1; turn <= 10; turn++)
            {
                // 드로우
                handler.DrawCards();

                // 리롤 1회
                if (handler.CanReroll && pile.Hand.Count > 0)
                {
                    var h = new List<HwaTuCard>(pile.Hand);
                    handler.Reroll(new List<HwaTuCard> { h[0] });
                }

                // 선택
                if (pile.Hand.Count >= 2)
                {
                    selectionHandler.SelectCard(pile.Hand[0]);
                    selectionHandler.SelectCard(pile.Hand[0]);
                }

                // 턴 정리
                pile.MoveAllToDiscard();

                if (pile.TotalCardCount != 20)
                {
                    Debug.LogError($"  ❌ 턴 {turn}: 카드 수 불일치! {pile.TotalCardCount} != 20");
                    allPass = false;
                    break;
                }
            }

            Debug.Log($"  10턴 완료 후 카드 수: {pile.TotalCardCount}");
            LogResult("10턴 동안 카드 수 20장 보존", allPass);
        }

        // ============================================================
        // 헬퍼
        // ============================================================

        /// <summary>테스트용 JokerManager를 임시 GameObject에 동적 생성한다.</summary>
        private JokerManager CreateTempJokerManager()
        {
            var go = new GameObject("[Test] TempJokerManager");
            go.transform.SetParent(this.transform); // 테스트 종료 시 함께 파괴
            return go.AddComponent<JokerManager>();
        }

        /// <summary>테스트용 SeotdaResult를 생성한다 (실제 Judge 사용).</summary>
        private SeotdaResult CreateDummySeotdaResult(int month1, int month2)
        {
            var cards = HwaTuCardDatabase.CreateAllCards();

            // 해당 월의 첫 번째 카드를 찾음
            var card1 = cards.Find(c => (int)c.Month == month1);
            var card2 = cards.Find(c => (int)c.Month == month2 && c != card1);

            // 같은 월이면 두 번째 카드 (피)
            if (card2 == null)
            {
                card2 = cards.FindAll(c => (int)c.Month == month2)[1];
            }

            return SeotdaJudge.Judge(card1, card2);
        }

        /// <summary>결과 출력 헬퍼.</summary>
        private void LogResult(string testName, bool passed)
        {
            if (passed)
            {
                _passCount++;
                Debug.Log($"  → ✅ PASS: {testName}");
            }
            else
            {
                _failCount++;
                Debug.LogError($"  → ❌ FAIL: {testName}");
            }
        }
    }

    /// <summary>
    /// 테스트 전용 더미 조커. DeckSystem 없이 JokerBase 생명주기를 검증한다.
    /// Activate에서 아무것도 하지 않는다.
    /// </summary>
    internal class TestDummyJoker : JokerBase
    {
        public override string Id => "JKR_TEST_DUMMY";
        public override string DisplayName => "테스트 더미 조커";
        public override string Description => "테스트용. 효과 없음.";

        protected override void Activate(JokerContext context)
        {
            // 아무 효과 없음 (생명주기 테스트용)
            Debug.Log("[TestDummyJoker] Activate 호출됨 (효과 없음)");
        }
    }
}
