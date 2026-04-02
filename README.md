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
├── _Project/
│   ├── Scripts/
│   │   ├── Core/                    # 프로젝트 전역 시스템
│   │   │   ├── Events/              # ScriptableObject 이벤트 채널
│   │   │   │   ├── GameEvent.cs
│   │   │   │   ├── GameEventGeneric.cs   # 제네릭 이벤트 채널 (데이터 전달용)
│   │   │   │   ├── StringEvent.cs        # string 데이터 이벤트
│   │   │   │   ├── IntEvent.cs           # int 데이터 이벤트
│   │   │   │   └── GameEventListener.cs
│   │   │   ├── Managers/            # 싱글턴 매니저들
│   │   │   │   └── GameManager.cs
│   │   │   ├── Singleton.cs         # 제네릭 싱글턴 베이스
│   │   │   ├── SceneLoader.cs       # Scene 전환 유틸리티
│   │   │   └── BootSceneSetup.cs    # BootScene 진입점
│   │   │
│   │   ├── UI/                      # UI 시스템 (View 영역)
│   │   │   ├── Core/                # UI 핵심 구조
│   │   │   │   ├── UIManager.cs
│   │   │   │   └── BaseUIComponent.cs
│   │   │   ├── Title/               # 타이틀 화면 (백로그 10번)
│   │   │   │   ├── TitleUIComponent.cs
│   │   │   │   └── TitleSceneSetup.cs
│   │   │   ├── Main/                # 메인 화면 (백로그 10번, 1번)
│   │   │   │   ├── MainUIComponent.cs
│   │   │   │   └── MainSceneSetup.cs
│   │   │   ├── Map/                 # 맵 화면 (백로그 1번, 2번)
│   │   │   ├── Battle/              # 전투 화면 (백로그 3~8번)
│   │   │   ├── Shop/                # 상점 화면 (백로그 9번)
│   │   │   └── Common/              # 공용 UI 컴포넌트 (팝업, 전환 등)
│   │   │
│   │   ├── Battle/                  # 전투 시스템 (Controller 영역)
│   │   │   ├── FSM/                 # 전투 상태머신
│   │   │   ├── Card/                # 카드 드로우, 족보 판정
│   │   │   │   └── DeckSystem.cs    # 덱/손패/버려진 산 관리
│   │   │   ├── Damage/              # 데미지 계산, 승패 판정
│   │   │   └── Enemy/               # 적 AI
│   │   │
│   │   ├── Map/                     # 맵 시스템 (Controller 영역)
│   │   │
│   │   ├── Shop/                    # 상점 시스템 (Controller 영역)
│   │   │
│   │   ├── Player/                  # 플레이어 정보 관리 Manager
│   │   │
│   │   └── Data/                    # 데이터 모델 (Model 영역)
│   │       ├── HwaTuCard.cs         # 화투 카드 데이터 구조
│   │       ├── HwaTuCardDatabase.cs # 화투 20장 전체 데이터 생성
│   │       ├── SeotdaHand.cs        # 섯다 족보 enum + 공격력 테이블
│   │       ├── SeotdaJudge.cs       # 족보 판정 시스템
│   │       ├── ScriptableObjects/   # S.O 정의 클래스
│   │       └── Json/                # Json 직렬화 클래스
│   │
│   ├── ScriptableObjects/           # S.O 에셋 인스턴스
│   │   ├── Events/                  # 이벤트 채널 에셋
│   │   │   ├── OnMapInitialize.asset
│   │   │   ├── OnBattleInitialize.asset
│   │   │   ├── OnShopInitialize.asset
│   │   │   ├── OnSceneChange.asset
│   │   │   └── OnBattleResult.asset
│   │   ├── Cards/                   # 화투 카드 데이터
│   │   └── Enemies/                 # 적 데이터
│   │
│   ├── Prefabs/
│   │   ├── UI/                      # UI 프리팹
│   │   │   ├── Screens/             # 화면 단위 프리팹
│   │   │   └── Components/          # 재사용 UI 컴포넌트
│   │   ├── Cards/                   # 카드 프리팹
│   │   └── Effects/                 # 이펙트 프리팹
│   │
│   ├── Scenes/
│   │   ├── BootScene.unity          # 초기 로딩 (매니저 초기화)
│   │   ├── TitleScene.unity         # 타이틀
│   │   ├── MainScene.unity          # 메인 메뉴
│   │   ├── MapScene.unity           # 맵
│   │   ├── BattleScene.unity        # 전투
│   │   └── ShopScene.unity          # 상점
│   │
│   ├── Art/
│   │   ├── UI/                      # UI 스프라이트, 아이콘
│   │   ├── Cards/                   # 화투 카드 이미지
│   │   ├── Characters/              # 캐릭터 이미지
│   │   └── Backgrounds/             # 배경 이미지
│   │
│   ├── Audio/
│   │   ├── BGM/
│   │   └── SFX/
│   │
│   └── Animations/
│       ├── UI/                      # UI 애니메이션
│       └── Cards/                   # 카드 애니메이션
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
