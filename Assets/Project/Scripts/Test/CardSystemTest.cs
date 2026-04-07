using System.Collections.Generic;
using UnityEngine;
using FFF.Data;
using FFF.Battle.Card;

namespace FFF.Test
{
    /// <summary>
    /// 카드 시스템 테스트.
    /// DeckSystem(단일 진실 공급원)을 생성하여 턴 흐름과 카드 로직을 검증합니다.
    /// </summary>
    public class CardSystemTest : MonoBehaviour
    {
        [Header("테스트 시드 (같은 시드 = 같은 결과)")]
        [SerializeField] private int _testSeed = 42;

        private DeckSystem _deckSystem;
        private int _passCount = 0;
        private int _failCount = 0;

        public void RunTest()
        {
            Debug.Log("========================================");
            Debug.Log("  [2. CardSystemTest] 카드 시스템 테스트 시작");
            Debug.Log("========================================\n");

            var allCards = HwaTuCardDatabase.CreateAllCards();
            Debug.Log($"[준비] 전체 카드: {allCards.Count}장\n");

            // 테스트용 DeckSystem 동적 생성
            _deckSystem = gameObject.AddComponent<DeckSystem>();
            _deckSystem.Initialize(allCards, _testSeed);

            // 테스트 실행
            Test_Initialize();
            Test_DrawCards();
            Test_Reroll();
            Test_SelectCards();
            Test_CleanupForNextTurn();
            Test_MultiTurnFlow();
            Test_RecycleDiscard();

            Debug.Log($"\n========== [2. CardSystemTest] 결과: {_passCount} 통과 / {_failCount} 실패 ==========\n");
        }

        private void Test_Initialize()
        {
            Debug.Log("── 테스트 1: 덱 초기화 ──");

            Debug.Log($"  DrawPile: {_deckSystem.DrawPile.Count}장");
            Debug.Log($"  Hand: {_deckSystem.Hand.Count}장");
            Debug.Log($"  SelectedCards: {_deckSystem.SelectedCards.Count}장");
            Debug.Log($"  DiscardPile: {_deckSystem.DiscardPile.Count}장");
            Debug.Log($"  전체: {_deckSystem.TotalCardCount}장");

            bool pass = _deckSystem.DrawPile.Count == 20
                     && _deckSystem.Hand.Count == 0
                     && _deckSystem.SelectedCards.Count == 0
                     && _deckSystem.DiscardPile.Count == 0;

            LogResult("덱 초기화", pass);
        }

        private void Test_DrawCards()
        {
            Debug.Log("\n── 테스트 2: 카드 드로우 ──");

            // 턴이 시작되었음을 알림 (리롤 횟수 계산용)
            _deckSystem.OnTurnStarted();
            var drawn = _deckSystem.DrawCards();

            Debug.Log($"  뽑은 카드 {drawn.Count}장:");
            foreach (var card in drawn)
            {
                Debug.Log($"    - {card}");
            }
            Debug.Log($"  DrawPile 남은: {_deckSystem.DrawPile.Count}장");
            Debug.Log($"  Hand: {_deckSystem.Hand.Count}장");

            bool pass = drawn.Count == 5
                     && _deckSystem.Hand.Count == 5
                     && _deckSystem.DrawPile.Count == 15;

            LogResult("카드 드로우", pass);
        }

        private void Test_Reroll()
        {
            Debug.Log("\n── 테스트 3: 리롤 ──");

            var handBefore = new List<HwaTuCard>(_deckSystem.Hand);
            var toReturn = new List<HwaTuCard> { handBefore[0], handBefore[1] };

            Debug.Log($"  리롤 전 Hand: {_deckSystem.Hand.Count}장");
            Debug.Log($"  리롤 대상: {toReturn[0]}, {toReturn[1]}");
            Debug.Log($"  남은 리롤 횟수: {_deckSystem.RerollsRemaining}");

            var redrawn = _deckSystem.Reroll(toReturn);

            Debug.Log($"  새로 뽑은 카드 {redrawn.Count}장:");
            foreach (var card in redrawn)
            {
                Debug.Log($"    - {card}");
            }
            Debug.Log($"  리롤 후 Hand: {_deckSystem.Hand.Count}장");
            Debug.Log($"  남은 리롤 횟수: {_deckSystem.RerollsRemaining}");

            bool pass = redrawn.Count == 2
                     && _deckSystem.Hand.Count == 5
                     && _deckSystem.RerollsRemaining == 0;

            LogResult("리롤", pass);

            Debug.Log("  [추가] 리롤 소진 후 재시도...");
            var failReroll = _deckSystem.Reroll(new List<HwaTuCard> { _deckSystem.Hand[0] });
            bool passFail = failReroll.Count == 0;
            LogResult("리롤 소진 후 거부", passFail);
        }

