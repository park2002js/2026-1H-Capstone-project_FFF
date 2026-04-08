using System.Collections.Generic;
using UnityEngine;
using TMPro; 
using FFF.UI.Core;

namespace FFF.UI.Battle
{
    /// <summary>
    /// 전투 화면을 그리는 역할만 담당합니다. (스스로 판단하지 않음)
    /// </summary>
    public class BattleUIComponent : BaseUIComponent
    {
        [Header("=== UI 연결 ===")]
        [SerializeField] private TextMeshProUGUI _playerHpText;
        [SerializeField] private TextMeshProUGUI _enemyHpText;
        [SerializeField] private Transform _accessoryLayoutGroup; // 장신구 아이콘 부모
        [SerializeField] private Transform _jokerLayoutGroup;     // 조커 아이콘 부모

        // 아이템 생성 시연용 임시 프리팹 (나중엔 리소스 로드로 변경 가능)
        [SerializeField] private GameObject _tempAccessoryIconPrefab;
        [SerializeField] private GameObject _tempJockerIconPrefab;

        public void SetPlayerHealth(int current, int max)
        {
            if (_playerHpText != null) 
                _playerHpText.text = $"Player HP: {current} / {max}";
            Debug.Log($"[BattleUI] 플레이어 체력 갱신: {current} / {max}");
        }

        public void SetEnemyHealth(int current, int max)
        {
            if (_enemyHpText != null) 
                _enemyHpText.text = $"Enemy HP: {current} / {max}";
            Debug.Log($"[BattleUI] 적 체력 갱신: {current} / {max}");
        }

        public void SetupItemIcons(List<string> accessoryIds, List<string> jokerIds)
        {
            // 실제 시연을 위해 하위 오브젝트로 더미 아이콘 생성 (프리팹이 있을 경우)
            if (_tempJockerIconPrefab != null && _tempAccessoryIconPrefab != null)
            {
                foreach(var acc in accessoryIds) Instantiate(_tempAccessoryIconPrefab, _accessoryLayoutGroup);
                foreach(var jkr in jokerIds) Instantiate(_tempJockerIconPrefab, _jokerLayoutGroup);
            }
            Debug.Log($"[BattleUI] 🎒 장신구 {accessoryIds.Count}개, 조커 {jokerIds.Count}개 아이콘 생성 (프리팹 기준)");
        }
    }
}