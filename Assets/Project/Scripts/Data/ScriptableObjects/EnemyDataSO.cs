using UnityEngine;

namespace FFF.Data
{
    /// <summary>
    /// 이 Scritable Object 템플릿을 이용해서 적 정보를 Assets/Project/ScriptableObjects/Enemy 아래에 작성하면
    /// 게임이 시작되면서 EnemyDatabase에 자동으로 저장됨
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "FFF/Data/Enemy Data")]
    public class EnemyDataSO : ScriptableObject
    {
        [Header("=== 적 기본 정보 ===")]
        public string EnemyId;
        public string EnemyName;
        public int MaxHealth;

        // 추후 이 곳에 적의 AI 패턴, 기믹 데이터(List<EnemyIntent> 등)가 추가할 예정
    }
}