using System.Collections.Generic;
using UnityEngine;

namespace FFF.Data
{
    /// <summary>
    /// 어느 경로에 존재하든, Resources 폴더에 저장된 EnemyDataSO를 ID 기반으로 불러오는 유틸리티
    /// Resources.LoadAll 함수는 모든 경로에서 Resources로 된 파일들을 하나로 합쳐서 읽어들임
    /// </summary>
    public static class EnemyDatabase
    {
        private const string ENEMIES_RESOURCE_PATH = "SO/Enemy"; // Resources/SO/Enemy 폴더

        public static List<EnemyDataSO> LoadAllEnemies()
        {
            var enemySOs = Resources.LoadAll<EnemyDataSO>(ENEMIES_RESOURCE_PATH);
            return new List<EnemyDataSO>(enemySOs);
        }

        public static EnemyDataSO FindById(string enemyId)
        {
            var allEnemies = LoadAllEnemies();
            return allEnemies.Find(e => e.EnemyId == enemyId);
        }
    }
}