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
        [Header("=== UI 컴포넌트 참조 ===")]
        [Tooltip("평상시 외형을 출력할 UI Image 컴포넌트")]
        [SerializeField] private Image _idleImage;
        [Tooltip("공격 시 외형을 출력할 UI Image 컴포넌트")]
        [SerializeField] private Image _attackImage;
        [Tooltip("전투 배경을 출력할 UI Image 컴포넌트")]
        [SerializeField] private Image _backgroundImage;

        [Header("=== 애니메이션 연동 참조 ===")]
        [Tooltip("Idle/Attack 토글을 관리하는 로컬 컴포넌트 (CharacterAttackVisual.cs)")]
        [SerializeField] private CharacterAttackVisual _characterVisual;
        [Tooltip("돌진/피격 연출의 기준이 되는 RectTransform (Attack RectTransform)")]
        [SerializeField] private RectTransform _characterRect;
        [Tooltip("연출을 총괄하는 배틀 애니메이션 컨트롤러")]
        [SerializeField] private BattleAnimationController _animController;

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

            // 1. 데이터에 기반하여 UI 이미지 스프라이트 갱신
            if (_idleImage != null) _idleImage.sprite = enemyData.IdleSprite;
            if (_attackImage != null) _attackImage.sprite = enemyData.AttackSprite;
            if (_backgroundImage != null) _backgroundImage.sprite = enemyData.BackgroundSprite;

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
    }
}