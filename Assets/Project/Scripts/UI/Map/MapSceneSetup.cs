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
            if (_mapUI == null)
            {
                Debug.LogWarning("[MapSceneSetup] MapUI가 null입니다.");
                return;
            }

            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                var mapData = gameManager.GetOrCreateRunMap(_useRandomSeed, _fixedSeed);
                gameManager.OnMapSceneReady(_mapUI, mapData);
                return;
            }

            Debug.LogWarning("[MapSceneSetup] GameManager가 null입니다. 씬 단독 테스트 모드로 진행합니다.");
            int seed = _useRandomSeed ? Random.Range(1, int.MaxValue) : _fixedSeed;
            var standaloneMapData = new MapGenerator().Generate(seed);
            foreach (var node in standaloneMapData.GetFloor(0))
            {
                node.IsReachable = true;
            }

            _mapUI.SetMapData(standaloneMapData);
            _mapUI.Show();
        }

        private void OnDestroy()
        {
            GameManager.Instance?.UnregisterScreen(UIScreenNames.MAP);
        }
    }
}
