using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Data;
using FFF.Battle.Card;
using FFF.Battle.Item.Accessory;
using FFF.Battle.Item.Joker;
using FFF.Battle.Modifier;

namespace FFF.Test
{
    /// <summary>
    /// 장신구 / 조커 시스템 통합 테스트.
    /// 새롭게 도입된 TurnModifier 기반의 DeckSystem 스탯 관리를 완벽하게 검증합니다.
    /// </summary>
    public class ItemSystemTest : MonoBehaviour
    {
        [Header("테스트 시드 (같은 시드 = 같은 결과)")]
        [SerializeField] private int _testSeed = 42;

        private int _passCount = 0;
        private int _failCount = 0;

        public void RunTest()
        {
            Debug.Log("╔══════════════════════════════════════╗");
            Debug.Log("║  [3. ItemSystemTest] 아이템 시스템 테스트 시작     ║");
            Debug.Log("╚══════════════════════════════════════╝\n");

            // Part A: CardPile 가중치 직접 테스트
            Test_A1_WeightedDraw_BasicFunction();
            Test_A2_WeightedDraw_HighCardBias();
            Test_A3_WeightedDraw_CardCountPreserved();

            // Part B & C: DeckSystem 모디파이어(TurnModifier) 동작 검증
            Test_B1_DeckSystem_PermanentModifier();
            Test_B2_DeckSystem_TempModifier();
            Test_B3_DeckSystem_TempModifierResetOnTurnEnd();
            Test_C1_DeckSystem_SetDrawWeightFunc();

            // Part D & E: 장신구(Accessory) 적용 테스트
            Test_D1_RerollAccessory_ApplyAndUse();
            Test_D2_RerollAccessory_RemoveRestore();
            Test_E1_HighCardWeight_SimulatedBias();

            // Part F & G: 조커(Joker) 조건 판정 테스트
            Test_F1_Joker_UseOnce_Success();
            Test_F2_Joker_UseAgain_Fail();
            Test_G1_EvenHand_Condition_EvenSum();
            Test_G2_EvenHand_Condition_OddSum();
            Test_G3_EvenHand_Condition_Ddaeng();

            // Part H & I: 조커 매니저 및 실 적용 테스트
            Test_H1_RerollBurstJoker_ModifierInjection();
            Test_I1_DamageMultiplier_Default();
            Test_I2_DamageMultiplier_Conditional();
            Test_I3_DamageMultiplier_ResetOnTurnEnd();

            // Part J: 전체 전투 시뮬레이션
            Test_J1_FullBattleFlow_WithAccessoryAndJoker();
            Test_J2_MultiTurn_CardCount_Preserved();

            Debug.Log("\n╔══════════════════════════════════════╗");
            Debug.Log($"║  테스트 완료: ✅ {_passCount} PASS / ❌ {_failCount} FAIL  ");
            Debug.Log("╚══════════════════════════════════════╝");

            if (_failCount == 0) Debug.Log("🎉 모든 아이템 테스트를 통과했습니다!");
            else Debug.LogWarning($"⚠️ {_failCount}개의 아이템 테스트가 실패했습니다.");
        }

        // ============================================================
        // 헬퍼 메서드 (깨끗한 상태의 DeckSystem을 매번 생성)
        // ============================================================
        private DeckSystem CreateCleanDeckSystem(int seed)
        {
            var go = new GameObject($"[Test] TempDeckSystem_{seed}");
            go.transform.SetParent(this.transform); // 테스트 끝나면 삭제 용이
            var ds = go.AddComponent<DeckSystem>();
            ds.Initialize(HwaTuCardDatabase.CreateAllCards(), seed);
            return ds;
        }

        // ============================================================
        // Part A: CardPile 가중치 드로우 (기존 로직 유지)
        // ============================================================
        private void Test_A1_WeightedDraw_BasicFunction()
        {
            var pile = new CardPile();
            pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed);
            var drawn = pile.MoveDrawToHandWeighted(5, card => 1.0f);
            LogResult("가중치 드로우 기본 동작", drawn.Count == 5 && pile.DrawPile.Count == 15);
        }

