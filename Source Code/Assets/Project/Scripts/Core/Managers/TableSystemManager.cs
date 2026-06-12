using System.Collections.Generic;
using UnityEngine;
using FFF.Map; // RoomType 참조 목적

namespace FFF.Core
{
    /// <summary>
    /// 게임 내 적 ID 및 아이템 테이블을 관리하고 반환하는 시스템 매니저.
    /// Step 3 구현을 위해 뼈대만 우선 작성된 상태임.
    /// </summary>
    public class TableSystemManager
    {
        /// <summary> 테이블 셔플용 난수 생성기 </summary>
        private System.Random _rng;

        // === 적 ID 풀 ===
        /// <summary> 일반 적 ID 보관 리스트 </summary>
        private List<string> _normalEnemyList = new List<string>();
        /// <summary> 엘리트 적 ID 보관 리스트 </summary>
        private List<string> _eliteEnemyList = new List<string>();
        /// <summary> 보스 적 ID 보관 리스트 </summary>
        private List<string> _bossEnemyList = new List<string>();

        // === 아이템 ID 풀 ===
        /// <summary> 악세서리 ID 보관 리스트 </summary>
        private List<string> _accessoryIdList = new List<string>();

        /// <summary> 악세서리 원본 데이터 (리필 목적) </summary>
        private readonly string[] _defaultAccessoryIds = { 
            "Accessory_001", "Accessory_002", "Accessory_003", "Accessory_004", "Accessory_005",
            "Accessory_006", "Accessory_007", "Accessory_008", "Accessory_009", "Accessory_010",
            "Accessory_011", "Accessory_012", "Accessory_013", "Accessory_014", "Accessory_015",
            "Accessory_016", "Accessory_017", "Accessory_018", "Accessory_019", "Accessory_020"};

        /// <summary> 조커 ID 보관 리스트 </summary>
        private List<string> _jokerIdList = new List<string>();
        /// <summary> 조커 원본 데이터 (리필 목적) </summary>
        private readonly string[] _defaultJokerIds = { 
            "Joker_001", "Joker_002", "Joker_003", "Joker_004", "Joker_005",
            "Joker_006", "Joker_007", "Joker_008"
            };

        // === 원본 데이터 (리필 목적) ===
        // 후에 Json이나 Text로 저장된 리스트를 불러와서 초기화하도록 로직을 변경할 예정. 코드내에서 전부다 추가하는것이 오히려 불편
        private readonly string[] _defaultNormalEnemies = { 
            "Enemy_001", "Enemy_002", "Enemy_003", "Enemy_004", "Enemy_005",
            "Enemy_006", "Enemy_007", "Enemy_008", "Enemy_009", "Enemy_010", "Enemy_011" };
        private readonly string[] _defaultEliteEnemies = { "Enemy_012", "Enemy_013", "Enemy_014" };
        private readonly string[] _defaultBossEnemies = { "Enemy_015" };

        /// <summary>
        /// 전달된 시드값으로 난수 생성기 초기화 및 적 리스트 세팅 수행.
        /// GameManager의 맵 생성 및 복원 시점 호출 목적.
        /// </summary>
        public void Initialize(int seed)
        {
            _rng = new System.Random(seed);
            
            RefillAndShuffle(ref _normalEnemyList, _defaultNormalEnemies);
            RefillAndShuffle(ref _eliteEnemyList, _defaultEliteEnemies);
            RefillAndShuffle(ref _bossEnemyList, _defaultBossEnemies);

            RefillAndShuffle(ref _accessoryIdList, _defaultAccessoryIds);
            RefillAndShuffle(ref _jokerIdList, _defaultJokerIds);
            
            Debug.Log($"[TableSystemManager] 적 목록 초기화 및 셔플 완료. Seed: {seed}");
        }

        /// <summary>
        /// 룸 타입에 따른 적 ID 추출 및 반환.
        /// 내부 리스트 고갈 시 원본 데이터 삽입 및 재셔플 처리.
        /// </summary>
        public string PopEnemyId(RoomType roomType)
        {
            // RNG 미초기화 예외 방지용 방어 코드
            if (_rng == null)
            {
                Debug.LogWarning("[TableSystemManager] RNG 미초기화 상태. 폴백 시드 1 적용.");
                Initialize(1);
            }

            List<string> targetList;
            string[] defaultArray;

            switch (roomType)
            {
                case RoomType.Elite:
                    targetList = _eliteEnemyList;
                    defaultArray = _defaultEliteEnemies;
                    break;
                case RoomType.Boss:
                    targetList = _bossEnemyList;
                    defaultArray = _defaultBossEnemies;
                    break;
                default:
                    targetList = _normalEnemyList;
                    defaultArray = _defaultNormalEnemies;
                    break;
            }

            // 리스트 고갈 시 리필 및 셔플 수행
            if (targetList.Count == 0)
            {
                RefillAndShuffle(ref targetList, defaultArray);
                Debug.Log($"[TableSystemManager] {roomType} 등급 적 목록 고갈. 리필 및 셔플 적용.");
            }

            string selectedId = targetList[targetList.Count - 1];
            targetList.RemoveAt(targetList.Count - 1);
            
            return selectedId;
        }

        /// <summary>
        /// 지정된 타입의 아이템 ID 지정 수량 추출 및 반환.
        /// 내부 리스트 고갈 시 원본 데이터 삽입 및 재셔플 처리.
        /// </summary>
        public List<string> PopItemIds(FFF.Data.ItemType itemType, int count)
        {
            if (_rng == null)
            {
                Debug.LogWarning("[TableSystemManager] RNG 미초기화 상태. 폴백 시드 1 적용.");
                Initialize(1);
            }

            List<string> result = new List<string>();
            List<string> targetList = null;
            string[] defaultArray = null;

            if (itemType == FFF.Data.ItemType.Accessory)
            {
                targetList = _accessoryIdList;
                defaultArray = _defaultAccessoryIds;
            }
            else if (itemType == FFF.Data.ItemType.Joker)
            {
                targetList = _jokerIdList;
                defaultArray = _defaultJokerIds;
            }
            else
            {
                return result; // 조커 등 기타 타입은 추후 확장에 대비함
            }

            // 요구량이 남은 풀보다 크면 뽑기 시작 '전'에 리필 및 셔플 진행 (당장의 중복 노출 방지)
            if (targetList.Count < count)
            {
                RefillAndShuffle(ref targetList, defaultArray);
            }

            // 실제 뽑기 진행 (원본 배열 크기 이상을 요구하는 엣지 케이스 방어)
            int actualCount = Mathf.Min(count, targetList.Count);
            for (int i = 0; i < actualCount; i++)
            {
                result.Add(targetList[0]);
                targetList.RemoveAt(0);
            }
            
            return result;
        }

        /// <summary>
        /// 원본 배열 기반 리스트 초기화 및 Fisher-Yates 셔플 연산.
        /// </summary>
        private void RefillAndShuffle(ref List<string> targetList, string[] defaultArray)
        {
            targetList.Clear();
            targetList.AddRange(defaultArray);

            for (int i = targetList.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (targetList[i], targetList[j]) = (targetList[j], targetList[i]);
            }
        }
    }
}