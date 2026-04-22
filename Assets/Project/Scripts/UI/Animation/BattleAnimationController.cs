using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FFF.Core.Events;

namespace FFF.UI.Animation
{
    /// <summary>
    /// 전투 씬의 애니메이션 연출을 총괄하는 오케스트레이터.
    /// 
    /// ── 핵심 책임 ──
    /// SO Event를 구독하여, 게임 로직이 "드로우 완료", "공격 발생" 등의 신호를 보내면
    /// 그에 맞는 시각 연출을 수행한다.
    /// 
    /// 게임 로직(DeckSystem, BattleManager 등)은 이 컴포넌트가 존재하는지조차 모른다.
    /// 이벤트를 쏘기만 하면, 여기서 알아서 연출한다 (알빠노).
    /// 
    /// ── 데모v3 대응 ──
    /// - 화면 흔들림 (Screen Shake): 공격/피격 시
    /// - 데미지 숫자 팝업: 적/플레이어 위에 숫자가 떠오름
    /// - 캐릭터 돌진 (Attack Lunge): 공격 시 앞으로 돌진
    /// - 캐릭터 피격 흔들림: 맞을 때 좌우 흔들림
    /// - 씬 전환 페이드: 검은 화면 인/아웃
    /// 
    /// ── 설계 원칙 ──
    /// - 기존 코드를 수정하지 않는다. SO Event 구독만으로 동작.
    /// - View 영역에만 존재. Controller/Model을 참조하지 않는다.
    /// - BattleScene에 하나만 배치. 씬 로컬 (싱글턴 아님).
    /// </summary>
    public class BattleAnimationController : MonoBehaviour
    {
        #region === Inspector: 연출 대상 참조 ===

        [Header("=== 캐릭터 참조 ===")]
        [Tooltip("플레이어 캐릭터의 RectTransform (돌진/피격 연출 대상)")]
        [SerializeField] private RectTransform _playerCharacter;

        [Tooltip("적 캐릭터의 RectTransform (돌진/피격 연출 대상)")]
        [SerializeField] private RectTransform _enemyCharacter;

        [Header("=== 화면 흔들림 ===")]
        [Tooltip("흔들릴 최상위 Canvas 또는 전투 영역 RectTransform")]
        [SerializeField] private RectTransform _shakeTarget;

        [Tooltip("흔들림 강도 (픽셀)")]
        [SerializeField] private float _shakeMagnitude = 5f;

        [Tooltip("흔들림 지속 시간")]
        [SerializeField] private float _shakeDuration = 0.35f;

        [Header("=== 데미지 숫자 ===")]
        [Tooltip("데미지 숫자 프리팹 (TextMeshProUGUI 포함)")]
        [SerializeField] private GameObject _damageNumberPrefab;

        [Tooltip("데미지 숫자가 생성될 Canvas")]
        [SerializeField] private Transform _damageNumberParent;

        [Header("=== 씬 전환 페이드 ===")]
        [Tooltip("화면 전체를 덮는 검은 패널의 CanvasGroup")]
        [SerializeField] private CanvasGroup _fadeOverlay;

        [Tooltip("페이드 소요 시간")]
        [SerializeField] private float _fadeDuration = 0.5f;

        [Header("=== 캐릭터 연출 설정 ===")]
        [Tooltip("공격 돌진 거리 (px)")]
        [SerializeField] private float _lungeDistance = 40f;

        [Tooltip("돌진 소요 시간")]
        [SerializeField] private float _lungeDuration = 0.3f;

        [Tooltip("피격 흔들림 강도")]
        [SerializeField] private float _hitShakeMagnitude = 15f;

        [Tooltip("피격 흔들림 시간")]
        [SerializeField] private float _hitShakeDuration = 0.4f;

        #endregion

        #region === Inspector: SO Event 구독 ===

        [Header("=== 구독할 SO Event Channels ===")]
        [Tooltip("공격 발생 이벤트 (데미지 연출 트리거)")]
        [SerializeField] private IntEvent _onPlayerAttack;

        [Tooltip("적 공격 발생 이벤트")]
        [SerializeField] private IntEvent _onEnemyAttack;

        [Tooltip("전투 종료 이벤트 (페이드 아웃 트리거)")]
        [SerializeField] private GameEvent _onBattleEnd;

