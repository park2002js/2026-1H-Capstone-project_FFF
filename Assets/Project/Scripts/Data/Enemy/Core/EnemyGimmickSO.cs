using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 적 고유의 기믹을 BattleModifier 리스트로 생성하여 반환합니다.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.Preserve]
    public abstract class EnemyGimmickSO
    {
        public abstract List<BattleModifier> CreateGimmickModifiers(string enemyId);
    }
}