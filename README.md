# 🎮 2026 Capstone Design Project: FFF

> **FFF (Forge's Flower Fight)**는 한국 화투와 섯다 족보를 기반으로 한 1인용 로그라이크 덱 빌딩 게임입니다.  
> 플레이어는 매 전투마다 카드를 선택해 족보를 만들고, 장신구와 조커 효과를 조합하며, 분기형 맵을 따라 최종 보스까지 도달해야 합니다.

[GitHub Repository](https://github.com/park2002js/2026-1H-Capstone-project_FFF)

---

## 목차

- [프로젝트 소개](#프로젝트-소개)
- [주요 특징](#주요-특징)
- [게임 플레이 흐름](#게임-플레이-흐름)
- [핵심 시스템](#핵심-시스템)
- [개발 환경](#개발-환경)
- [시작 가이드](#시작-가이드)
- [브랜치 전략](#브랜치-전략)
- [프로젝트 구조](#프로젝트-구조)
- [아키텍처 설계](#아키텍처-설계)
- [팀원 소개](#팀원-소개)

---

## 프로젝트 소개

FFF는 짧은 전투 선택과 장기적인 덱 성장을 결합한 카드 전략 게임입니다.  
기본 전투 규칙은 화투 카드 2장을 제출해 섯다 족보를 판정하는 방식이며, 플레이어는 전투 보상으로 카드, 조커, 장신구를 획득해 다음 전투를 준비합니다.

게임은 `타이틀 -> 메인 -> 맵 -> 전투/상점/휴식/보상 -> 맵 복귀 -> 보스전 -> 엔딩` 흐름으로 진행됩니다.  
각 전투에서는 카드 선택, 리롤, 조커 사용, 적 의도 확인, 턴 종료 판단이 핵심 선택지로 작동합니다.

---

## 주요 특징

- **1인 싱글플레이**: 혼자서 완결된 러닝을 즐기는 전략 중심 플레이
- **화투 기반 전투**: 카드 2장으로 섯다 족보를 만들고 공격력을 계산
- **덱 빌딩**: 보상으로 획득한 카드를 덱에 추가하며 플레이 스타일 확장
- **조커 시스템**: 전투 중 한 번 사용해 공격력, 데미지, 드로우 등 변수를 만드는 액티브 아이템
- **장신구 시스템**: 전투 시작 시 자동 적용되는 패시브 아이템
- **로그라이크 맵**: 몬스터, 엘리트, 상점, 휴식, 보상, 보스 노드로 구성된 분기형 진행
- **Modifier 기반 효과 엔진**: 조건과 효과를 조립해 아이템/적 기믹을 확장하기 쉬운 구조

---

## 게임 플레이 흐름

```text
BootScene
  -> TitleScene
  -> MainScene
  -> StageScene
      -> BattleScene
      -> ShopScene
      -> RestScene
      -> TreasureScene
      -> StageScene
  -> EndingScene
```

1. `BootScene`에서 전역 매니저와 씬 로더를 초기화합니다.
2. `TitleScene`과 `MainScene`에서 게임 시작 또는 이어하기를 선택합니다.
3. `StageScene`에서 로그라이크 맵 노드를 선택합니다.
4. 전투, 상점, 휴식, 보상 씬을 거치며 플레이어 데이터를 성장시킵니다.
5. 보스 노드를 클리어하면 엔딩 씬으로 이동합니다.

---

## 핵심 시스템

### 전투 FSM

전투는 상태 머신 방식으로 진행됩니다.

- `BattleStart`: 전투 데이터, 덱, 적, 장신구 초기화
- `TurnReady`: 턴 시작, 카드 드로우, 적 의도 결정
- `TurnProceed`: 카드 선택, 리롤, 조커 사용, 예상 공격력 갱신
- `TurnEnd`: 공격/피해 계산, 버프 턴 차감, 카드 정리
- `BattleEnd`: 승패 처리, 골드 지급, 보상 선택

### 카드와 족보

- `HwaTuCard`: 카드의 월, 타입, ID 등 기본 데이터
- `HwaTuCardSO`: Unity Inspector에서 관리하는 카드 ScriptableObject
- `HwaTuCardDatabase`: Resources의 카드 데이터를 로드하는 진입점
- `SeotdaJudge`: 카드 2장을 섯다 족보로 판정
- `StrengthCal`, `DamageCal`: 공격력과 최종 데미지 계산

### 아이템

- `ItemBase`: 조커/장신구 공통 베이스
- `JokerItemBase`: 전투 중 소모되는 액티브 아이템
- `AccessoryItemBase`: 전투에 지속 적용되는 패시브 아이템
- `ItemFactory`: 문자열 ID를 실제 아이템 객체로 변환
- `ItemDataSO`: 아이템 이름, 설명, 아이콘, 가격 데이터

### Modifier 효과 엔진

아이템과 적 기믹은 `조건(Condition)`과 `효과(Effect)`를 조립해 만듭니다.

- `BattleModifier`: 조건, 효과, 지속 턴을 가진 효과 단위
- `ModifierManager`: 등록된 Modifier를 보관하고 계산 파이프라인에 적용
- `ModifierContext`: 현재 턴, 카드, 공격 주체 등 조건 판정에 필요한 전투 상황
- `IModifierCondition`: 효과가 발동할 조건 인터페이스
- `IModifierEffect`: 값을 어떻게 바꿀지 정의하는 효과 인터페이스

### 맵 진행

- `MapGenerator`: 시드 기반 로그라이크 맵 생성
- `MapData`: 전체 맵 데이터 컨테이너
- `MapNode`: 단일 노드의 층, 위치, 방 타입, 연결 정보
- `RoomType`: 몬스터, 엘리트, 상점, 휴식, 보상, 보스 타입 정의
- `GameManager`: 노드 선택 후 적절한 씬으로 이동시키는 전체 진행 관리자

---

## 개발 환경

| 항목 | 내용 |
| :--- | :--- |
| Engine | `Unity Engine 6000.0.71f1` |
| IDE | `Visual Studio 2022` |
| Render Pipeline | `Universal Render Pipeline` |
| Target | PC / Standalone 중심 |
| Language | `C#` |

> 반드시 Unity `6000.0.71f1` 버전으로 실행하는 것을 권장합니다. 다른 버전으로 열 경우 Library 재생성, 패키지 충돌, 씬 직렬화 차이가 발생할 수 있습니다.

---

## 시작 가이드

### 1. 저장소 클론

```bash
git clone https://github.com/park2002js/2026-1H-Capstone-project_FFF.git
```

### 2. Unity Hub에 프로젝트 추가

1. Unity Hub를 실행합니다.
2. `Add` 또는 `Add project from disk`를 선택합니다.
3. 클론 받은 프로젝트 루트 폴더를 선택합니다.
4. Unity `6000.0.71f1` 버전으로 프로젝트를 엽니다.

### 3. 실행

1. `Assets/Project/Scenes/BootScene.unity`를 엽니다.
2. Unity Editor 상단의 Play 버튼을 누릅니다.
3. `BootScene`이 전역 시스템을 초기화한 뒤 `TitleScene`으로 이동합니다.

### 4. 빌드 시 확인 사항

- `ProjectSettings/EditorBuildSettings.asset`에서 `BootScene`이 첫 번째 씬인지 확인합니다.
- Resources 기반 로드가 필요한 데이터는 `Assets/Project/Resources` 하위에 위치해야 합니다.
- 플레이어, 적, 카드, 아이템 ID는 코드의 Factory/Table 데이터와 SO 파일명이 일치해야 합니다.

---

## 브랜치 전략

Main-Dev-Feature 구조를 따릅니다.

```text
main
└── dev
    ├── feature/battle-system
    ├── feature/map-ui
    ├── feature/item-modifier
    └── feature/audio
```

- `main`: 안정 빌드 또는 발표용 버전
- `dev`: 기능 통합 및 QA 대상 브랜치
- `feature/*`: 기능 단위 개발 브랜치

개발 흐름은 다음과 같습니다.

1. `dev`에서 feature 브랜치를 생성합니다.
2. 기능 구현 후 자체 테스트를 진행합니다.
3. Pull Request 또는 팀 리뷰 후 `dev`로 병합합니다.
4. 주요 마일스톤에서 `main`으로 병합합니다.

<img width="264" height="150" alt="feature-branch" src="https://github.com/user-attachments/assets/14ff024a-2094-4b85-8336-d6c80d5bdf74" />

---

## 프로젝트 구조

```text
Assets/Project/
├── Art/                    # 게임에 직접 쓰이는 이미지 원본
│   ├── Accessories/         # 장신구 아이콘
│   ├── Background/          # 씬/전투 배경
│   ├── Cards/               # 화투 카드 이미지
│   ├── Characters/          # 적 캐릭터 이미지
│   ├── Exception/           # ErrorSprite 같은 fallback 이미지
│   ├── Joker/               # 조커 카드 이미지
│   ├── Player/              # 플레이어 캐릭터 이미지
│   └── UI/                  # UI 아이콘/폰트 에셋
│
├── Audio/                  # 오디오 원본 또는 분류 보관
│   ├── BGM/
│   └── SFX/
│
├── Prefabs/                # 재사용 가능한 Unity 프리팹
│   ├── FlowerCards/         # 화투 카드 기반 프리팹 세트
│   └── UI/                  # UI 컴포넌트/화면 프리팹
│
├── Resources/              # 런타임 Resources.Load 대상
│   ├── Cards/               # HwaTuCardSO 카드 데이터
│   ├── SO/Item/             # Joker_001, Accessory_001 같은 아이템 데이터 SO
│   ├── SO/Enemy/            # Enemy_001~ 적 데이터 SO
│   ├── UI/Jokbo/            # 족보 안내 이미지
│   └── Audio/               # 런타임 로드용 BGM/SFX
│
├── Scenes/                 # Unity 씬
│   ├── BootScene.unity      # 초기 시스템 부팅
│   ├── TitleScene.unity     # 타이틀
│   ├── MainScene.unity      # 메인 메뉴
│   ├── StageScene.unity     # 맵/스테이지 진행
│   ├── BattleScene.unity    # 전투
│   ├── ShopScene.unity      # 상점
│   ├── RestScene.unity      # 휴식
│   ├── TreasureScene.unity  # 보상/상자
│   └── EndingScene.unity    # 엔딩
│
├── ScriptableObjects/      # 에디터에서 연결하는 전역 SO
│   ├── Events/              # OnTurnReady, OnBattleEnd 등 이벤트 채널
│   └── Audio/               # SoundCatalog
│
└── Scripts/                # 게임 로직 전체
    ├── Audio/               # 사운드 시스템
    ├── Battle/              # 전투 진행, 카드, 데미지, Modifier
    ├── Core/                # 씬 전환, 이벤트, 전역 매니저
    ├── Data/                # 카드/아이템/적/플레이어 데이터 모델
    ├── Map/                 # 로그라이크 맵 생성/노드 구조
    └── UI/                  # 각 씬의 화면 구성과 입력 처리
```

### Scripts 세부 구조

```text
Scripts/
├── Audio/
│   ├── SoundManager.cs          # BGM/SFX/UI 사운드 재생 중심
│   ├── SoundCatalogSO.cs        # 사운드 ID와 AudioClip 매핑 데이터
│   ├── SoundIds.cs              # 사운드 ID 상수 모음
│   └── SoundBus.cs              # Master/BGM/SFX/UI 볼륨 분류
│
├── Battle/
│   ├── Card/                    # 덱, 손패, 선택 카드, 묘지 관리
│   ├── Damage/                  # 공격력/피해량 계산
│   ├── Data/                    # 전투 중 임시 데이터
│   ├── FSM/                     # 전투 상태 머신
│   └── Modifier/                # 조건/효과 기반 버프 엔진
│
├── Core/
│   ├── Events/                  # ScriptableObject 이벤트 채널
│   ├── Managers/                # GameManager, TableSystemManager, Singleton
│   └── Scene/                   # SceneLoader, BootSceneSetup, LoadingScreenController
│
├── Data/
│   ├── Card/                    # 화투 카드, 족보, 판정 로직
│   ├── Enemy/                   # 적 SO, AI, 기믹
│   ├── Item/                    # 조커/장신구 로직과 Factory
│   └── Player/                  # 플레이어 영구 데이터와 전투 데이터 동기화
│
├── Map/
│   ├── MapGenerator.cs          # 시드 기반 맵 생성
│   ├── MapData.cs               # 맵 전체 데이터
│   ├── MapNode.cs               # 단일 노드 데이터
│   └── RoomType.cs              # 방 타입 enum
│
└── UI/
    ├── Animation/               # 카드/전투/캐릭터 연출
    ├── Battle/                  # 전투 화면
    ├── Common/                  # 공통 HUD, 툴팁, 설정 UI
    ├── Core/                    # UIManager, BaseUIComponent
    ├── Main/                    # 메인 메뉴
    ├── Map/                     # 맵 화면
    ├── Rest/                    # 휴식 화면
    ├── Shop/                    # 상점 화면
    ├── Title/                   # 타이틀 화면
    └── Treasure/                # 보상 상자 화면
```

---

## 아키텍처 설계

### MVC/MVP 역할 분리

| 역할 | 폴더 | 설명 |
| :--- | :--- | :--- |
| Model | `Scripts/Data`, `Resources`, `ScriptableObjects` | 카드, 플레이어, 적, 아이템 데이터 |
| View | `Scripts/UI` | 화면 표시, 버튼, HUD, 애니메이션 |
| Controller/Presenter | `Scripts/Core`, `Scripts/Battle`, `Scripts/Map` | 흐름 제어, 전투 상태 전환, 맵 진행 |

### 데이터 흐름

```text
Player Input
  -> UIComponent
  -> GameManager / BattleManager / Scene Manager
  -> Data / Battle System / Modifier System
  -> UI Refresh
```

### 씬 전환 책임

- `SceneLoader`: 씬 이름 상수와 실제 로드 처리
- `GameManager`: 어떤 씬으로 이동할지 결정
- `SceneSetup`: 씬이 로드된 뒤 필요한 UI와 매니저 참조를 연결
- `UIManager`: 등록된 UI 패널 표시와 숨김 처리

### Resources 사용 기준

- 런타임에 ID로 찾아야 하는 데이터는 `Resources` 하위에 배치합니다.
- 카드 데이터는 `Resources/Cards`
- 아이템 데이터는 `Resources/SO/Item`
- 적 데이터는 `Resources/SO/Enemy`
- 사운드 클립은 `Resources/Audio`

---

## 주요 클래스 빠른 참조

| 클래스 | 역할 |
| :--- | :--- |
| `GameManager` | 전체 게임 흐름, 씬 전환, 맵 노드 선택, 데이터 동기화 |
| `SceneLoader` | Unity 씬 로드 유틸리티 |
| `BattleManager` | 전투 상태 머신의 중심 |
| `DeckSystem` | 덱/드로우/선택/묘지 시스템의 Facade |
| `BattleEndManager` | 전투 종료, 보상 선택, 결과 처리 |
| `MapGenerator` | 로그라이크 맵 생성 |
| `ItemFactory` | 아이템 ID를 실제 C# 객체로 생성 |
| `ModifierManager` | 등록된 전투 효과를 계산 파이프라인에 적용 |
| `BattleUIComponent` | 전투 화면의 주요 View |
| `TopRunHudComponent` | 상단 HUD, 보유 카드/아이템 요약 |

---

## 팀원 소개

| 이름 | GitHub ID |
| :--- | :--- |
| 김찬영 | [@a5153203](https://github.com/a5153203) |
| 박지성 | [@park2002js](https://github.com/park2002js) |
| 정혁진 | [@Vqlntinx](https://github.com/Vqlntinx) |

---

## 라이선스

본 프로젝트는 2026년 1학기 캡스톤 디자인 수업 프로젝트로 제작되었습니다.  
외부 에셋, 사운드, 폰트 사용 범위는 각 에셋의 라이선스 정책을 따릅니다.
