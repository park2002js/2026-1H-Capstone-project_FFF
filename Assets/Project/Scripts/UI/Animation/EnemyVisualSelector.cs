using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

            [Tooltip("이 몬스터와 함께 활성화할 배경 Root. 비워두면 Id/Root 이름으로 BackGroundUI 아래에서 자동 탐색합니다.")]
            public GameObject BackgroundRoot;
        }

        [SerializeField] private List<MonsterEntry> _entries = new();

        [Tooltip("선택 결과를 주입받을 BattleAnimationController")]
        [SerializeField] private BattleAnimationController _animController;

        [Tooltip("전투 배경들을 담고 있는 부모 Transform. 비워두면 이름이 BackGroundUI인 오브젝트를 자동 탐색합니다.")]
        [SerializeField] private Transform _backgroundRoot;

        [Tooltip("Start 시 자동으로 선택할 기본 ID (단일 스테이지/시연용). 스테이지 시스템이 외부에서 Select() 호출 시 무시됨.")]
        [SerializeField] private string _defaultId;

        [Header("=== 자동 일반 몬스터 등록 ===")]
        [SerializeField] private bool _autoRegisterNormalMonsterVisuals = true;
        [SerializeField] private string _characterAssetFolder = "Assets/Project/Art/Characters";
        [SerializeField] private string _battleBackgroundAssetFolder = "Assets/Project/Art/Background/BattleScene";
        [SerializeField] private Vector2 _autoMonsterPositionOffset = new Vector2(-90f, -80f);
        [SerializeField, Range(0.1f, 2f)] private float _autoMonsterSizeScale = 0.75f;
        [SerializeField] private Vector2 _autoMonsterHealthBarAlignmentOffset = new Vector2(-120f, -170f);

        private string _currentId;
        private bool _autoEntriesBuilt;

        public string CurrentId => _currentId;

        private void Start()
        {
            EnsureAutoMonsterEntries();

            if (!string.IsNullOrEmpty(_defaultId))
                Select(_defaultId);
        }

        public List<string> GetRegisteredIds()
        {
            EnsureAutoMonsterEntries();

            var ids = new List<string>();
            var usedIds = new HashSet<string>();

            foreach (var entry in _entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id) || !usedIds.Add(entry.Id))
                    continue;

                ids.Add(entry.Id);
            }

            return ids;
        }

        /// <summary>
        /// 지정한 ID의 몬스터만 활성화하고, BattleAnimationController에 외형/RectTransform을 주입한다.
        /// 스테이지 시작 시 BattleStartManager 또는 스테이지 컨트롤러에서 호출.
        /// </summary>
        public void Select(string id)
        {
            TrySelect(id);
        }

        public bool TrySelect(string id)
        {
            EnsureAutoMonsterEntries();

            MonsterEntry chosen = null;

            foreach (var entry in _entries)
            {
                if (entry == null) continue;
                bool match = entry.Id == id;
                if (entry.Root != null) entry.Root.SetActive(match);
                GameObject background = ResolveBackgroundRoot(entry);
                if (background != null) background.SetActive(match);
                if (match) chosen = entry;
            }

            if (chosen == null)
            {
                Debug.LogError($"[EnemyVisualSelector] '{id}'에 해당하는 몬스터 항목을 찾을 수 없습니다.");
                return false;
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

            return true;
        }

        private void EnsureAutoMonsterEntries()
        {
            if (_autoEntriesBuilt || !_autoRegisterNormalMonsterVisuals)
                return;

            _autoEntriesBuilt = true;

#if UNITY_EDITOR
            var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in _entries)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Id))
                    usedIds.Add(entry.Id);
            }

            if (!Directory.Exists(_characterAssetFolder))
                return;

            foreach (string idlePath in Directory.GetFiles(_characterAssetFolder, "*.png", SearchOption.TopDirectoryOnly))
            {
                string id = Path.GetFileNameWithoutExtension(idlePath);
                if (string.IsNullOrWhiteSpace(id) ||
                    id.EndsWith("Attack", StringComparison.OrdinalIgnoreCase) ||
                    IsExcludedNormalMonsterVisual(id) ||
                    !usedIds.Add(id))
                    continue;

                string attackPath = Path.Combine(_characterAssetFolder, $"{id}Attack.png").Replace("\\", "/");
                if (!File.Exists(attackPath))
                {
                    Debug.LogWarning($"[EnemyVisualSelector] {id} Attack 스프라이트가 없어 자동 등록을 건너뜁니다: {attackPath}");
                    continue;
                }

                Sprite idleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(idlePath.Replace("\\", "/"));
                Sprite attackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(attackPath);
                if (idleSprite == null || attackSprite == null)
                {
                    Debug.LogWarning($"[EnemyVisualSelector] {id} 스프라이트 로드 실패로 자동 등록을 건너뜁니다.");
                    continue;
                }

                _entries.Add(CreateRuntimeEntry(id, idleSprite, attackSprite));
            }
