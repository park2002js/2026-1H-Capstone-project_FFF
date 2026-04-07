using System.Collections.Generic;
using UnityEngine;
using FFF.Data;
using FFF.Battle.Card;

namespace FFF.Test
{
    /// <summary>
    /// 카드 시스템 테스트용 MonoBehaviour.
    /// 
    /// 사용법:
    /// 1. 빈 Scene 생성 (예: TestScene)
    /// 2. 빈 GameObject에 이 스크립트를 붙인다.
    /// 3. Play 버튼을 누르면 Console에 테스트 결과가 출력된다.
    /// 
    /// DeckSystem(MonoBehaviour)을 통하지 않고,
    /// CardPile / CardDrawHandler / CardSelectionHandler를 직접 생성해서 테스트한다.
    /// SO Event 없이 순수 로직만 검증하는 용도.
    /// </summary>
    public class CardSystemTest : MonoBehaviour
    {
        [Header("테스트 시드 (같은 시드 = 같은 결과)")]
        [SerializeField] private int _testSeed = 42;

        private CardPile _pile;
        private CardDrawHandler _drawHandler;
        private CardSelectionHandler _selectionHandler;

        private void Start()
        {
            Debug.Log("========================================");
            Debug.Log("  카드 시스템 테스트 시작");
            Debug.Log("========================================\n");

            // 섯다용 카드만 사용 (현재 HwaTuCardDatabase.CreateAllCards()는 20장을 반환합니다)
            var allCards = HwaTuCardDatabase.CreateAllCards();

            Debug.Log($"[준비] 전체 카드: {allCards.Count}장\n");

            // 하위 시스템 생성
            _pile = new CardPile();
            _drawHandler = new CardDrawHandler(_pile, drawCount: 5, maxRerolls: 1);
            _selectionHandler = new CardSelectionHandler(_pile, maxSelectCount: 2);

            // 테스트 실행
            Test_Initialize(allCards);
            Test_DrawCards();
            Test_Reroll();
            Test_SelectCards();
            Test_CleanupForNextTurn();
            Test_MultiTurnFlow();
            Test_RecycleDiscard();

            Debug.Log("\n========================================");
            Debug.Log("  모든 테스트 완료!");
            Debug.Log("========================================");
        }

        /// <summary>테스트 1: 덱 초기화</summary>
        private void Test_Initialize(List<HwaTuCard> cards)
        {
            Debug.Log("── 테스트 1: 덱 초기화 ──");

            _pile.Initialize(cards, _testSeed);

            Debug.Log($"  DrawPile: {_pile.DrawPile.Count}장");
            Debug.Log($"  Hand: {_pile.Hand.Count}장");
            Debug.Log($"  SelectedCards: {_pile.SelectedCards.Count}장");
            Debug.Log($"  DiscardPile: {_pile.DiscardPile.Count}장");
            Debug.Log($"  전체: {_pile.TotalCardCount}장");

            bool pass = _pile.DrawPile.Count == 20
                     && _pile.Hand.Count == 0
                     && _pile.SelectedCards.Count == 0
                     && _pile.DiscardPile.Count == 0;

            LogResult("덱 초기화", pass);
        }

        /// <summary>테스트 2: 카드 드로우 (5장)</summary>
        private void Test_DrawCards()
        {
            Debug.Log("\n── 테스트 2: 카드 드로우 ──");

            var drawn = _drawHandler.DrawCards();

            Debug.Log($"  뽑은 카드 {drawn.Count}장:");
            foreach (var card in drawn)
            {
                Debug.Log($"    - {card}");
            }
            Debug.Log($"  DrawPile 남은: {_pile.DrawPile.Count}장");
            Debug.Log($"  Hand: {_pile.Hand.Count}장");

            bool pass = drawn.Count == 5
                     && _pile.Hand.Count == 5
                     && _pile.DrawPile.Count == 15;

            LogResult("카드 드로우", pass);
        }

