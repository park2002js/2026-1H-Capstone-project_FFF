using System.Collections.Generic;
using UnityEngine;
using FFF.Battle.Enemy;
using FFF.Battle.Modifier;

// 모든 Enemy AI 들이 아래의 함수를 구체화해야함
// SO.asset 파일에는 저 함수를 구체화한 .cs 파일에 연결될 것임
namespace FFF.Data
{
    public abstract class EnemyAISO : ScriptableObject 
    {
        /// <summary>
        /// AI 두뇌 역할: 상황(Context)과 Enemy 상태(self)를 보고 2장의 카드를 선정 후 반환
        /// </summary>
        public abstract List<HwaTuCard> DecideCards(EnemyDataBattle self, ModifierContext context);
    }
}