        private void Test_SelectCards()
        {
            Debug.Log("\n── 테스트 4: 카드 선택 ──");

            var card1 = _deckSystem.Hand[0];
            bool sel1 = _deckSystem.SelectCard(card1);
            Debug.Log($"  1번 선택: {card1} → {(sel1 ? "성공" : "실패")}");
            Debug.Log($"  Hand: {_deckSystem.Hand.Count}장, Selected: {_deckSystem.SelectedCards.Count}장");

            var card2Actual = _deckSystem.Hand[0];
            bool sel2 = _deckSystem.SelectCard(card2Actual);
            Debug.Log($"  2번 선택: {card2Actual} → {(sel2 ? "성공" : "실패")}");
            Debug.Log($"  Hand: {_deckSystem.Hand.Count}장, Selected: {_deckSystem.SelectedCards.Count}장");
            Debug.Log($"  선택 완료 여부: {_deckSystem.IsSelectionComplete}");

            bool pass = _deckSystem.SelectedCards.Count == 2
                     && _deckSystem.Hand.Count == 3
                     && _deckSystem.IsSelectionComplete;

            LogResult("카드 2장 선택", pass);

            if (_deckSystem.Hand.Count > 0)
            {
                bool sel3 = _deckSystem.SelectCard(_deckSystem.Hand[0]);
                LogResult("3장째 선택 거부", !sel3);
            }

            Debug.Log("  [추가] 선택 해제 테스트...");
            var deselect = _deckSystem.SelectedCards[0];
            bool desel = _deckSystem.DeselectCard(deselect);
            Debug.Log($"  선택 해제: {deselect} → {(desel ? "성공" : "실패")}");
            Debug.Log($"  Hand: {_deckSystem.Hand.Count}장, Selected: {_deckSystem.SelectedCards.Count}장");

            bool passDesel = _deckSystem.SelectedCards.Count == 1 && _deckSystem.Hand.Count == 4;
            LogResult("선택 해제", passDesel);

            _deckSystem.SelectCard(_deckSystem.Hand[0]);
        }

        private void Test_CleanupForNextTurn()
        {
            Debug.Log("\n── 테스트 5: 턴 정리 ──");

            int handBefore = _deckSystem.Hand.Count;
            int selectedBefore = _deckSystem.SelectedCards.Count;
            Debug.Log($"  정리 전 - Hand: {handBefore}장, Selected: {selectedBefore}장, Discard: {_deckSystem.DiscardPile.Count}장");

            _deckSystem.CleanupForNextTurn();

            Debug.Log($"  정리 후 - Hand: {_deckSystem.Hand.Count}장, Selected: {_deckSystem.SelectedCards.Count}장, Discard: {_deckSystem.DiscardPile.Count}장");

            bool pass = _deckSystem.Hand.Count == 0
                     && _deckSystem.SelectedCards.Count == 0
                     && _deckSystem.DiscardPile.Count == (handBefore + selectedBefore);

            LogResult("턴 정리", pass);
            Debug.Log($"  전체 카드 수 검증: {_deckSystem.TotalCardCount}장");
            LogResult("카드 수 보존", _deckSystem.TotalCardCount == 20);
        }

        private void Test_MultiTurnFlow()
        {
            Debug.Log("\n── 테스트 6: 멀티 턴 흐름 (3턴 시뮬레이션) ──");

            for (int turn = 2; turn <= 4; turn++)
            {
                Debug.Log($"\n  --- 턴 {turn} ---");
                
                _deckSystem.OnTurnStarted();
                var drawn = _deckSystem.DrawCards();
                Debug.Log($"  드로우: {drawn.Count}장 | DrawPile: {_deckSystem.DrawPile.Count} | Hand: {_deckSystem.Hand.Count}");

                if (_deckSystem.Hand.Count >= 2)
                {
                    _deckSystem.SelectCard(_deckSystem.Hand[0]);
                    _deckSystem.SelectCard(_deckSystem.Hand[0]);
                    Debug.Log($"  선택: {_deckSystem.SelectedCards.Count}장 | 완료: {_deckSystem.IsSelectionComplete}");
                }

                _deckSystem.CleanupForNextTurn();
                Debug.Log($"  정리 후 - Draw: {_deckSystem.DrawPile.Count} | Discard: {_deckSystem.DiscardPile.Count} | Total: {_deckSystem.TotalCardCount}");
            }

            LogResult("멀티 턴 흐름", _deckSystem.TotalCardCount == 20);
        }

        private void Test_RecycleDiscard()
        {
            Debug.Log("\n── 테스트 7: 묘지 재활용 ──");

            Debug.Log($"  현재 DrawPile: {_deckSystem.DrawPile.Count}장, DiscardPile: {_deckSystem.DiscardPile.Count}장");

            int turnCount = 0;
            int maxTurns = 20;

            while (turnCount < maxTurns)
            {
                turnCount++;
                _deckSystem.OnTurnStarted();
                var drawn = _deckSystem.DrawCards();

                if (_deckSystem.Hand.Count >= 2)
                {
                    _deckSystem.SelectCard(_deckSystem.Hand[0]);
                    _deckSystem.SelectCard(_deckSystem.Hand[0]);
                }
                
                _deckSystem.CleanupForNextTurn();

                if (_deckSystem.DrawPile.Count < 5 && _deckSystem.DiscardPile.Count > 0)
                {
                    Debug.Log($"  턴 {turnCount}: DrawPile {_deckSystem.DrawPile.Count}장 (부족!) → 다음 드로우에서 재활용 발생 예상");
                    break;
                }
            }

            _deckSystem.OnTurnStarted();
            var drawnAfter = _deckSystem.DrawCards();
            Debug.Log($"  재활용 후 드로우: {drawnAfter.Count}장");
            Debug.Log($"  DrawPile: {_deckSystem.DrawPile.Count}장, DiscardPile: {_deckSystem.DiscardPile.Count}장");

            bool pass = drawnAfter.Count == 5 && _deckSystem.TotalCardCount == 20;
            LogResult("묘지 재활용", pass);
        }

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
}