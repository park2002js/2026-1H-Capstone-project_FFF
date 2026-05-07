using System;
using System.Collections.Generic;
using UnityEngine;

namespace FFF.UI.Animation
{
    /// <summary>
    /// 부모 Enemy GameObject에 부착하여, 여러 몬스터 외형(ForestMonster, WellGhost 등) 중
    /// 하나를 선택해 활성화하고, 그 외형의 CharacterAttackVisual / RectTransform을
    /// BattleAnimationController에 자동 주입한다.
    ///
    /// ── 사용법 ──
    /// - Inspector에 몬스터 항목들을 등록 (Id / Root / Visual / Character Rect)
    /// - DefaultId를 지정하면 Start 시 자동 선택 (시연/단일 스테이지용)
    /// - 스테이지 시스템 연동 시 외부에서 Select(id) 호출
    /// </summary>
    public class EnemyVisualSelector : MonoBehaviour
    {
        [Serializable]
        public class MonsterEntry
        {
            [Tooltip("스테이지 데이터에서 참조할 식별자 (예: \"ForestMonster\", \"WellGhost\")")]
            public string Id;

            [Tooltip("이 몬스터의 외곽 GameObject (예: ForestMonsterUI). Select 시 SetActive로 토글됨.")]
            public GameObject Root;

            [Tooltip("이 몬스터의 idle/attack 토글 컴포넌트")]
            public CharacterAttackVisual Visual;

            [Tooltip("돌진/피격 연출 대상 RectTransform")]
            public RectTransform CharacterRect;
        }

        [SerializeField] private List<MonsterEntry> _entries = new();

        [Tooltip("선택 결과를 주입받을 BattleAnimationController")]
        [SerializeField] private BattleAnimationController _animController;

        [Tooltip("Start 시 자동으로 선택할 기본 ID (단일 스테이지/시연용). 스테이지 시스템이 외부에서 Select() 호출 시 무시됨.")]
        [SerializeField] private string _defaultId;

        private string _currentId;

        public string CurrentId => _currentId;

        private void Start()
        {
            if (!string.IsNullOrEmpty(_defaultId))
                Select(_defaultId);
        }

        /// <summary>
        /// 지정한 ID의 몬스터만 활성화하고, BattleAnimationController에 외형/RectTransform을 주입한다.
        /// 스테이지 시작 시 BattleStartManager 또는 스테이지 컨트롤러에서 호출.
        /// </summary>
        public void Select(string id)
        {
            MonsterEntry chosen = null;

            foreach (var entry in _entries)
            {
                if (entry == null) continue;
                bool match = entry.Id == id;
                if (entry.Root != null) entry.Root.SetActive(match);
                if (match) chosen = entry;
            }

            if (chosen == null)
            {
                Debug.LogError($"[EnemyVisualSelector] '{id}'에 해당하는 몬스터 항목을 찾을 수 없습니다.");
                return;
            }

            _currentId = id;

            if (_animController != null)
            {
                _animController.SetEnemyVisual(chosen.Visual);
                _animController.SetEnemyCharacter(chosen.CharacterRect);
                Debug.Log($"[EnemyVisualSelector] 적 외형 주입 완료: {id}");
            }
            else
            {
                Debug.LogWarning("[EnemyVisualSelector] BattleAnimationController 참조가 비어 있어 주입을 생략합니다.");
            }
        }
    }
}