#endif
        }

        private MonsterEntry CreateRuntimeEntry(string id, Sprite idleSprite, Sprite attackSprite)
        {
            RectTransform templateRect = FindTemplateCharacterRect();

            GameObject root = new GameObject($"{id}UI", typeof(RectTransform));
            root.layer = gameObject.layer;
            root.transform.SetParent(transform, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            ApplyCharacterRectTemplate(rootRect, templateRect);
            ApplyAutoMonsterRectAdjustment(rootRect);

            GameObject idle = CreateVisualImage(id, root.transform, idleSprite, true);
            GameObject attack = CreateVisualImage($"{id}Attack", root.transform, attackSprite, false);

            CharacterAttackVisual visual = root.AddComponent<CharacterAttackVisual>();
            visual.Configure(idle, attack);

            root.SetActive(false);

            return new MonsterEntry
            {
                Id = id,
                Root = root,
                Visual = visual,
                CharacterRect = rootRect,
                BackgroundRoot = CreateRuntimeBackground(id)
            };
        }

        private RectTransform FindTemplateCharacterRect()
        {
            foreach (var entry in _entries)
            {
                if (entry != null && entry.CharacterRect != null)
                    return entry.CharacterRect;
            }

            return transform as RectTransform;
        }

        private void ApplyCharacterRectTemplate(RectTransform target, RectTransform template)
        {
            if (target == null)
                return;

            if (template != null)
            {
                target.anchorMin = template.anchorMin;
                target.anchorMax = template.anchorMax;
                target.pivot = template.pivot;
                target.sizeDelta = template.sizeDelta;
                target.anchoredPosition = template.anchoredPosition;
                target.localRotation = template.localRotation;
                target.localScale = template.localScale;
                return;
            }

            target.anchorMin = new Vector2(0.5f, 0.5f);
            target.anchorMax = new Vector2(0.5f, 0.5f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.sizeDelta = new Vector2(320f, 360f);
            target.anchoredPosition = Vector2.zero;
            target.localScale = Vector3.one;
        }

        private void ApplyAutoMonsterRectAdjustment(RectTransform target)
        {
            if (target == null)
                return;

            target.anchoredPosition += _autoMonsterPositionOffset;
            target.anchoredPosition += _autoMonsterHealthBarAlignmentOffset;
            target.sizeDelta *= _autoMonsterSizeScale;
        }

        private GameObject CreateVisualImage(string name, Transform parent, Sprite sprite, bool active)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            go.SetActive(active);
            return go;
        }

        private GameObject CreateRuntimeBackground(string id)
        {
#if UNITY_EDITOR
            Transform backgroundParent = ResolveBackgroundParent();
            if (backgroundParent == null)
                return null;

            Sprite sprite = LoadBackgroundSprite(id);
            if (sprite == null)
            {
                Debug.LogWarning($"[EnemyVisualSelector] {id} 배경 스프라이트를 찾지 못했습니다. 경로: {_battleBackgroundAssetFolder}");
                return null;
            }

            GameObject go = new GameObject($"{TrimEnemyNameSuffixes(id)}BackGround", typeof(RectTransform), typeof(Image));
            go.layer = backgroundParent.gameObject.layer;
            go.transform.SetParent(backgroundParent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            ApplyBackgroundRectTemplate(rect, FindTemplateBackgroundRect(backgroundParent));

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;

            go.SetActive(false);
            Debug.Log($"[EnemyVisualSelector] {id} 배경 자동 생성 완료: {sprite.name}");
            return go;
#else
            return null;
#endif
        }

        private RectTransform FindTemplateBackgroundRect(Transform backgroundParent)
        {
            if (backgroundParent == null)
                return null;

            foreach (var entry in _entries)
            {
                GameObject background = ResolveBackgroundRoot(entry);
                if (background != null && background.transform is RectTransform rect)
                    return rect;
            }

            foreach (Transform child in backgroundParent)
            {
                if (child is RectTransform rect)
                    return rect;
            }

            return backgroundParent as RectTransform;
        }

        private void ApplyBackgroundRectTemplate(RectTransform target, RectTransform template)
        {
            if (target == null)
                return;

            if (template != null)
            {
                target.anchorMin = template.anchorMin;
                target.anchorMax = template.anchorMax;
                target.pivot = template.pivot;
                target.sizeDelta = template.sizeDelta;
                target.anchoredPosition = template.anchoredPosition;
                target.localRotation = template.localRotation;
                target.localScale = template.localScale;
                target.SetSiblingIndex(template.GetSiblingIndex());
                return;
            }

            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
            target.localScale = Vector3.one;
        }

#if UNITY_EDITOR
        private Sprite LoadBackgroundSprite(string id)
        {
            if (!Directory.Exists(_battleBackgroundAssetFolder))
                return null;

            string idBase = TrimEnemyNameSuffixes(id);
            string normalizedId = NormalizeName(id);
            string normalizedBase = NormalizeName(idBase);

            foreach (string path in Directory.GetFiles(_battleBackgroundAssetFolder, "*.png", SearchOption.AllDirectories))
            {
                string normalizedFile = NormalizeName(Path.GetFileNameWithoutExtension(path));
                string normalizedFolder = NormalizeName(new DirectoryInfo(Path.GetDirectoryName(path)).Name);

                if (normalizedFolder == normalizedId ||
                    normalizedFolder == normalizedBase ||
                    normalizedFile == NormalizeName($"{id}BackGround") ||
                    normalizedFile == NormalizeName($"{idBase}BackGround"))
                {
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace("\\", "/"));
                }
            }

            return null;
        }
#endif

        private static bool IsExcludedNormalMonsterVisual(string id)
        {
            string normalized = NormalizeName(id);
            return normalized == NormalizeName("TheCat");
        }

        private GameObject ResolveBackgroundRoot(MonsterEntry entry)
        {
            if (entry == null)
                return null;

            if (entry.BackgroundRoot != null)
                return entry.BackgroundRoot;

            Transform backgroundParent = ResolveBackgroundParent();
            if (backgroundParent == null)
                return null;

            string[] candidates = CreateBackgroundNameCandidates(entry);
            Transform[] children = backgroundParent.GetComponentsInChildren<Transform>(true);
            foreach (string candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                string normalizedCandidate = NormalizeName(candidate);
                foreach (Transform child in children)
                {
                    if (child == null || child == backgroundParent)
                        continue;

                    if (NormalizeName(child.name) == normalizedCandidate)
                        return child.gameObject;
                }
            }

            return null;
        }

        private Transform ResolveBackgroundParent()
        {
            if (_backgroundRoot != null)
                return _backgroundRoot;

            Transform[] transforms = transform.root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate != null && NormalizeName(candidate.name) == NormalizeName("BackGroundUI"))
                {
                    _backgroundRoot = candidate;
                    return _backgroundRoot;
                }
            }

            return null;
        }

        private static string[] CreateBackgroundNameCandidates(MonsterEntry entry)
        {
            string idBase = TrimEnemyNameSuffixes(entry.Id);
            string rootBase = TrimEnemyNameSuffixes(entry.Root != null ? entry.Root.name : null);

            return new[]
            {
                $"{entry.Id}BackGround",
                $"{entry.Id}Background",
                $"{idBase}BackGround",
                $"{idBase}Background",
                $"{rootBase}BackGround",
                $"{rootBase}Background"
            };
        }

        private static string TrimEnemyNameSuffixes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string result = value.Trim();
            result = TrimSuffix(result, "UI");
            result = TrimSuffix(result, "Monster");
            result = TrimSuffix(result, "Ghost");
            return result;
        }

        private static string TrimSuffix(string value, string suffix)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(suffix))
                return value;

            return value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                ? value.Substring(0, value.Length - suffix.Length)
                : value;
        }

        private static string NormalizeName(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
        }
    }
}