        /// <summary>테스트 3: 리롤 (2장 교체)</summary>
        private void Test_Reroll()
        {
            Debug.Log("\n── 테스트 3: 리롤 ──");

            // 손패에서 앞 2장을 리롤 대상으로
            var handBefore = new List<HwaTuCard>(_pile.Hand);
            var toReturn = new List<HwaTuCard> { handBefore[0], handBefore[1] };

            Debug.Log($"  리롤 전 Hand: {_pile.Hand.Count}장");
            Debug.Log($"  리롤 대상: {toReturn[0]}, {toReturn[1]}");
            Debug.Log($"  남은 리롤 횟수: {_drawHandler.RerollsRemaining}");

            var redrawn = _drawHandler.Reroll(toReturn);

            Debug.Log($"  새로 뽑은 카드 {redrawn.Count}장:");
            foreach (var card in redrawn)
            {
                Debug.Log($"    - {card}");
            }
            Debug.Log($"  리롤 후 Hand: {_pile.Hand.Count}장");
            Debug.Log($"  남은 리롤 횟수: {_drawHandler.RerollsRemaining}");

            bool pass = redrawn.Count == 2
                     && _pile.Hand.Count == 5
                     && _drawHandler.RerollsRemaining == 0;

            LogResult("리롤", pass);

            // 리롤 소진 후 추가 리롤 시도
            Debug.Log("  [추가] 리롤 소진 후 재시도...");
            var failReroll = _drawHandler.Reroll(new List<HwaTuCard> { _pile.Hand[0] });
            bool passFail = failReroll.Count == 0;
            LogResult("리롤 소진 후 거부", passFail);
        }

        /// <summary>테스트 4: 카드 선택 (2장)</summary>
        private void Test_SelectCards()
        {
            Debug.Log("\n── 테스트 4: 카드 선택 ──");

            var card1 = _pile.Hand[0];
            var card2 = _pile.Hand[1]; // Hand[0] 선택 후 인덱스 밀림 고려

            // 1장 선택
            bool sel1 = _selectionHandler.SelectCard(card1);
            Debug.Log($"  1번 선택: {card1} → {(sel1 ? "성공" : "실패")}");
            Debug.Log($"  Hand: {_pile.Hand.Count}장, Selected: {_pile.SelectedCards.Count}장");

            // 2장 선택 (Hand에서 다시 가져와야 함 - card2가 아직 Hand에 있는지 확인)
            var card2Actual = _pile.Hand[0]; // 1장 빠졌으니 인덱스 0이 다음 카드
            bool sel2 = _selectionHandler.SelectCard(card2Actual);
            Debug.Log($"  2번 선택: {card2Actual} → {(sel2 ? "성공" : "실패")}");
            Debug.Log($"  Hand: {_pile.Hand.Count}장, Selected: {_pile.SelectedCards.Count}장");
            Debug.Log($"  선택 완료 여부: {_selectionHandler.IsSelectionComplete}");

            bool pass = _pile.SelectedCards.Count == 2
                     && _pile.Hand.Count == 3
                     && _selectionHandler.IsSelectionComplete;

            LogResult("카드 2장 선택", pass);

            // 3장째 선택 시도 (실패해야 함)
            if (_pile.Hand.Count > 0)
            {
                bool sel3 = _selectionHandler.SelectCard(_pile.Hand[0]);
                LogResult("3장째 선택 거부", !sel3);
            }

            // 선택 해제 테스트
            Debug.Log("  [추가] 선택 해제 테스트...");
            var deselect = _pile.SelectedCards[0];
            bool desel = _selectionHandler.DeselectCard(deselect);
            Debug.Log($"  선택 해제: {deselect} → {(desel ? "성공" : "실패")}");
            Debug.Log($"  Hand: {_pile.Hand.Count}장, Selected: {_pile.SelectedCards.Count}장");

            bool passDesel = _pile.SelectedCards.Count == 1 && _pile.Hand.Count == 4;
            LogResult("선택 해제", passDesel);

            // 다시 선택해서 2장 맞추기
            _selectionHandler.SelectCard(_pile.Hand[0]);
        }

