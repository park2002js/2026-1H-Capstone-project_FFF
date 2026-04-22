using UnityEngine;
using FFF.Core;
using FFF.UI.Core;
using FFF.Map;

namespace FFF.UI.Map
{
    public class MapSceneSetup : MonoBehaviour
    {
        [SerializeField] private MapUIComponent _mapUI;

        [Header("맵 생성 설정")]
        [SerializeField] private bool _useRandomSeed = true;
        [SerializeField] private int _fixedSeed = 0;

        private void Start()
        {
            if (GameManager.Instance == null || _mapUI == null)
            {
                Debug.LogWarning("[MapSceneSetup] GameManager 또는 MapUI가 null입니다. 씬 단독 테스트 모드로 진행할 수 있습니다.");
            }
            
            int seed = _useRandomSeed ? Random.Range(1, int.MaxValue) : _fixedSeed;
            var mapData = new MapGenerator().Generate(seed);
            
            GameManager.Instance?.OnMapSceneReady(_mapUI, mapData);
        }

        private void OnDestroy()
        {
            GameManager.Instance?.UnregisterScreen(UIScreenNames.MAP);
        }
    }
}
