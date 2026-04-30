using UnityEngine;
using FFF.Battle.Data;

namespace FFF.Data
{
    /// <summary>
    /// Stage(전투)가 끝난 후, 로컬에서 변경된 데이터(PlayerDataBattle)를 
    /// Master 원본(PlayerDataSO)에 동기화(Commit)하는 1회성 유틸리티 클래스입니다.
    /// </summary>
    public class PlayerDataUpdater
    {
        public void SyncBattleDataToMaster(PlayerDataBattle battleData, PlayerDataSO masterData)
        {
            if (battleData == null || masterData == null)
            {
                Debug.LogError("[PlayerDataUpdater] 데이터가 null이라 동기화할 수 없습니다.");
                return;
            }

            // 1. 체력 갱신 (전투 중 맞아서 깎인 체력을 원본에 덮어씌움)
            masterData.CurrentHealth = battleData.CurrentHealth;

            // 2. 조커 갱신 (전투 중 소비되어 리스트에서 빠진 내역을 덮어씌움)
            masterData.HeldJokerIds.Clear();
            masterData.HeldJokerIds.AddRange(battleData.HeldJokerIds);

            // 3. 장신구 갱신 (전투 중 파괴되거나 변경되는 기믹을 대비)
            masterData.EquippedAccessoryIds.Clear();
            masterData.EquippedAccessoryIds.AddRange(battleData.EquippedAccessoryIds);

            Debug.Log($"[PlayerDataUpdater] 전투 결과를 Master Data에 갱신 완료. (남은 체력: {masterData.CurrentHealth})");
        }
    }
}