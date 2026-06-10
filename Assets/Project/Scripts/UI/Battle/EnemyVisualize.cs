using UnityEngine;
using UnityEngine.UI;
using FFF.Data;
using FFF.UI.Animation;

namespace FFF.UI.Battle
{
    /// <summary>
    /// 적의 데이터를 기반으로 UI 이미지를 설정하고 애니메이션 컨트롤러에 등록함.
    /// (기존 EnemyVisualSelector 스크립트 완벽 대체 목적)
    /// </summary>
    public class EnemyVisualize : MonoBehaviour
    {
        // 몬스터별 Idle/Attack 이미지 위치·크기 보정값.
        // (X, Y) = RectTransform.anchoredPosition, (Width, Height) = RectTransform.sizeDelta.
        // 스프라이트 이름(IdleSprite/AttackSprite) 또는 EnemyId/EnemyName에 키워드가 포함되면 적용된다.
        private static readonly EnemyImageOverride[] EnemyImageOverrides =
        {
            new EnemyImageOverride("MaidenGhost", new Vector2(736f, 260f), new Vector2(584f, 709f)),
            new EnemyImageOverride("Wolf",        new Vector2(725f, 187f), new Vector2(1033f, 735f)),
            new EnemyImageOverride("Warrior",     new Vector2(690f, 248f), new Vector2(716f, 792f)),
            new EnemyImageOverride("Jangseung",   new Vector2(696f, 299f), new Vector2(863f, 918f)),
            new EnemyImageOverride("WellGhost",   new Vector2(714f, 109f), new Vector2(530f, 543f)),
            new EnemyImageOverride("Elite",       new Vector2(735f, 211f), new Vector2(584f, 646f)),
            new EnemyImageOverride("Haetae",      new Vector2(739f, 200f), new Vector2(877f, 646f)),
            new EnemyImageOverride("GrimReaper",  new Vector2(697f, 279f), new Vector2(959f, 844f)),
            new EnemyImageOverride("Wisp",        new Vector2(718f, 172f), new Vector2(850f, 767f)),
            new EnemyImageOverride("Crow",        new Vector2(775f, 272f), new Vector2(1067f, 850f)),
        };

        [Header("=== UI 컴포넌트 참조 ===")]
        [Tooltip("평상시 외형을 출력할 UI Image 컴포넌트")]
        [SerializeField] private Image _idleImage;
        [Tooltip("공격 시 외형을 출력할 UI Image 컴포넌트")]
        [SerializeField] private Image _attackImage;
        [Tooltip("전투 배경을 출력할 UI Image 컴포넌트")]
        [SerializeField] private Image _backgroundImage;

        [Header("=== 외형 크기 ===")]
        [Tooltip("적 캐릭터 이미지 크기 배율. 0.7이면 원래 RectTransform 크기의 70%로 표시됩니다.")]
        [SerializeField, Range(0.1f, 1f)] private float _characterImageScale = 0.7f;
        [Tooltip("적 캐릭터 이미지 위치 보정. 체력바/공격 기준점은 그대로 두고 그림만 이동합니다.")]
        [SerializeField] private Vector2 _characterImageOffset = new Vector2(0f, -93f);

        [Header("=== 애니메이션 연동 참조 ===")]
        [Tooltip("Idle/Attack 토글을 관리하는 로컬 컴포넌트 (CharacterAttackVisual.cs)")]
        [SerializeField] private CharacterAttackVisual _characterVisual;
        [Tooltip("돌진/피격 연출의 기준이 되는 RectTransform (Attack RectTransform)")]
        [SerializeField] private RectTransform _characterRect;
        [Tooltip("연출을 총괄하는 배틀 애니메이션 컨트롤러")]
        [SerializeField] private BattleAnimationController _animController;

        private Vector2 _idleBaseSize;
        private Vector2 _attackBaseSize;
        private Vector2 _idleBasePosition;
        private Vector2 _attackBasePosition;
        private bool _hasCachedBaseSize;
        private EnemyDataSO _currentEnemyData;

        private void Awake()
        {
            CacheBaseImageSizes();
        }

        private void Start()
        {
            ApplyCharacterImageScale();
        }

