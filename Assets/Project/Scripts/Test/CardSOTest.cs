using System.Collections.Generic;
using UnityEngine;
using FFF.Data;
using FFF.Battle.Card;

namespace FFF.Test
{
    /// <summary>
    /// SO 카드 로딩 + 기존 시스템 호환성 테스트.
    /// 빈 GameObject에 붙이고 Play하면 Console에 결과 출력.
    /// 이 스크립트 단 하나만 씬에 존재하면, 나머지 테스트들도 자동으로 연계 실행됩니다.
    /// </summary>
    public class CardSOTest : MonoBehaviour
    {
        private int _pass;
        private int _fail;

        private void Start()
        {
            _pass = 0;
            _fail = 0;

            Debug.Log("========== [1. CardSOTest] 테스트 시작 ==========");

            Test_LoadAllCards();
            Test_CardCount();
            Test_NoDuplicateIds();
            Test_GwangMonths();
            Test_CardPileCompatibility();
            Test_SeotdaJudgeCompatibility();

            Debug.Log($"========== [1. CardSOTest] 결과: {_pass} 통과 / {_fail} 실패 ==========\n");

            // 요구사항 1번: 이 스크립트 하나로 모든 테스트가 동작하도록 나머지 테스트 자동 실행
            var cardSystemTest = gameObject.AddComponent<CardSystemTest>();
            cardSystemTest.RunTest();

            // var itemSystemTest = gameObject.AddComponent<ItemSystemTest>();
            // itemSystemTest.RunTest();
        }

        /// <summary>1. SO 로드가 되는지</summary>
        private void Test_LoadAllCards()
        {
            var cards = HwaTuCardDatabase.CreateAllCards();
            Assert(cards != null && cards.Count > 0, "SO 로드", $"{cards?.Count}장 로드됨");
        }

        /// <summary>2. 정확히 20장인지</summary>
        private void Test_CardCount()
        {
            var cards = HwaTuCardDatabase.CreateAllCards();
            Assert(cards.Count == 20, "카드 수 == 20", $"실제: {cards.Count}장");
        }

        /// <summary>3. CardId 중복 없는지</summary>
        private void Test_NoDuplicateIds()
        {
            var cards = HwaTuCardDatabase.CreateAllCards();
            var ids = new HashSet<string>();
            bool hasDup = false;
            foreach (var c in cards)
            {
                if (!ids.Add(c.CardId))
                {
                    Debug.LogError($"  중복 CardId 발견: {c.CardId}");
                    hasDup = true;
                }
            }
            Assert(!hasDup, "CardId 중복 없음", hasDup ? "중복 있음" : "모두 고유");
        }

        /// <summary>4. 광 카드가 1, 3, 8월에만 있는지</summary>
        private void Test_GwangMonths()
        {
            var cards = HwaTuCardDatabase.CreateAllCards();
            var gwangs = cards.FindAll(c => c.Type == CardType.Gwang);
            bool correctCount = gwangs.Count == 3;
            bool correctMonths = gwangs.Exists(c => c.Month == CardMonth.January)
                              && gwangs.Exists(c => c.Month == CardMonth.March)
                              && gwangs.Exists(c => c.Month == CardMonth.August);
            Assert(correctCount && correctMonths, "광 = 1,3,8월 각 1장",
                $"광 {gwangs.Count}장, 월: {string.Join(", ", gwangs.ConvertAll(c => c.Month.ToString()))}");
        }

        /// <summary>5. CardPile에 넣어서 셔플/드로우 되는지</summary>
        private void Test_CardPileCompatibility()
        {
            var cards = HwaTuCardDatabase.CreateAllCards();
            var pile = new CardPile();
            pile.Initialize(cards);

            bool totalOk = pile.TotalCardCount == 20;
            bool drawOk = pile.DrawPile.Count == 20;

            var drawn = pile.MoveDrawToHand(5);
            bool handOk = pile.Hand.Count == 5;
            bool remainOk = pile.DrawPile.Count == 15;

            Assert(totalOk && drawOk && handOk && remainOk, "CardPile 호환",
                $"Total={pile.TotalCardCount}, Hand={pile.Hand.Count}, Draw={pile.DrawPile.Count}");
        }

        /// <summary>6. SeotdaJudge에 넣어서 족보 판정되는지</summary>
        private void Test_SeotdaJudgeCompatibility()
        {
            var card1 = new HwaTuCard(CardMonth.January, CardType.Gwang, "M1_Gwang", "1월 광");
            var card3 = new HwaTuCard(CardMonth.March, CardType.Gwang, "M3_Gwang", "3월 광");

            var hand = SeotdaJudge.Judge(card1, card3);
            Assert(hand.Hand == SeotdaHand.IlSamGwangDdaeng, "SeotdaJudge 호환 (1-3 광땡)",
                $"판정 결과: {hand.Hand}");
        }

        private void Assert(bool condition, string testName, string detail)
        {
            if (condition)
            {
                _pass++;
                Debug.Log($"  [PASS] {testName} — {detail}");
            }
            else
            {
                _fail++;
                Debug.LogError($"  [FAIL] {testName} — {detail}");
            }
        }
    }
}