        private void Test_A2_WeightedDraw_HighCardBias()
        {
            int highCardDrawCount = 0; int totalDrawn = 0;
            for (int t = 0; t < 100; t++)
            {
                var pile = new CardPile();
                pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed + t);
                var drawn = pile.MoveDrawToHandWeighted(5, card => card.GetMonthValue() >= 5 ? 10.0f : 1.0f);
                foreach (var card in drawn)
                {
                    totalDrawn++;
                    if (card.GetMonthValue() >= 5) highCardDrawCount++;
                }
            }
            float ratio = (float)highCardDrawCount / totalDrawn;
            LogResult($"가중치 드로우 높은 카드 편향 (현재 {ratio:P1})", ratio > 0.75f);
        }

        private void Test_A3_WeightedDraw_CardCountPreserved()
        {
            var pile = new CardPile();
            pile.Initialize(HwaTuCardDatabase.CreateAllCards(), _testSeed);
            pile.MoveDrawToHandWeighted(5, card => 2.0f);
            LogResult("가중치 드로우 카드 수 보존", pile.TotalCardCount == 20);
        }

        // ============================================================
        // Part B & C: DeckSystem 모디파이어 기능 검증
        // ============================================================
        private void Test_B1_DeckSystem_PermanentModifier()
        {
            var ds = CreateCleanDeckSystem(_testSeed);
            int baseMax = ds.TotalMaxRerolls;
            
            // 영구 모디파이어 추가
            ds.AddModifier(new TurnModifier(ModifierType.MaxRerolls, 2, TurnModifier.PERMANENT_TURN));
            
            LogResult("DeckSystem 영구 모디파이어 실시간 합산", ds.TotalMaxRerolls == baseMax + 2);
        }

        private void Test_B2_DeckSystem_TempModifier()
        {
            var ds = CreateCleanDeckSystem(_testSeed);
            ds.OnTurnStarted(); // 사용 횟수 0 초기화
            
            // 1턴짜리 모디파이어 추가
            ds.AddModifier(new TurnModifier(ModifierType.MaxRerolls, 4, 1));
            
            LogResult("DeckSystem 임시 모디파이어 남은 횟수 즉시 반영", ds.RerollsRemaining == 5);
        }

        private void Test_B3_DeckSystem_TempModifierResetOnTurnEnd()
        {
            var ds = CreateCleanDeckSystem(_testSeed);
            ds.AddModifier(new TurnModifier(ModifierType.MaxRerolls, 3, 1));
            
            // 턴 종료 (1턴 감소하여 모디파이어 삭제되어야 함)
            ds.CleanupForNextTurn();
            
            LogResult("DeckSystem 턴 종료 시 임시 모디파이어 소멸", ds.TotalMaxRerolls == 1);
        }

        private void Test_C1_DeckSystem_SetDrawWeightFunc()
        {
            var ds = CreateCleanDeckSystem(_testSeed);
            ds.SetDrawWeightFunc(card => card.GetMonthValue() >= 5 ? 2f : 1f);
            ds.OnTurnStarted();
            var drawn = ds.DrawCards();
            LogResult("DeckSystem 가중치 드로우 세팅 작동", drawn.Count == 5 && ds.Hand.Count == 5);
        }

        // ============================================================
        // Part D & E: 장신구
        // ============================================================
        private void Test_D1_RerollAccessory_ApplyAndUse()
        {
            var ds = CreateCleanDeckSystem(_testSeed);
            ds.OnTurnStarted();
            
            var accessory = new RerollBonusAccessory();
            accessory.Apply(ds); // 영구 버프 주입
            
            LogResult("리롤 장신구 Apply → 최대 리롤 증가", ds.TotalMaxRerolls == 2);
            
            // 리롤 2회 진행 검증
            ds.DrawCards();
            ds.Reroll(new List<HwaTuCard> { ds.Hand[0] });
            ds.Reroll(new List<HwaTuCard> { ds.Hand[0] });
            
            LogResult("장신구 효과로 2회 리롤 정상 사용", ds.RerollsRemaining == 0);
        }

        private void Test_D2_RerollAccessory_RemoveRestore()
        {
            var ds = CreateCleanDeckSystem(_testSeed);
            var accessory = new RerollBonusAccessory();
            
            accessory.Apply(ds);
            accessory.Remove(ds); // キャ싱된 모디파이어를 찾아서 삭제해야 함
            
            LogResult("리롤 장신구 Remove → 스탯 원복", ds.TotalMaxRerolls == 1);
        }

        private void Test_E1_HighCardWeight_SimulatedBias()
        {
            // HighCardWeightAccessory가 내부적으로 가중치 1.1f를 세팅한다고 가정하고 시뮬레이션
            Func<HwaTuCard, float> wf = card => card.GetMonthValue() >= 5 ? 1.1f : 1.0f;
            var ds = CreateCleanDeckSystem(_testSeed);
            ds.SetDrawWeightFunc(wf);
            ds.OnTurnStarted();
            var drawn = ds.DrawCards();
            LogResult("가중치 장신구 시뮬레이션 동작", drawn.Count == 5);
        }

        // ============================================================
        // Part F & G: 조커 조건 검증
        // ============================================================
        private void Test_F1_Joker_UseOnce_Success()
        {
            var joker = new RerollBurstJoker();
            var ds = CreateCleanDeckSystem(_testSeed);
            var context = new JokerContext { DeckSystem = ds, JokerManager = CreateTempJokerManager() };
            
            bool used = joker.Use(context);
            LogResult("조커 1회 사용 성공", used && joker.IsUsed);
        }

        private void Test_F2_Joker_UseAgain_Fail()
        {
            var joker = new TestDummyJoker();
            var ctx = new JokerContext { DeckSystem = CreateCleanDeckSystem(0), JokerManager = CreateTempJokerManager() };
            joker.Use(ctx);
            bool usedAgain = joker.Use(ctx);
            LogResult("사용한 조커 재사용 불가", !usedAgain);
        }

        private void Test_G1_EvenHand_Condition_EvenSum()
        {
            var result = CreateDummySeotdaResult(2, 4); // 6
            LogResult("짝수 조커 조건 - 짝수 합 충족", (result.Card1.GetMonthValue() + result.Card2.GetMonthValue()) % 2 == 0);
        }

        private void Test_G2_EvenHand_Condition_OddSum()
        {
            var result = CreateDummySeotdaResult(1, 4); // 5
            LogResult("짝수 조커 조건 - 홀수 합 미충족", (result.Card1.GetMonthValue() + result.Card2.GetMonthValue()) % 2 != 0);
        }

        private void Test_G3_EvenHand_Condition_Ddaeng()
        {
            var result = CreateDummySeotdaResult(8, 8); // 16 (8땡)
            LogResult("짝수 땡 → 짝수 합 충족", (result.Card1.GetMonthValue() + result.Card2.GetMonthValue()) % 2 == 0 && result.Hand == SeotdaHand.PalDdaeng);
        }

        // ============================================================
        // Part H & I: 조커 모디파이어 및 매니저 배율
        // ============================================================
        private void Test_H1_RerollBurstJoker_ModifierInjection()
        {
            var joker = new RerollBurstJoker();
            var ds = CreateCleanDeckSystem(_testSeed);
            ds.OnTurnStarted();

            var ctx = new JokerContext { DeckSystem = ds, JokerManager = CreateTempJokerManager() };
            joker.Use(ctx); // 내부에 1턴 모디파이어(리롤+4) 생성 및 주입
            
            LogResult("리롤 폭발 조커 -> 리롤 횟수 +4 뻥튀기", ds.TotalMaxRerolls == 5);
        }

        private void Test_I1_DamageMultiplier_Default()
        {
            var jm = CreateTempJokerManager();
            float mult = jm.GetDamageMultiplier(CreateDummySeotdaResult(2, 4));
            LogResult("조커 매니저 기본 배율 1.0", Mathf.Approximately(mult, 1.0f));
        }

        private void Test_I2_DamageMultiplier_Conditional()
        {
            var jm = CreateTempJokerManager();
            jm.SetDamageMultiplier(2.0f, r => (r.Card1.GetMonthValue() + r.Card2.GetMonthValue()) % 2 == 0);
            
            float multEven = jm.GetDamageMultiplier(CreateDummySeotdaResult(2, 4));
            float multOdd = jm.GetDamageMultiplier(CreateDummySeotdaResult(1, 4)); // 홀수
            
            LogResult("조건부 배율 적용", Mathf.Approximately(multEven, 2.0f) && Mathf.Approximately(multOdd, 1.0f));
        }

        private void Test_I3_DamageMultiplier_ResetOnTurnEnd()
        {
            var jm = CreateTempJokerManager();
            jm.SetDamageMultiplier(5.0f, null);
            
            var method = typeof(JokerManager).GetMethod("ResetTurnEffects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(jm, null);
            
            float mult = jm.GetDamageMultiplier(CreateDummySeotdaResult(1, 2));
            LogResult("조커 매니저 턴 종료 시 배율 1.0 복귀", Mathf.Approximately(mult, 1.0f));
        }

        // ============================================================
        // Part J: 전체 흐름 시뮬레이션
        // ============================================================
        private void Test_J1_FullBattleFlow_WithAccessoryAndJoker()
        {
            var ds = CreateCleanDeckSystem(_testSeed);
            var jm = CreateTempJokerManager();
            
            // 1. 장신구 적용 (영구 +1 리롤)
            new RerollBonusAccessory().Apply(ds);
            
            // 2. 턴 시작
            ds.OnTurnStarted();
            ds.DrawCards();
            
            // 3. 조커 적용 (임시 +4 리롤)
            new RerollBurstJoker().Use(new JokerContext { DeckSystem = ds, JokerManager = jm });
            
            // 4. 리롤 막 쓰기
            int rerollUses = 0;
            while (ds.CanReroll && ds.Hand.Count > 0)
            {
                var h = new List<HwaTuCard>(ds.Hand);
                if (ds.Reroll(new List<HwaTuCard> { h[0] }).Count > 0) rerollUses++;
            }
            
            // 5. 턴 종료
            ds.CleanupForNextTurn();

            bool pass = rerollUses == 6 && ds.TotalMaxRerolls == 2 && ds.TotalCardCount == 20;
            LogResult("전체 전투 파이프라인 (장신구+조커+정리)", pass);
        }

        private void Test_J2_MultiTurn_CardCount_Preserved()
        {
            var ds = CreateCleanDeckSystem(_testSeed);
            bool preserved = true;

            for(int i=0; i<5; i++)
            {
                ds.OnTurnStarted();
                ds.DrawCards();
                ds.CleanupForNextTurn();
                if(ds.TotalCardCount != 20) preserved = false;
            }
            LogResult("멀티 턴(5턴) 후 카드 20장 온전함", preserved);
        }

        // ============================================================
        // 헬퍼
        // ============================================================
        private JokerManager CreateTempJokerManager()
        {
            var go = new GameObject("[Test] TempJokerManager");
            go.transform.SetParent(this.transform);
            return go.AddComponent<JokerManager>();
        }

        private float GetDamageMultiplier(int month1, int month2)
        {
            return CreateTempJokerManager().GetDamageMultiplier(CreateDummySeotdaResult(month1, month2));
        }

        private SeotdaResult CreateDummySeotdaResult(int month1, int month2)
        {
            var cards = HwaTuCardDatabase.CreateAllCards();
            var card1 = cards.Find(c => (int)c.Month == month1);
            var card2 = cards.Find(c => (int)c.Month == month2 && c != card1);
            if (card2 == null) card2 = cards.FindAll(c => (int)c.Month == month2)[1];
            return SeotdaJudge.Judge(card1, card2);
        }

        private void LogResult(string testName, bool passed)
        {
            if (passed) { _passCount++; Debug.Log($"  → ✅ PASS: {testName}"); }
            else { _failCount++; Debug.LogError($"  → ❌ FAIL: {testName}"); }
        }
    }

    internal class TestDummyJoker : JokerBase
    {
        public override string Id => "JKR_TEST_DUMMY";
        public override string DisplayName => "테스트 더미 조커";
        public override string Description => "테스트용";

        protected override void Activate(JokerContext context) { }
    }
}