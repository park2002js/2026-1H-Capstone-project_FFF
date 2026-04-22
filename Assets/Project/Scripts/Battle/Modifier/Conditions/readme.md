Modifier의 폴더 구조는 다음과 같습니다.


└── Battle/
    └── Modifier/
        ├── Core/               # 뼈대 및 매니저
        │   ├── ModifierManager.cs
        │   ├── ItemModifier.cs
        │   ├── ModifierValueType.cs
        │   ├── IModifierCondition.cs
        │   ├── IModifierEffect.cs
        │   └── ModifierContext.cs      # 배달통
        │
        ├── Conditions/         # 언제 발동할 것인가? (게이트키퍼)
        │   ├── Base/           # 기초 논리 부품
        │   │   ├── AlwaysTrueCondition.cs
        │   │   ├── AndCondition.cs
        │   │   └── OrCondition.cs
        │   │
        │   └── Specific/       # 상세 조건 부품
        │       ├── TargetTurnCondition.cs
        │       └── ...  # 등등 기획에 맞춰 추가
        │
        └── Effects/            # 어떻게 바꿀 것인가? (수치 조작)
            ├── AddValueEffect.cs
            └── ... # 등등 기획에 맞춰 추가