        /// <summary>테스트 5: 턴 정리</summary>
        private void Test_CleanupForNextTurn()
        {
            Debug.Log("\n── 테스트 5: 턴 정리 ──");

            int handBefore = _pile.Hand.Count;
            int selectedBefore = _pile.SelectedCards.Count;
            Debug.Log($"  정리 전 - Hand: {handBefore}장, Selected: {selectedBefore}장, Discard: {_pile.DiscardPile.Count}장");

            _pile.MoveAllToDiscard();

            Debug.Log($"  정리 후 - Hand: {_pile.Hand.Count}장, Selected: {_pile.SelectedCards.Count}장, Discard: {_pile.DiscardPile.Count}장");

            bool pass = _pile.Hand.Count == 0
                     && _pile.SelectedCards.Count == 0
                     && _pile.DiscardPile.Count == (handBefore + selectedBefore);

            LogResult("턴 정리", pass);
            Debug.Log($"  전체 카드 수 검증: {_pile.TotalCardCount}장");
            LogResult("카드 수 보존", _pile.TotalCardCount == 20);
        }

        /// <summary>테스트 6: 멀티 턴 흐름 (3턴 연속)</summary>
        private void Test_MultiTurnFlow()
        {
            Debug.Log("\n── 테스트 6: 멀티 턴 흐름 (3턴 시뮬레이션) ──");

            for (int turn = 2; turn <= 4; turn++)
            {
                Debug.Log($"\n  --- 턴 {turn} ---");

                // 드로우
                var drawn = _drawHandler.DrawCards();
                Debug.Log($"  드로우: {drawn.Count}장 | DrawPile: {_pile.DrawPile.Count} | Hand: {_pile.Hand.Count}");

                // 카드 2장 선택
                if (_pile.Hand.Count >= 2)
                {
                    _selectionHandler.SelectCard(_pile.Hand[0]);
                    _selectionHandler.SelectCard(_pile.Hand[0]);
                    Debug.Log($"  선택: {_pile.SelectedCards.Count}장 | 완료: {_selectionHandler.IsSelectionComplete}");
                }

                // 턴 정리
                _pile.MoveAllToDiscard();
                Debug.Log($"  정리 후 - Draw: {_pile.DrawPile.Count} | Discard: {_pile.DiscardPile.Count} | Total: {_pile.TotalCardCount}");
            }

            LogResult("멀티 턴 흐름", _pile.TotalCardCount == 20);
        }

        /// <summary>테스트 7: 묘지 재활용 (DrawPile 고갈 시)</summary>
        private void Test_RecycleDiscard()
        {
            Debug.Log("\n── 테스트 7: 묘지 재활용 ──");

            Debug.Log($"  현재 DrawPile: {_pile.DrawPile.Count}장, DiscardPile: {_pile.DiscardPile.Count}장");

            // DrawPile이 고갈될 때까지 반복 드로우
            int turnCount = 0;
            int maxTurns = 20; // 안전장치

            while (turnCount < maxTurns)
            {
                turnCount++;
                var drawn = _drawHandler.DrawCards();

                // 간단히 2장 선택 후 정리
                if (_pile.Hand.Count >= 2)
                {
                    _selectionHandler.SelectCard(_pile.Hand[0]);
                    _selectionHandler.SelectCard(_pile.Hand[0]);
                }
                _pile.MoveAllToDiscard();

                if (_pile.DrawPile.Count < 5 && _pile.DiscardPile.Count > 0)
                {
                    Debug.Log($"  턴 {turnCount}: DrawPile {_pile.DrawPile.Count}장 (부족!) → 다음 드로우에서 재활용 발생 예상");
                    break;
                }
            }

            // 한 번 더 드로우 → 재활용 발생
            var drawnAfter = _drawHandler.DrawCards();
            Debug.Log($"  재활용 후 드로우: {drawnAfter.Count}장");
            Debug.Log($"  DrawPile: {_pile.DrawPile.Count}장, DiscardPile: {_pile.DiscardPile.Count}장");

            bool pass = drawnAfter.Count == 5 && _pile.TotalCardCount == 20;
            LogResult("묘지 재활용", pass);
        }

        /// <summary>결과 출력 헬퍼</summary>
        private void LogResult(string testName, bool passed)
        {
            string icon = passed ? "✅ PASS" : "❌ FAIL";
            Debug.Log($"  → {icon}: {testName}");
        }
    }
}
