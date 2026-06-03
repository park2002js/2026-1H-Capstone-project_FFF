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