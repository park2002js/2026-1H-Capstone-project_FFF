using System.Collections.Generic;
using UnityEngine;
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

        [Header("=== 적의 AI 및 Gimmick 설명 ===")]
        [Tooltip("인스펙터에서 이 적의 AI 패턴 및 기믹을 표기해야 전투시 해당 설명이 뜹니다")]
        public string AIPatternDescription = "체력 50% 이하 시 분기형 AI";

        [Header("=== 적 외형 및 배경 이미지 ===")]
        [Tooltip("평상시 외형(Idle) 이미지")]
        public Sprite IdleSprite;
        
        [Tooltip("공격 시 외형(Attack) 이미지")]
        public Sprite AttackSprite;
        
        [Tooltip("스테이지 전투 배경 이미지")]
        public Sprite BackgroundSprite;
    }
}