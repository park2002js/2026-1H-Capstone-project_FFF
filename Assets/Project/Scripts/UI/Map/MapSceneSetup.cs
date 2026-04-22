using UnityEngine;
using FFF.UI.Core;
using FFF.Map;

namespace FFF.UI.Map
{
    /// <summary>
    /// MapScene에 배치하는 부트스트랩 컴포넌트.
    /// BattleSceneSetup과 동일한 패턴으로 동작한다.
    ///
    /// TODO: seed는 현재 Inspector 임시값. GameManager 구현 후 GameManager.Instance.CurrentSeed로 교체.
    /// </summary>
    public class MapSceneSetup : MonoBehaviour
    {
        [Header("이 Scene의 UI 화면")]
        [SerializeField] private MapUIComponent _mapUI;

        [Header("맵 생성 설정 (임시 — GameManager 구현 후 제거)")]
        [SerializeField] private int _seed = 0;

        private void Start()
        {
            if (UIManager.Instance == null || _mapUI == null)
            {
                Debug.LogError("[MapSceneSetup] UIManager 또는 MapUI가 null입니다. BootScene을 먼저 로드했는지 확인하세요.");
                return;
            }

            var mapData = new MapGenerator().Generate(_seed);
            _mapUI.SetMapData(mapData);

            UIManager.Instance.RegisterScreen(UIScreenNames.MAP, _mapUI);
            UIManager.Instance.ShowScreen(UIScreenNames.MAP);
        }

        private void OnDestroy()
        {
            UIManager.Instance?.UnregisterScreen(UIScreenNames.MAP);
        }
    }
}
