using System.Collections.Generic;
using UnityEngine;

namespace FFF.Data
{
    /// <summary>
    /// Resources 폴더에 저장된 EnemyDataSO를 ID 기반으로 불러오는 유틸리티
    /// </summary>
    public static class EnemyDatabase
    {
        private const string ENEMIES_RESOURCE_PATH = "Enemies"; // Resources/Enemies 폴더

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