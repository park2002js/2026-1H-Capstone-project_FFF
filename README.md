# 🎮 2026 Capstone Design Project: FFF

## 브랜치 구조 안내
Main-Dev-Feature 형식을 따라, Feature 단계에서 기능 개발 후 검토, 각 feature 완료 시 Dev 브랜치로 병합합니다.
<img width="264" height="150" alt="feature-branch" src="https://github.com/user-attachments/assets/14ff024a-2094-4b85-8336-d6c80d5bdf74" />


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
│   └── Scripts/
│       ├── Battle/                      # 전투 시스템 핵심 로직 (Controller 영역)
│       │   ├── Card/                    # 카드 관리 시스템 (덱, 드로우, 묘지 제어)
│       │   │   ├── CardDrawHandler.cs       # 카드 드로우(Hand/DrawPile 이동) 및 리롤(반납/재드로우) 로직 처리
│       │   │   ├── CardPile.cs              # 4개 카드 영역(Draw, Hand, Selected, Discard)의 순수 데이터 통제
│       │   │   ├── CardSelectionHandler.cs  # 카드 선택 및 해제 상태, 최대 선택 가능 개수 제한 로직
│       │   │   └── DeckSystem.cs            # 카드 시스템의 진입점(Facade) 및 외부(UI/FSM)와의 이벤트 통신 창구
│       │   ├── Damage/                  # 데미지 및 공격력 연산 전담 구역
│       │   │   ├── CombatCalculator.cs      # 데미지/공격력 계산기들을 하나로 묶어 제공하는 단일 창구(Facade)
│       │   │   ├── DamageCal.cs             # 승리 족보 점수를 받아 파이프라인(증폭/방어)을 거친 최종 피해량 계산기
│       │   │   └── StrengthCal.cs           # 제출한 카드 2장의 족보를 판독 후 공격력 파이프라인을 거친 최종 공격력 산출기
│       │   ├── Enemy/                   # 적 AI 및 데이터 관리
│       │   │   └── EnemyData.cs             # 전투 중인 적의 체력, 스탯 및 매 턴 행동 의도(Intent) 보관
│       │   ├── FSM/                     # 전투 진행 단계(턴)를 상태머신 형태로 분할 통제하는 매니저들
│       │   │   ├── BattleEndManager.cs      # 전투 종료 페이즈. 승패 결과창 출력 및 씬 리로드/타이틀 이동 처리
│       │   │   ├── BattleManager.cs         # 턴 상태 변경, Context(배달통) 생성 등 전투 생명주기 전체를 통제하는 심장부
│       │   │   ├── BattleStartManager.cs    # 전투 시작 전 1회 실행. 덱 초기화, 적 세팅, 장신구 장착 진행
│       │   │   ├── TurnEndManager.cs        # 턴 결산 페이즈. 데미지 적용, 버프 수명 차감, 남은 카드 묘지행(정리) 지시
│       │   │   ├── TurnProceedManager.cs    # 메인 행동 페이즈. 유저의 카드 선택 검증 및 예상 공격력 UI 갱신 유도
│       │   │   └── TurnReadyManager.cs      # 턴 시작 페이즈. 멀리건(리롤), 턴 수 증가, 적 의도 결정을 비동기로 지휘
│       │   ├── Item/                    # 플레이어가 사용하는 조커/장신구 인벤토리 시스템
│       │   │   ├── Accessory/               # 영구 지속되는 패시브 아이템 (장신구)
│       │   │   │   ├── AccessoryBase.cs         # 장신구 추상 베이스. 전투 시작 시 파이프라인에 영구 효과 주입/해제 규격 정의
│       │   │   │   └── AccessoryManager.cs      # 장착된 장신구 목록 관리 및 전투 진입 시 일괄 적용 통제 매니저
│       │   │   └── Joker/                   # 1회성 소모품 액티브 아이템 (조커)
│       │   │       ├── JokerBase.cs             # 조커 추상 베이스. 사용 시 효과 부품(ItemModifier)을 조립해 파이프라인에 꽂아넣음
│       │   │       └── JokerManager.cs          # 보유 조커 인벤토리 관리 및 플레이어의 조커 사용(Use) 방아쇠 역할
│       │   └── Modifier/                # 조건(Condition)과 효과(Effect)를 레고 블록처럼 조립하는 파이프라인 버프 엔진
│       │       ├── Conditions/              # '언제 켜질 것인가?'를 판별하는 발동 조건 부품들
│       │       │   ├── Base/                    # 기초 논리 구조 블록
│       │       │   │   ├── AlwaysTrueCondition.cs   # 조건 검사를 생략하고 무조건 통과시키는 프리패스 게이트
│       │       │   │   ├── AndCondition.cs          # 조립된 다수의 조건이 '모두 참'일 때만 통과시키는 논리곱(AND) 게이트
│       │       │   │   └── OrConditions.cs          # 조립된 다수의 조건 중 '하나라도 참'이면 통과시키는 논리합(OR) 게이트
│       │       │   └── Specific/                # 특정 상황을 판별하는 상세 블록
│       │       │       └── TargetTurnCondition.cs   # 지연 발동 기믹을 위해 특정 턴(N턴 뒤)에만 통과시키는 타이머 게이트
│       │       ├── Core/                    # 파이프라인 뼈대 및 엔진 구성 요소
│       │       │   ├── IModifierCondition.cs    # 발동 조건을 판별하는 부품의 인터페이스 규격
│       │       │   ├── IModifierEffect.cs       # 실제 수치를 연산하는 조작 부품의 인터페이스 규격
│       │       │   ├── ItemModifier.cs          # 조건과 효과 부품을 담고 턴(수명)을 차감하는 '규격화된 포장 상자' 클래스
│       │       │   ├── ModifierContext.cs       # 조건 부품들에게 현재 턴, 플레이어 체력, 족보 등의 상황을 담아 전달하는 배달통
│       │       │   ├── ModifierManager.cs       # 등록된 ItemModifier들을 들고 있으며 중간에서 값을 가로채어 연산시키는 중앙 엔진
│       │       │   └── ModifierValueType.cs     # 공격력, 리롤 횟수 등 가로챌 값의 목적지(타겟)를 정의한 Enum
│       │       └── Effects/                 # '어떻게 값을 바꿀 것인가?'를 담당하는 수치 조작 부품들
│       │           └── AddValueEffect.cs        # 들어온 값에 특정 숫자만큼 더하거나 빼는 단순 덧셈 작업자 부품
│       │
│       ├── Core/                        # 프로젝트 전역 시스템 (씬 전환, 이벤트, 전역 매니저)
│       │   ├── BootSceneSetup.cs            # 게임 첫 진입점. 필수 시스템을 메모리에 올린 후 Title 씬으로 자동 이동시킴
│       │   ├── SceneLoader.cs               # Scene 이름(상수) 정의 및 비동기 씬 로드/전환을 전담하는 유틸리티
│       │   ├── Singleton.cs                 # DontDestroyOnLoad를 지원하여 매니저들을 영구 보존하는 제네릭 싱글턴 클래스
│       │   ├── Events/                      # ScriptableObject 기반의 디커플링 통신 채널 (옵저버 패턴)
│       │   │   ├── GameEvent.cs                 # 데이터 전달 없이 신호(Signal)만 주고받는 기본 이벤트 채널
│       │   │   ├── GameEventGeneric.cs          # 제네릭 데이터를 전달하는 이벤트 베이스
│       │   │   ├── GameEventListner.cs          # Inspector에서 UnityEvent로 쉽게 연결할 수 있도록 돕는 리스너 컴포넌트
│       │   │   ├── IntEvent.cs                  # 데미지나 수치 등 정수(int) 데이터를 전달하는 이벤트 채널
│       │   │   └── StringEvent.cs               # 씬 이름이나 ID 등 문자열(string)을 전달하는 이벤트 채널
│       │   └── Managers/                    # 전역 상태를 통제하는 최상위 매니저
│       │       └── GameManager.cs               # 씬 간 화면 전환 결정, 데이터 저장소와 View 연결 등 전체 게임 오케스트레이터
│       │
│       ├── Data/                        # 게임 데이터 구조 (Model 영역)
│       │   ├── BattleContext.cs             # 단일 전투 세션(1회 배틀) 동안 유지되는 승패 등 임시 상태 보관소
│       │   ├── HwaTuCard.cs                 # 화투 카드의 월, 종류, ID 등 순수 스탯을 가진 핵심 데이터 모델
│       │   ├── HwaTuCardDatabase.cs         # Resources 폴더의 카드 SO 데이터들을 런타임 List 객체로 읽어오는 로더 유틸
│       │   ├── PlayerData.cs                # 전투가 끝나도 유지되는 플레이어의 영구 체력, 소지 아이템 ID 보관소
│       │   ├── SeotdaHand.cs                # 광땡, 알리 등 섯다 족보의 종류(Enum)와 각 족보별 '기본 데미지'가 하드코딩된 테이블
│       │   ├── SeotdaJudge.cs               # 카드 2장을 입력받아 실제 섯다 규칙에 맞게 족보 등급을 판별해내는 수학적 코어 로직
│       │   └── ScriptableObjects/           # 기획자가 인스펙터에서 수정 가능한 에셋 템플릿
│       │       └── HwaTuCardSO.cs               # 각 카드의 설정값(월, 이름 등)을 파일 형태로 저장할 수 있게 해주는 에셋 포맷
│       │
│       ├── Map/                         # 로그라이크 맵 분기 생성 및 관리 시스템
│       │   ├── MapData.cs                   # 15층 7열 구조를 가진 생성된 맵 전체의 노드 정보를 담고 있는 컨테이너
│       │   ├── MapGenerator.cs              # Random Seed 기반으로 경로를 뚫고 방의 속성을 겹치지 않게 배정하는 생성 알고리즘
│       │   ├── MapNode.cs                   # 단일 방의 층수, 열, 룸 타입, 연결된 이전/다음 방 정보를 가진 구조체
│       │   └── RoomType.cs                  # 몬스터, 엘리트, 휴식, 보스 등 맵 상의 노드 역할을 구분하는 Enum
│       │
│       ├── Test/                        # 테스트 자동화 및 에디터 시뮬레이션 환경 구축
│       │   ├── BattleTestStarter.cs         # 에디터 플레이 시 강제로 전투 FSM 엔진을 가동시켜주는 임시 버튼 연결용 훅
│       │   ├── CardSOTest.cs                # SO 에셋 20장이 모두 로드되는지, 광 카드의 조건이 맞는지 검사하는 데이터 무결성 테스트
│       │   ├── CardSystemTest.cs            # 가상의 DeckSystem을 띄워 드로우, 턴 결산, 묘지 재활용 로직을 검증하는 시스템 테스트
│       │   └── ItemSystemTest.cs            # 아이템과 Modifier 부품들을 조립해, 매니저가 가로채기 연산을 제대로 하는지 검증
│       │
│       └── UI/                          # 유저에게 화면을 그려주는 표시 영역 (View 영역)
│           ├── Animation/                   # UI 애니메이션 및 시각 효과 전담 컨트롤러
│           │   ├── BattleAnimationController.cs # 화면 흔들림, 데미지 팝업, 씬 페이드 아웃 등 전투의 거시적 연출 통제 오케스트레이터
│           │   ├── CardAnimator.cs              # 개별 카드의 트윈 로직(이동, 띄우기, 스케일업, 낙하 등)을 담당하는 컴포넌트
│           │   └── UITweenHelper.cs             # DoTween 등의 외부 에셋 없이 코루틴만으로 부드러운 UI 이징(Easing)을 처리하는 유틸
│           ├── Battle/                      # 전투 씬 내부의 UI 파트
│           │   ├── BattleSceneSetup.cs          # 전투 씬 진입 시 UIManager 등록 및 전투 뷰 바인딩
│           │   ├── BattleUIComponent.cs         # 체력바 표시, 적 의도 텍스트 갱신, 리롤 버튼 통제 등 전투 상황을 시각화하는 메인 뷰어
│           │   ├── CardUIComponent.cs           # 화면에 스폰된 프리팹 카드 하나의 정보(텍스트/이미지) 매핑 및 클릭 입력 감지
│           │   └── StatusBarUI.cs               # 화면 중앙에 텍스트를 잠깐 띄우고 서서히 페이드아웃 되는 상태 알림창 컴포넌트
│           ├── Core/                        # 공통 UI 코어 시스템
│           │   ├── BaseUIComponent.cs           # 모든 UI 패널이 상속받는 기본 클래스 (Show/Hide/Init, CanvasGroup 조작 등)
│           │   └── UIManager.cs                 # 등록된 UI 객체들의 딕셔너리를 관리하며 화면 전환(표시/숨김)만을 담당하는 스위처
│           ├── Main/                        # 메인 메뉴 씬 UI 파트
│           │   ├── MainSceneSetup.cs            # 메인 메뉴 씬 로드 시 뷰 바인딩 및 셋업
│           │   └── MainUIComponent.cs           # '새 게임', '이어하기' 버튼 이벤트를 받아 GameManager로 전달하는 역할
│           ├── Map/                         # 맵(진행도) 씬 UI 파트
│           │   ├── MapEdgeView.cs               # 맵의 노드와 노드 사이를 연결해주는 검은 선(경로)을 화면에 그리는 컴포넌트
│           │   ├── MapNodeView.cs               # 맵 상의 방 버튼 역할을 하며, 플레이어의 방 선택 클릭을 전달하는 컴포넌트
│           │   ├── MapSceneSetup.cs             # 맵 씬 진입 시 MapGenerator로 생성된 맵 데이터를 UI 컴포넌트에 넘겨주는 셋업
│           │   └── MapUIComponent.cs            # 생성된 노드와 간선 데이터들을 실제 화면 공간(ScrollView)에 정렬하고 스폰하는 역할
│           └── Title/                       # 타이틀 씬 UI 파트
│               ├── TitleSceneSetup.cs           # 게임의 첫 화면, 타이틀 씬 뷰 셋업
│               └── TitleUIComponent.cs          # 화면 하단 'Press Any Key' 문구의 점멸 연출 및 아무 키 입력 감지 후 씬 전환 신호 전달
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
