using UnityEngine;
using FFF.Data;
using FFF.Battle.Data;
using FFF.Battle.Modifier;
using System.Collections.Generic;

namespace FFF.Battle.Enemy
{
    public struct EnemyIntent
    {
        public HwaTuCard Card1;
        public HwaTuCard Card2;
        public int BasePower;
        public string HandName;
    }

    /// <summary>
    /// 전투 씬에 배치되어 현재 상대하는 적의 상태(런타임 데이터)를 관리
    /// PlayerDataBattle처럼 마스터 데이터(SO)를 전달 받아서 초기화
    /// </summary>
    public class EnemyDataBattle : MonoBehaviour
    {
        public string EnemyId { get; private set; }
        public string EnemyName { get; private set; }
        public string AIPatternDescription { get; private set; }
        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }

        // SO 파일 내에 정의된 AI 함수 호출을 위한 원본 저장
        private EnemyAISO EnemyAILogic;
        
        // 이번 턴의 적 행동 의도
        public EnemyIntent CurrentIntent { get; private set; }

        /// <summary>
        /// 전투 시작 시 BattleStartManager가 SO 데이터를 넘기며 호출합니다.
        /// 인자로 넘어온 SO 데이터를 바탕으로 현재 전투의 적 정보를 초기화합니다.
        /// </summary>
        public void Initialize(EnemyDataSO enemyData, int bonusHealth = 0) // <ver 1.2.1> 임시 - 적의 추가 체력을 설정하는 로직을 만들지 않아서 여기에 임시로 추가함
        {
            // Enemy Data 누락으로 인해 게임 에러 방지를 위한 임시 데이터 
            if (enemyData == null)
            {
                Debug.LogWarning("[EnemyDataBattle] EnemyDataSO가 null입니다! 임시 데이터로 세팅합니다.");
                EnemyId = "Mock_01";
                EnemyName = "시연용 허수아비";
                AIPatternDescription = string.Empty;
                MaxHealth = 181;
                CurrentHealth = 181;
                RegisterTestGimmick(); // 임시 테스트용 기믹 등록
                return;
            }

            EnemyAILogic = enemyData.AILogic;
            EnemyId = enemyData.EnemyId;
            EnemyName = enemyData.EnemyName;
            AIPatternDescription = enemyData.AIPatternDescription;

            MaxHealth = enemyData.MaxHealth + bonusHealth;      // <ver 1.2.1> 임시 - 적의 추가 체력을 설정하는 로직을 만들지 않아서 여기에 임시로 추가함
            CurrentHealth = enemyData.MaxHealth + bonusHealth;  // <ver 1.2.1> 임시 - 적의 추가 체력을 설정하는 로직을 만들지 않아서 여기에 임시로 추가함
            
            if (enemyData.GimmickLogic != null)
            {
                var modifiers = enemyData.GimmickLogic.CreateGimmickModifiers(EnemyId);
                foreach (var mod in modifiers)
                {
                    ModifierManager.Instance.AddModifier(mod);
                }
                Debug.Log($"[EnemyDataBattle] {EnemyName} 기믹 등록 완료.");
            }
            else
            {
                RegisterTestGimmick();
                Debug.Log($"[EnemyDataBattle] {EnemyName} 기믹 파일이 누락되어 임시 기믹으로 대체.");
            }

            Debug.Log($"[EnemyDataBattle] 적 세팅 완료: {EnemyName} (HP: {CurrentHealth}/{MaxHealth})");
        }

        // TurnReady 시작 시 호출되어 로직에 따라 이번턴 행동을 결정 합니다.
        // BattleManager의 인스턴스로부터 BattleContext를 전달받아 SO파일의 AI로직에서 활용할 수 있도록 합니다.
        public void GenerateIntent(ModifierContext context)
        {

            // 현재 Enemy의 상태(this)와 Battle 상황(BattleContext)을 인자로 전달
            // SO에서는 내부 로직에 의해 결정된 2장의 카드를 반환
            List<HwaTuCard> pickedCards = EnemyAILogic.DecideCards(this, context);

            if (pickedCards == null || pickedCards.Count < 2)
            {
                List<HwaTuCard> allCards = HwaTuCardDatabase.CreateAllCards();
                pickedCards[0] = allCards[UnityEngine.Random.Range(0, 1)];
                pickedCards[1] = allCards[UnityEngine.Random.Range(0, 1)];
                Debug.Log("[EnemyDataBattle] 내부 Enemy AI 쪽 반환 문제 발생");
            }

            HwaTuCard c1 = pickedCards[0];
            HwaTuCard c2 = pickedCards[1];
            
            SeotdaResult result = SeotdaJudge.Judge(c1, c2);

            CurrentIntent = new EnemyIntent
            {
                Card1 = c1,
                Card2 = c2,
                BasePower = result.BasePower,
                HandName = result.DisplayName
            };

            Debug.Log($"[EnemyDataBattle] 적 의도 결정됨: {result.DisplayName} (공격력: {result.BasePower})");
        }
        

        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;
            CurrentHealth -= damage;
            if (CurrentHealth < 0) CurrentHealth = 0;
            
            Debug.Log($"[EnemyData] 적이 {damage}의 피해를 입었습니다! 남은 체력: {CurrentHealth}");
        }

        private void RegisterTestGimmick()
        {
            // TODO: Step 2에서 구체적인 Enemy 기믹 클래스로 분리될 영역
            // 현재는 시스템 검증을 위해 뼈대만 배치함
            if (ModifierManager.Instance != null)
            {
                Debug.Log($"[EnemyDataBattle] {EnemyName}의 기믹 로드 및 ModifierManager 등록 준비 완료.");
                // 예: ModifierManager.Instance.RegisterModifier(new BattleModifier(...));
            }
        }
    }
}