        #endregion

        #region === Unity 생명주기 ===

        private void OnEnable()
        {
            if (_onPlayerAttack != null) _onPlayerAttack.Subscribe(HandlePlayerAttack);
            if (_onEnemyAttack != null) _onEnemyAttack.Subscribe(HandleEnemyAttack);
            if (_onBattleEnd != null) _onBattleEnd.Subscribe(HandleBattleEnd);
        }

        private void OnDisable()
        {
            if (_onPlayerAttack != null) _onPlayerAttack.Unsubscribe(HandlePlayerAttack);
            if (_onEnemyAttack != null) _onEnemyAttack.Unsubscribe(HandleEnemyAttack);
            if (_onBattleEnd != null) _onBattleEnd.Unsubscribe(HandleBattleEnd);
        }

        private void Start()
        {
            // 페이드 오버레이 초기화 (투명)
            if (_fadeOverlay != null)
            {
                _fadeOverlay.alpha = 0f;
                _fadeOverlay.blocksRaycasts = false;
            }
        }

        #endregion

        #region === 이벤트 핸들러 ===

        /// <summary>
        /// 플레이어 공격 시: 돌진 → 화면 흔들림 → 적 피격 → 데미지 숫자.
        /// </summary>
        private void HandlePlayerAttack(int damage)
        {
            StartCoroutine(PlayerAttackSequence(damage));
        }

        /// <summary>
        /// 적 공격 시: 적 돌진 → 화면 흔들림 → 플레이어 피격 → 데미지 숫자.
        /// </summary>
        private void HandleEnemyAttack(int damage)
        {
            StartCoroutine(EnemyAttackSequence(damage));
        }

        /// <summary>
        /// 전투 종료 시: 페이드 아웃.
        /// </summary>
        private void HandleBattleEnd()
        {
            // 페이드는 결과창 띄운 뒤 씬 전환 직전에 사용.
            // 여기서는 준비만 해둔다. 필요 시 PlayFadeOut() 직접 호출.
        }

        #endregion

        #region === 공격 시퀀스 ===

