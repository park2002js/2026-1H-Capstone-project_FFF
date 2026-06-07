using System;
using UnityEngine;
using FFF.Battle.Modifier;

namespace FFF.Data
{
    /// <summary>
    /// 모든 아이템(장신구, 조커)의 공통 로직 규격.
    /// 기존 AccessoryBase 및 JokerBase를 대체하는 단일 베이스 클래스.
    /// </summary>
    public abstract class ItemBase
    {
        /// <summary> 아이템 생성 및 관리를 위한 고유 식별자 </summary>
        public abstract string Id { get; }

        /// <summary> 동적 로드를 위한 이미지 파일 이름 (예: "boone", 확장자 제외) </summary>
        protected abstract string SpriteFileName { get; }
        
        /// <summary> 에셋이 위치한 최상위 폴더 경로 </summary>
        protected abstract string BaseFolderPath { get; }

        private Sprite _cachedIcon;


        // SO 데이터 캐싱
        private ItemDataSO _cachedItemData;
        
        /// <summary> Resources/SO/Item/ 경로에서 Id와 일치하는 SO 에셋 로드 </summary>
        protected virtual ItemDataSO LoadedItemData
        {
            get
            {
                if (_cachedItemData == null)
                {
                    _cachedItemData = Resources.Load<ItemDataSO>($"SO/Item/{Id}");
                    if (_cachedItemData == null)
                    {
                        Debug.LogWarning($"[ItemBase] SO 에셋 로드 실패. 파일명 확인 요망: Resources/SO/Item/{Id}");
                    }
                }
                return _cachedItemData;
            }
        }

        /// <summary> SO의 Icon 최우선 반환. 없을 경우 기존 캐싱 로직 사용 </summary>
        public Sprite Icon
        {
            get
            {
                if (LoadedItemData != null && LoadedItemData.Icon != null)
                    return LoadedItemData.Icon;

                if (_cachedIcon == null)
                {
                    _cachedIcon = LoadSpriteAsset(BaseFolderPath, SpriteFileName);
                }
                return _cachedIcon;
            }
        }

        // SO 데이터의 텍스트 최우선 반환, 없으면 하드코딩 텍스트를 사용
        /// <summary> UI에 표시될 아이템의 이름 </summary>
        public virtual string DisplayName => LoadedItemData != null ? LoadedItemData.DisplayName : Id;
        
        /// <summary> UI에 표시될 아이템의 설명 텍스트 </summary>
        public virtual string Description => LoadedItemData != null ? LoadedItemData.Description : "설명 누락";
        

        /// <summary>
        /// 지정된 경로에서 Sprite 에셋을 로드함.
        /// 실패 시 ErrorSprite를 반환함.
        /// </summary>
        private Sprite LoadSpriteAsset(string folderPath, string fileName)
        {
            Sprite loadedSprite = null;

#if UNITY_EDITOR
            // 에디터 환경: 주어진 Assets 경로에서 직접 로드 시도 (기본 확장자 .png 가정)
            string fullPath = $"{folderPath}/{fileName}.png";
            loadedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);

            // 해당 경로에 이미지가 존재하지 않을 경우 폴백(Fallback) 처리
            if (loadedSprite == null)
            {
                string errorPath = "Assets/Project/Art/Exception/ErrorSprite.png";
                loadedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(errorPath);
                Debug.LogWarning($"[ItemBase] 이미지를 찾을 수 없습니다: {fullPath} -> ErrorSprite로 대체됨.");
            }
#else
            // TODO: 실제 빌드 시에는 Resources.Load 또는 Addressables.LoadAssetAsync를 사용하도록 수정 필요
            Debug.LogWarning("[ItemBase] 빌드 환경에서의 동적 에셋 로드 로직이 구현되지 않았습니다.");
#endif
            return loadedSprite;
        }

        /// <summary>
        /// 모든 아이템의 효과 발동 통일 규격.
        /// ModifierManager에 자신의 효과를 담은 BattleModifier를 1회 등록함.
        /// </summary>
        /// <param name="context">현재 전투 컨텍스트 (효과 적용용)</param>
        /// <param name="onConsume">소모성 아이템(조커)의 경우 등록 직후 처리를 위한 콜백 (기본값 null)</param>
        public abstract void Apply(ModifierContext context, Action onConsume = null);

        /// <summary>
        /// 장신구 등 영구 적용 버프의 명시적 해제 규격.
        /// </summary>
        public abstract void Remove(ModifierContext context);
    }
}