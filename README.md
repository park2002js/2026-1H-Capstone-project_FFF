# 🎮 2026 Capstone Design Project: FFF


## ✨ 주요 특징

  * **1인 싱글플레이**: 깊이 있는 몰입감을 선사하는 단독 플레이 경험.
  * **카드 덱 빌딩**: 전략적인 카드 조합과 선택을 통한 자신만의 플레이 스타일 구축.
  * **로그라이크**: 매번 변화하는 환경과 긴장감 넘치는 영구적 죽음 요소.


## 🛠 개발 환경

  * **Engine**: `Unity Engine 6000.0.71f1`
  * **IDE**: `Visual Studio 2022`


## 🚀 시작 가이드

### 1\. 저장소 클론 받기

먼저 프로젝트를 로컬 환경으로 복제합니다.

```bash
git clone https://github.com/park2002js/2026-1H-Capstone-project_FFF.git
```

### 2\. Unity Hub에 프로젝트 추가하기

1.  **Unity Hub**를 실행합니다.
2.  `Add` 버튼(또는 `Add project from disk`)을 클릭합니다.
3.  위에서 클론 받은 프로젝트 폴더를 선택하여 추가합니다.

### 3\. 프로젝트 열기

  * Unity Hub 목록에서 프로젝트를 선택합니다.
  * **주의**: 반드시 명시된 버전인 **6000.0.71f1**으로 실행해야 라이브러리 충돌을 방지할 수 있습니다.


## 🤝 팀 규칙
*
*



## 👥 팀원 소개

| 이름 | GitHub ID |
| :--- | :--- |
| **김찬영** | [@a5153203]
| **박지성** | [@park2002js]
| **정혁진** | [@Vqlntinx]

-----

# FFF (Forge's Flower Fight) - Unity 프로젝트 폴더 구조

## Assets/
```
Assets/
├── Project/
│   ├── Scripts/
│   │   ├── Core/                    # 프로젝트 전역 시스템
│   │   │   ├── Events/              # ScriptableObject 이벤트 채널
│   │   │   │   ├── GameEvent.cs
│   │   │   │   └── GameEventGeneric.cs   # 제네릭 이벤트 채널 (데이터 전달용)
│   │   │   ├── Managers/            # 싱글턴 매니저들
│   │   │   │   └── GameManager.cs
│   │   │   ├── Singleton.cs         # 제네릭 싱글턴 베이스
│   │   │   ├── SceneLoader.cs       # Scene 전환 유틸리티
│   │   │   └── BootSceneSetup.cs    # BootScene 진입점
│   │   │
│   │   ├── UI/                      # UI 시스템 (View 영역)
│   │   │   ├── Animation/           # UI 및 애니메이션 연출 (신규 추가)
│   │   │   │   ├── BattleAnimationController.cs
│   │   │   │   ├── CardAnimator.cs
│   │   │   │   └── UITweenHelper.cs
│   │   │   ├── Core/                # UI 핵심 구조
│   │   │   │   ├── BaseUIComponent.cs
│   │   │   │   └── UIManager.cs
│   │   │   ├── Title/               # 타이틀 화면
│   │   │   │   ├── TitleSceneSetup.cs
│   │   │   │   └── TitleUIComponent.cs
│   │   │   ├── Main/                # 메인 화면
│   │   │   │   ├── MainSceneSetup.cs
│   │   │   │   └── MainUIComponent.cs
│   │   │   ├── Map/                 # 맵 화면
│   │   │   │   ├── MapEdgeView.cs
│   │   │   │   ├── MapNodeView.cs
│   │   │   │   ├── MapSceneSetup.cs
│   │   │   │   └── MapUIComponent.cs
│   │   │   └── Battle/              # 전투 화면
│   │   │       ├── BattleSceneSetup.cs
│   │   │       ├── BattleUIComponent.cs
│   │   │       ├── CardUIComponent.cs
│   │   │       └── StatusBarUI.cs
│   │   │
│   │   ├── Battle/                  # 전투 시스템 (Controller 영역)
│   │   │   ├── FSM/                 # 전투 상태머신 (턴 흐름 매니저들)
│   │   │   │   ├── BattleManager.cs
│   │   │   │   ├── BattleStartManager.cs
│   │   │   │   ├── TurnReadyManager.cs
│   │   │   │   ├── TurnProceedManager.cs
│   │   │   │   ├── TurnEndManager.cs
│   │   │   │   └── BattleEndManager.cs
│   │   │   ├── Damage/              # 데미지/공격력 계산 로직
│   │   │   │   ├── CombatCalculator.cs
│   │   │   │   ├── DamageCal.cs
│   │   │   │   └── StrengthCal.cs
│   │   │   ├── Enemy/               # 적 시스템
│   │   │   │   └── EnemyData.cs
│   │   │   ├── Item/                # 아이템 (조커/장신구) 시스템
│   │   │   │   └── Joker/
│   │   │   │       └── JokerManager.cs
│   │   │   ├── Card/                # 카드 관리 (DeckSystem 등)
│   │   │   └── Modifier/            # 턴 버프/디버프 관리
│   │   │
│   │   ├── Map/                     # 맵 로직 (Controller 영역)
│   │   │   ├── MapData.cs
│   │   │   ├── MapGenerator.cs
│   │   │   ├── MapNode.cs
│   │   │   └── RoomType.cs
│   │   │
│   │   ├── Data/                    # 데이터 모델 (Model 영역)
│   │   │   ├── ScriptableObjects/   # SO 정의 클래스
│   │   │   │   └── HwaTuCardSO.cs
│   │   │   ├── BattleContext.cs     # 단일 전투 세션 상태 보관소
│   │   │   ├── HwaTuCard.cs         # 화투 카드 데이터 구조
│   │   │   ├── HwaTuCardDatabase.cs # SO 에셋 로더
│   │   │   ├── PlayerData.cs        # 플레이어 전역 상태 보관소
│   │   │   ├── SeotdaHand.cs        # 섯다 족보 enum + 공격력 테이블
│   │   │   └── SeotdaJudge.cs       # 족보 판정 시스템
│   │   │
│   │   └── Test/                    # 테스트 자동화
│   │       ├── BattleTestStarter.cs
│   │       ├── CardSOTest.cs
│   │       ├── CardSystemTest.cs
│   │       └── ItemSystemTest.cs
│   │
│   ├── ScriptableObjects/           # 실제 S.O 에셋 인스턴스
│   ├── Prefabs/                     # UI, 카드 프리팹
│   ├── Scenes/                      # Boot, Title, Main, Map, Battle
│   ├── Art/                         # 스프라이트, UI 리소스
│   └── Animations/                  # 애니메이션 파일
```

## 폴더 구조 설계 근거

### MVC(MVP) 패턴 매핑
- **Model**: `Scripts/Data/` + `ScriptableObjects/`
- **View**: `Scripts/UI/`
- **Controller(Presenter)**: `Scripts/Battle/`, `Scripts/Map/`, `Scripts/Shop/`, `Scripts/Core/Managers/`

### Scene 구성 근거
- `BootScene`: 싱글턴 매니저들(GameManager, UIManager 등) 초기화 전용
- 나머지 Scene: 백로그의 화면 전환 흐름에 맞춰 분리
- 아키텍처 다이어그램의 "Scene 변경 UI 클릭 신호" 흐름 반영