        /// <summary>
        /// 데모v3의 플레이어 공격 연출 전체 시퀀스.
        /// 플레이어 돌진 → (딜레이) → 화면 흔들림 + 적 피격 + 데미지 숫자
        /// </summary>
        private IEnumerator PlayerAttackSequence(int damage)
        {
            // 1. 플레이어 돌진 (오른쪽으로)
            if (_playerCharacter != null)
            {
                yield return PlayLunge(_playerCharacter, Vector2.right * _lungeDistance);
            }

            // 2. 화면 흔들림
            if (_shakeTarget != null)
            {
                StartCoroutine(UITweenHelper.ShakeRect(_shakeTarget, _shakeDuration, _shakeMagnitude));
            }

            // 3. 적 피격 흔들림
            if (_enemyCharacter != null)
            {
                StartCoroutine(UITweenHelper.ShakeRect(_enemyCharacter, _hitShakeDuration, _hitShakeMagnitude));
            }

            // 4. 데미지 숫자 팝업
            if (_enemyCharacter != null)
            {
                SpawnDamageNumber(_enemyCharacter, damage);
            }

            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// 데모v3의 적 공격 연출 시퀀스.
        /// </summary>
        private IEnumerator EnemyAttackSequence(int damage)
        {
            // 1. 적 돌진 (왼쪽으로)
            if (_enemyCharacter != null)
            {
                yield return PlayLunge(_enemyCharacter, Vector2.left * _lungeDistance);
            }

            // 2. 화면 흔들림
            if (_shakeTarget != null)
            {
                StartCoroutine(UITweenHelper.ShakeRect(_shakeTarget, _shakeDuration, _shakeMagnitude));
            }

            // 3. 플레이어 피격 흔들림
            if (_playerCharacter != null)
            {
                StartCoroutine(UITweenHelper.ShakeRect(_playerCharacter, _hitShakeDuration, _hitShakeMagnitude));
            }

            // 4. 데미지 숫자 팝업
            if (_playerCharacter != null)
            {
                SpawnDamageNumber(_playerCharacter, damage);
            }

            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// 캐릭터 돌진 연출: 앞으로 이동 → 원위치 복귀.
        /// 데모v3의 attackLunge 애니메이션 재현.
        /// </summary>
        private IEnumerator PlayLunge(RectTransform character, Vector2 direction)
        {
            Vector2 originalPos = character.anchoredPosition;
            Vector2 lungeTarget = originalPos + direction;

            // 앞으로 돌진
            yield return UITweenHelper.MoveTo(character, lungeTarget,
                _lungeDuration * 0.4f, UITweenHelper.EaseType.OutQuad);

            // 잠시 유지
            yield return new WaitForSeconds(_lungeDuration * 0.1f);

            // 원위치 복귀
            yield return UITweenHelper.MoveTo(character, originalPos,
                _lungeDuration * 0.5f, UITweenHelper.EaseType.OutCubic);
        }

        #endregion

        #region === 데미지 숫자 ===

        /// <summary>
        /// 대상 캐릭터 위에 데미지 숫자를 생성한다.
        /// 데모v3: 빨간 큰 숫자가 위로 떠오르며 사라짐.
        /// </summary>
        private void SpawnDamageNumber(RectTransform target, int damage)
        {
            if (_damageNumberPrefab == null || _damageNumberParent == null) return;

            GameObject dmgObj = Instantiate(_damageNumberPrefab, _damageNumberParent);
            RectTransform dmgRect = dmgObj.GetComponent<RectTransform>();

            // 대상 위치에 약간의 랜덤 오프셋
            dmgRect.position = target.position + new Vector3(
                Random.Range(-10f, 10f), Random.Range(0f, 20f), 0f);

            // 텍스트 설정
            TextMeshProUGUI dmgText = dmgObj.GetComponent<TextMeshProUGUI>();
            if (dmgText != null)
            {
                dmgText.text = damage.ToString();
            }

            // 떠오르며 사라지는 연출
            StartCoroutine(DamageNumberSequence(dmgObj));
        }

        /// <summary>
        /// 데미지 숫자 애니메이션: 커졌다 작아지며 위로 떠오름 → 페이드아웃 → Destroy.
        /// </summary>
        private IEnumerator DamageNumberSequence(GameObject dmgObj)
        {
            RectTransform rect = dmgObj.GetComponent<RectTransform>();
            CanvasGroup cg = dmgObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = dmgObj.AddComponent<CanvasGroup>();

            Vector2 startPos = rect.anchoredPosition;
            float duration = 1.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 위로 떠오름
                rect.anchoredPosition = startPos + Vector2.up * (70f * t);

                // 스케일: 처음에 커졌다가 정상 크기로
                float scale = t < 0.15f
                    ? Mathf.Lerp(0.3f, 1.4f, t / 0.15f)
                    : Mathf.Lerp(1.4f, 1f, Mathf.Clamp01((t - 0.15f) / 0.15f));
                rect.localScale = Vector3.one * scale;

                // 페이드: 70% 이후 사라지기 시작
                cg.alpha = t < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);

                yield return null;
            }

            Destroy(dmgObj);
        }

        #endregion

        #region === 씬 전환 페이드 (외부 호출용) ===

        /// <summary>
        /// 화면을 검은색으로 페이드 아웃한다.
        /// 데모v3: 적 처치 후 씬 전환 연출.
        /// GameManager 등에서 씬 전환 전에 호출.
        /// </summary>
        public Coroutine PlayFadeOut(System.Action onComplete = null)
        {
            if (_fadeOverlay == null) return null;
            _fadeOverlay.blocksRaycasts = true;
            return StartCoroutine(UITweenHelper.FadeTo(_fadeOverlay, 1f, _fadeDuration,
                UITweenHelper.EaseType.Linear, onComplete));
        }

        /// <summary>
        /// 화면을 검은색에서 투명으로 페이드 인한다.
        /// 씬 로드 직후 호출.
        /// </summary>
        public Coroutine PlayFadeIn(System.Action onComplete = null)
        {
            if (_fadeOverlay == null) return null;
            _fadeOverlay.alpha = 1f;
            return StartCoroutine(UITweenHelper.FadeTo(_fadeOverlay, 0f, _fadeDuration,
                UITweenHelper.EaseType.Linear, () =>
                {
                    _fadeOverlay.blocksRaycasts = false;
                    onComplete?.Invoke();
                }));
        }

        #endregion
    }
}
