using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 전투의 현재 진행 상태를 나타내는 열거형
public enum TurnState
{
    None,
    TurnReady,  // 턴 준비 (드로우 및 멀리건)
    TurnProceed,// 턴 진행 (카드 선택, 아이템 사용)
    TurnEnd     // 턴 종료 (공격력 비교, 피해량 적용)
}

public class BattleManager : MonoBehaviour
{
    // --- 싱글톤 인스턴스 ---
    public static BattleManager Instance { get; private set; }

    // --- 주요 변수 (상태 및 데이터) ---
    private TurnState currentPhase;
    
    // 플레이어 및 적 데이터 (체력 등)
    private int playerHP;
    private int enemyHP;

    // 턴 진행 중 선택된 카드들을 저장할 리스트 (최대 2장)
    private List<object> selectedCards = new List<object>(); // object는 임시 타입 (Card 클래스로 변경 필요)
    
    // 계산된 공격력
    private int expectedPlayerPower; 
    private int expectedEnemyPower;

    // 외부 매니저 참조 변수 (생성 시점에 가져올 예정)
    // private GameManager gameManager;
    // private UIManager uiManager; 

    // ==========================================
    // 1. 초기화 및 세팅
    // ==========================================

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
            InitializeManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeManager()
    {
        // 필요한 외부 매니저들을 구독하거나 인스턴스를 가져와 저장
        // gameManager = GameManager.Instance;
        // uiManager = UIManager.Instance;
    }

    // ==========================================
    // 2. 전투 시작 (UI 또는 외부 로직에서 호출)
    // ==========================================

    public void StartBattle()
    {
        // 전투 초기 세팅 (변수 및 UI 초기화)
        playerHP = 100; // 임시 값
        enemyHP = 100;  // 임시 값
        currentPhase = TurnState.None;

        // 초기화 완료 후 턴 준비 단계로 진입
        SetupTurn();
    }

    // ==========================================
    // 3. 턴 준비 단계 (Turn Preparation)
    // ==========================================

    private void SetupTurn()
    {
        currentPhase = TurnState.TurnReady;
        selectedCards.Clear();
        expectedPlayerPower = 0;

        // 1. 카드 더미에서 카드 뽑기 로직 호출
        // 2. UI에 뽑힌 카드 출력 및 멀리건(리롤) UI 활성화
    }

    // [외부 호출] 플레이어가 멀리건을 끝내고 최종 손패를 확정했을 때 UI에서 호출
    public void FinalHandAndStartTurn()
    {
        if (currentPhase != TurnState.TurnReady) return;
        
        // 턴 진행 단계로 넘어감
        ProceedTurn();
    }

    // ==========================================
    // 4. 턴 진행 단계 (Turn Execution)
    // ==========================================

    private void ProceedTurn()
    {
        currentPhase = TurnState.TurnProceed;
        // 턴 종료 버튼 비활성화 UI 지시 (카드 2장이 안 골라졌으므로)
    }

    // [외부 호출] 플레이어가 패에서 카드를 선택/해제 했을 때 호출
    public void OnCardSelected(object selectedCard) 
    {
        if (currentPhase != TurnState.TurnProceed) return;

        // 선택 리스트에 추가 및 제거 로직
        // ...

        // 2장이 완성되었는지 체크
        if (selectedCards.Count == 2)
        {
            CalculateAndPreviewCombo(); // 공격력 계산 및 UI 미리보기 갱신
            // UI 측에 '턴 종료 버튼' 활성화 지시
        }
        else
        {
            // UI 측에 '턴 종료 버튼' 비활성화 지시
        }
    }

    // [외부 호출] 플레이어가 조커/액티브 아이템을 사용했을 때 호출
    public void UseActiveItem(object item)
    {
        if (currentPhase != TurnState.TurnProceed) return;
        
        // 아이템 효과 발동 로직 (공격력 버프, 체력 회복 등)
    }

    private void CalculateAndPreviewCombo()
    {
        // 선택된 2장의 화투패 조합 검사
        // 여러 요소(버프, 시너지 등)를 반영하여 expectedPlayerPower 계산
        // UI에 예상 공격력 및 조합 이름 표시 지시
    }

    // ==========================================
    // 5. 턴 종료/결산 단계 (Turn Resolution)
    // ==========================================

    // [외부 호출] 플레이어가 '턴 종료' 버튼을 눌렀을 때 UI에서 호출
    public void OnTurnEndButtonClicked()
    {
        if (currentPhase != TurnState.TurnProceed || selectedCards.Count != 2) return;

        EndTurn();
    }

    private void EndTurn()
    {
        currentPhase = TurnState.TurnEnd;

        // 1. 적의 행동 패턴 결정 및 공격력 계산
        expectedEnemyPower = DetermineEnemyAction();

        // 2. 공격력 비교
        if (expectedPlayerPower > expectedEnemyPower)
        {
            // 플레이어 승리 로직 (피해량 계산)
            int damage = CalculateDamage(expectedPlayerPower); // 부가 효과 포함 계산
            enemyHP -= damage;
        }
        else if (expectedEnemyPower > expectedPlayerPower)
        {
            // 적 승리 로직
            int damage = CalculateDamage(expectedEnemyPower);
            playerHP -= damage;
        }
        else
        {
            // 무승부 처리 로직 (필요시)
        }

        // 3. 체력 갱신 UI 지시 및 사망 여부 판단
        CheckWhoWin();
    }

    private int DetermineEnemyAction()
    {
        // 적의 패턴/AI에 따라 낼 카드와 공격력을 결정하여 반환
        return 0; // 임시 반환
    }

    private int CalculateDamage(int winningPower)
    {
        // 부가적인 효과(아이템, 조합 특수기 등)를 더하여 최종 피해량 반환
        return winningPower; 
    }

    // ==========================================
    // 6. 전투 종료 단계 (Combat End)
    // ==========================================

    private void CheckWhoWin()
    {
        if (playerHP <= 0)
        {
            // 플레이어 패배
            EndBattle(isPlayerDead: true);
        }
        else if (enemyHP <= 0)
        {
            // 적 패배 (플레이어 승리)
            EndBattle(isPlayerDead: false);
        }
        else
        {
            // 아무도 죽지 않았다면 다시 턴 준비 단계로
            SetupTurn();
        }
    }

    private void EndBattle(bool isPlayerDead)
    {
        if (isPlayerDead)
        {
            // GameManager에게 플레이어 전투 패배 로직 수행 지시
            // GameManager.Instance.OnPlayerDefeated();
        }
        else
        {
            // GameManager에게 전투 보상 UI 띄우는 로직 수행 지시
            // GameManager.Instance.ShowCombatRewardUI();
        }
    }
}