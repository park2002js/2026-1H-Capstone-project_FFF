using UnityEngine;
using UnityEngine.Audio;
using FFF.Core;
using FFF.Audio;

namespace FFF.Core
{
    /// <summary>
    /// BootScene에 배치하는 게임 진입점.
    /// 
    /// 역할:
    /// 1. 싱글턴 매니저들이 Awake에서 자동 초기화되도록 보장
    /// 2. SceneLoader 초기화
    /// 3. 모든 초기화 완료 후 TitleScene으로 전환
    /// 
    /// 게임 실행 시 BootScene이 가장 먼저 로드되어야 한다.
    /// Build Settings에서 BootScene을 index 0에 배치할 것.
    /// </summary>
    public class BootSceneSetup : MonoBehaviour
    {
        [SerializeField] private SoundCatalogSO _soundCatalog;
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private AudioMixerGroup _masterMixerGroup;
        [SerializeField] private AudioMixerGroup _bgmMixerGroup;
        [SerializeField] private AudioMixerGroup _sfxMixerGroup;
        [SerializeField] private AudioMixerGroup _uiMixerGroup;

        private void Start()
        {
            // SceneLoader 초기화 (Scene 로드 이벤트 등록)
            SoundManager soundManager = SoundManager.EnsureExists();
            if (_soundCatalog != null)
                soundManager.SetCatalog(_soundCatalog);
            if (_audioMixer != null || _masterMixerGroup != null || _bgmMixerGroup != null || _sfxMixerGroup != null || _uiMixerGroup != null)
                soundManager.ConfigureAudioMixer(_audioMixer, _masterMixerGroup, _bgmMixerGroup, _sfxMixerGroup, _uiMixerGroup);

            SceneLoader.Initialize();

            Debug.Log("[Boot] 초기화 완료 → TitleScene 전환");

            // 타이틀 화면으로 이동
            SceneLoader.LoadScene(SceneLoader.SceneNames.TITLE);
        }
    }
}