        /// <summary>
        /// 전달받은 적 SO 데이터를 통해 이미지를 할당하고 컨트롤러에 연동함.
        /// </summary>
        /// <param name="enemyData">이번 전투에 할당된 적 SO 데이터</param>
        public void SetupVisual(EnemyDataSO enemyData)
        {
            if (enemyData == null)
            {
                Debug.LogWarning("[EnemyVisualize] 전달된 EnemyDataSO가 없음.");
                return;
            }

            _currentEnemyData = enemyData;

            // 1. 데이터에 기반하여 UI 이미지 스프라이트 갱신
            if (_idleImage != null) _idleImage.sprite = enemyData.IdleSprite;
            if (_attackImage != null) _attackImage.sprite = enemyData.AttackSprite;
            if (_backgroundImage != null) _backgroundImage.sprite = enemyData.BackgroundSprite;
            ApplyCharacterImageScale();

            // 2. BattleAnimationController에 애니메이션 연출용 객체 주입
            if (_animController != null)
            {
                _animController.SetEnemyVisual(_characterVisual);
                _animController.SetEnemyCharacter(_characterRect);
                Debug.Log($"[EnemyVisualize] 적 외형 및 배경 셋업 완료: {enemyData.EnemyName}");
            }
            else
            {
                Debug.LogWarning("[EnemyVisualize] BattleAnimationController 참조가 누락되어 연출 주입 불가함.");
            }
        }

        private void ApplyCharacterImageScale()
        {
            CacheBaseImageSizes();

            float scale = Mathf.Clamp(_characterImageScale, 0.1f, 1f);
            ApplyImageSize(_idleImage, _idleBaseSize * scale);
            ApplyImageSize(_attackImage, _attackBaseSize * scale);
            ApplyImagePosition(_idleImage, _idleBasePosition + _characterImageOffset);
            ApplyImagePosition(_attackImage, _attackBasePosition + _characterImageOffset);

            // 몬스터별 보정값이 있으면 Idle/Attack 두 이미지 모두 해당 위치·크기로 덮어쓴다.
            if (TryGetEnemyImageOverride(_currentEnemyData, out Vector2 overridePosition, out Vector2 overrideSize))
            {
                ApplyImageSize(_idleImage, overrideSize);
                ApplyImageSize(_attackImage, overrideSize);
                ApplyImagePosition(_idleImage, overridePosition);
                ApplyImagePosition(_attackImage, overridePosition);
            }
        }

        private static bool TryGetEnemyImageOverride(EnemyDataSO enemyData, out Vector2 position, out Vector2 size)
        {
            position = Vector2.zero;
            size = Vector2.zero;

            if (enemyData == null)
                return false;

            foreach (EnemyImageOverride entry in EnemyImageOverrides)
            {
                if (MatchesEnemy(enemyData, entry.Keyword))
                {
                    position = entry.Position;
                    size = entry.Size;
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesEnemy(EnemyDataSO enemyData, string keyword)
        {
            return ContainsKeyword(enemyData.EnemyId, keyword)
                || ContainsKeyword(enemyData.EnemyName, keyword)
                || ContainsKeyword(enemyData.IdleSprite != null ? enemyData.IdleSprite.name : null, keyword)
                || ContainsKeyword(enemyData.AttackSprite != null ? enemyData.AttackSprite.name : null, keyword);
        }

        private static bool ContainsKeyword(string value, string keyword)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(keyword))
                return false;

            return value.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private readonly struct EnemyImageOverride
        {
            public readonly string Keyword;
            public readonly Vector2 Position;
            public readonly Vector2 Size;

            public EnemyImageOverride(string keyword, Vector2 position, Vector2 size)
            {
                Keyword = keyword;
                Position = position;
                Size = size;
            }
        }

        private void CacheBaseImageSizes()
        {
            if (_hasCachedBaseSize)
                return;

            _idleBaseSize = GetImageSize(_idleImage);
            _attackBaseSize = GetImageSize(_attackImage);
            _idleBasePosition = GetImagePosition(_idleImage);
            _attackBasePosition = GetImagePosition(_attackImage);
            _hasCachedBaseSize = true;
        }

        private static Vector2 GetImageSize(Image image)
        {
            if (image != null && image.rectTransform != null)
                return image.rectTransform.sizeDelta;

            return Vector2.zero;
        }

        private static void ApplyImageSize(Image image, Vector2 size)
        {
            if (image == null || image.rectTransform == null || size == Vector2.zero)
                return;

            image.rectTransform.sizeDelta = size;
        }

        private static Vector2 GetImagePosition(Image image)
        {
            if (image != null && image.rectTransform != null)
                return image.rectTransform.anchoredPosition;

            return Vector2.zero;
        }

        private static void ApplyImagePosition(Image image, Vector2 position)
        {
            if (image == null || image.rectTransform == null)
                return;

            image.rectTransform.anchoredPosition = position;
        }
    }
}
