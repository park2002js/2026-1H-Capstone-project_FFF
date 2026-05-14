using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Enemy;
using FFF.Battle.Data;
using FFF.Battle.Modifier;

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

        public EnemyAISO AILogic;

        [Header("=== AI 로직 설정 ===")]
        [Tooltip("인스펙터에서 이 적의 AI 패턴을 메모해두는 용도")]
        public string AIPatternDescription = "체력 50% 이하 시 분기형 AI";
    }
}