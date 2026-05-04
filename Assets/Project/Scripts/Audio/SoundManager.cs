using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using FFF.Core;

namespace FFF.Audio
{
    public class SoundManager : Singleton<SoundManager>
    {
        private const float SilentDecibels = -80f;
        private const float MinLinearVolume = 0.0001f;

        private static readonly SoundBus[] VolumeBuses =
        {
            SoundBus.Master,
            SoundBus.Bgm,
            SoundBus.Sfx,
            SoundBus.Ui
        };

        [Serializable]
        public class SceneBgmBinding
        {
            [SerializeField] private string _sceneName;
            [SerializeField] private string _soundId;
            [SerializeField, Min(0f)] private float _fadeSeconds = 1f;

            public string SceneName => _sceneName;
            public string SoundId => _soundId;
            public float FadeSeconds => _fadeSeconds;
        }

        [Header("=== Catalog ===")]
        [SerializeField] private SoundCatalogSO _catalog;
        [SerializeField] private string _defaultUiClickId = SoundIds.UiClick;

        [Header("=== Audio Mixer ===")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private AudioMixerGroup _masterMixerGroup;
        [SerializeField] private AudioMixerGroup _bgmMixerGroup;
        [SerializeField] private AudioMixerGroup _sfxMixerGroup;
        [SerializeField] private AudioMixerGroup _uiMixerGroup;

        [Header("=== Source Settings ===")]
        [SerializeField, Min(1)] private int _sfxPoolSize = 8;
        [SerializeField, Min(0f)] private float _defaultBgmFadeSeconds = 1f;

        [Header("=== Scene BGM ===")]
        [SerializeField] private List<SceneBgmBinding> _sceneBgmBindings = new List<SceneBgmBinding>();
        [SerializeField] private bool _useConventionalSceneBgmIds = true;

        private readonly Dictionary<SoundBus, float> _volumes = new Dictionary<SoundBus, float>();
        private readonly Dictionary<SoundBus, bool> _mutes = new Dictionary<SoundBus, bool>();
        private readonly Dictionary<string, SceneBgmBinding> _sceneBgmLookup = new Dictionary<string, SceneBgmBinding>();
        private readonly HashSet<string> _warningKeys = new HashSet<string>();

        private AudioSource[] _bgmSources;
        private readonly List<AudioSource> _sfxSources = new List<AudioSource>();
        private AudioSource _uiSource;
        private int _activeBgmIndex;
        private int _sfxPoolCursor;
        private string _currentBgmId;
        private Coroutine _bgmFadeRoutine;

        public string DefaultUiClickId => _defaultUiClickId;
        public SoundCatalogSO Catalog => _catalog;

        public static bool TryGetInstance(out SoundManager soundManager)
        {
            soundManager = FindFirstObjectByType<SoundManager>();
            return soundManager != null;
        }

        public static SoundManager EnsureExists()
        {
            if (TryGetInstance(out SoundManager existing))
                return existing;

            var go = new GameObject("[SoundManager]");
            return go.AddComponent<SoundManager>();
        }

        public static void PlayDefaultUiClick()
        {
            if (TryGetInstance(out SoundManager soundManager))
                soundManager.PlayUi(soundManager.DefaultUiClickId);
        }

        protected override void OnInitialize()
        {
            BuildSceneBgmLookup();
            EnsureAudioSources();
            LoadVolumePrefs();
            ApplyAllMixerVolumes();
            ApplyRuntimeSourceVolumes();
            _catalog?.ValidateCatalog();
        }

        public void SetCatalog(SoundCatalogSO catalog)
        {
            _catalog = catalog;
            _warningKeys.Clear();
            _catalog?.ValidateCatalog();
        }

        public void ConfigureAudioMixer(
            AudioMixer audioMixer,
            AudioMixerGroup masterMixerGroup,
            AudioMixerGroup bgmMixerGroup,
            AudioMixerGroup sfxMixerGroup,
            AudioMixerGroup uiMixerGroup)
        {
            _audioMixer = audioMixer;
            _masterMixerGroup = masterMixerGroup;
            _bgmMixerGroup = bgmMixerGroup;
            _sfxMixerGroup = sfxMixerGroup;
            _uiMixerGroup = uiMixerGroup;

            ApplyOutputGroups();
            ApplyAllMixerVolumes();
            ApplyRuntimeSourceVolumes();
        }

        public bool PlaySceneBgm(string sceneName)
        {
            return PlaySceneBgm(sceneName, _defaultBgmFadeSeconds);
        }

        public bool PlaySceneBgm(string sceneName, float fadeSeconds)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            string soundId = null;
            float resolvedFadeSeconds = fadeSeconds;

            if (_sceneBgmLookup.TryGetValue(sceneName, out SceneBgmBinding binding))
            {
                soundId = binding.SoundId;
                resolvedFadeSeconds = binding.FadeSeconds;
            }

            if (string.IsNullOrWhiteSpace(soundId) && _useConventionalSceneBgmIds)
                soundId = SoundIds.GetConventionalSceneBgmId(sceneName);

            if (string.IsNullOrWhiteSpace(soundId))
                return false;

            return PlayBgm(soundId, resolvedFadeSeconds);
        }

        public bool PlayBgm(string soundId, float fadeSeconds)
        {
            if (!TryResolveEntry(soundId, out SoundCatalogSO.SoundEntry entry))
                return false;

            if (!ValidatePlayableEntry(entry, soundId))
                return false;

            WarnBusMismatch(entry, SoundBus.Bgm, soundId);
            EnsureAudioSources();

            AudioSource activeSource = _bgmSources[_activeBgmIndex];
            if (_currentBgmId == soundId && activeSource.isPlaying)
                return true;

            int nextIndex = 1 - _activeBgmIndex;
            AudioSource nextSource = _bgmSources[nextIndex];
            ConfigureBgmSource(nextSource, entry);

            if (_bgmFadeRoutine != null)
                StopCoroutine(_bgmFadeRoutine);

            _bgmFadeRoutine = StartCoroutine(FadeBgm(activeSource, nextSource, nextIndex, soundId, Mathf.Max(0f, fadeSeconds)));
            return true;
        }

        public void StopBgm(float fadeSeconds)
        {
            EnsureAudioSources();

            if (_bgmFadeRoutine != null)
                StopCoroutine(_bgmFadeRoutine);

            AudioSource activeSource = _bgmSources[_activeBgmIndex];
            _bgmFadeRoutine = StartCoroutine(FadeOutBgm(activeSource, Mathf.Max(0f, fadeSeconds)));
        }

        public bool PlaySfx(string soundId, float volumeScale = 1f)
        {
            return PlayOneShot(soundId, SoundBus.Sfx, volumeScale);
        }

        public bool PlayUi(string soundId, float volumeScale = 1f)
        {
            return PlayOneShot(soundId, SoundBus.Ui, volumeScale);
        }

        public void SetVolume(SoundBus bus, float value01)
        {
            if (!IsVolumeBus(bus))
                return;

            _volumes[bus] = Mathf.Clamp01(value01);
            PlayerPrefs.SetFloat(GetVolumeKey(bus), _volumes[bus]);
            PlayerPrefs.Save();

            ApplyMixerVolume(bus);
            ApplyRuntimeSourceVolumes();
        }

        public float GetVolume(SoundBus bus)
        {
            return _volumes.TryGetValue(bus, out float volume) ? volume : 1f;
        }

        public void SetMute(SoundBus bus, bool muted)
        {
            if (!IsVolumeBus(bus))
                return;

            _mutes[bus] = muted;
            PlayerPrefs.SetInt(GetMuteKey(bus), muted ? 1 : 0);
            PlayerPrefs.Save();

            ApplyMixerVolume(bus);
            ApplyRuntimeSourceVolumes();
        }

        public bool IsMuted(SoundBus bus)
        {
            return _mutes.TryGetValue(bus, out bool muted) && muted;
        }

        private bool PlayOneShot(string soundId, SoundBus requestedBus, float volumeScale)
        {
            if (!TryResolveEntry(soundId, out SoundCatalogSO.SoundEntry entry))
                return false;

            if (!ValidatePlayableEntry(entry, soundId))
                return false;

            WarnBusMismatch(entry, requestedBus, soundId);
            EnsureAudioSources();

            float volume = GetSourceVolume(requestedBus, Mathf.Clamp01(entry.DefaultVolume) * Mathf.Max(0f, volumeScale));

            if (requestedBus == SoundBus.Ui)
            {
                _uiSource.volume = 1f;
                _uiSource.PlayOneShot(entry.Clip, volume);
                return true;
            }

            AudioSource source = GetAvailableSfxSource();
            source.volume = 1f;
            source.PlayOneShot(entry.Clip, volume);
            return true;
        }

        private bool TryResolveEntry(string soundId, out SoundCatalogSO.SoundEntry entry)
        {
            entry = null;

            if (string.IsNullOrWhiteSpace(soundId))
            {
                WarnOnce("empty-id", "[SoundManager] Empty soundId requested.");
                return false;
            }

            if (_catalog == null)
            {
                WarnOnce("missing-catalog", "[SoundManager] SoundCatalogSO is not assigned. Create a SoundCatalog asset and assign it to SoundManager.");
                return false;
            }

            if (!_catalog.TryGetEntry(soundId, out entry))
            {
                WarnOnce($"missing-id:{soundId}", $"[SoundManager] Sound id not found in catalog: '{soundId}'.");
                return false;
            }

            return true;
        }

        private bool ValidatePlayableEntry(SoundCatalogSO.SoundEntry entry, string soundId)
        {
            if (entry.Clip != null)
                return true;

            WarnOnce($"missing-clip:{soundId}", $"[SoundManager] Sound id '{soundId}' has no AudioClip assigned.");
            return false;
        }

        private void WarnBusMismatch(SoundCatalogSO.SoundEntry entry, SoundBus requestedBus, string soundId)
        {
            if (entry.Bus == requestedBus)
                return;

            WarnOnce(
                $"bus-mismatch:{soundId}:{requestedBus}",
                $"[SoundManager] Sound id '{soundId}' is registered as {entry.Bus}, but was requested through {requestedBus}."
            );
        }

        private void WarnOnce(string key, string message)
        {
            if (_warningKeys.Add(key))
                Debug.LogWarning(message, this);
        }

        private void EnsureAudioSources()
        {
            if (_bgmSources == null || _bgmSources.Length != 2)
            {
                _bgmSources = new AudioSource[2];
                for (int i = 0; i < _bgmSources.Length; i++)
                    _bgmSources[i] = CreateAudioSource(_bgmMixerGroup);
            }

            if (_sfxSources.Count == 0)
            {
                for (int i = 0; i < Mathf.Max(1, _sfxPoolSize); i++)
                    _sfxSources.Add(CreateAudioSource(_sfxMixerGroup));
            }

            if (_uiSource == null)
                _uiSource = CreateAudioSource(_uiMixerGroup);
        }

        private AudioSource CreateAudioSource(AudioMixerGroup mixerGroup)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = mixerGroup;
            return source;
        }

        private void ApplyOutputGroups()
        {
            if (_bgmSources != null)
            {
                for (int i = 0; i < _bgmSources.Length; i++)
                {
                    if (_bgmSources[i] != null)
                        _bgmSources[i].outputAudioMixerGroup = _bgmMixerGroup;
                }
            }

            for (int i = 0; i < _sfxSources.Count; i++)
            {
                if (_sfxSources[i] != null)
                    _sfxSources[i].outputAudioMixerGroup = _sfxMixerGroup;
            }

            if (_uiSource != null)
                _uiSource.outputAudioMixerGroup = _uiMixerGroup;
        }

        private void ConfigureBgmSource(AudioSource source, SoundCatalogSO.SoundEntry entry)
        {
            source.Stop();
            source.clip = entry.Clip;
            source.loop = entry.Loop;
            source.volume = 0f;
            source.outputAudioMixerGroup = _bgmMixerGroup;
            source.Play();
        }

        private IEnumerator FadeBgm(AudioSource from, AudioSource to, int nextIndex, string nextBgmId, float duration)
        {
            float fromStart = from != null ? from.volume : 0f;
            float toTarget = GetCurrentBgmTargetVolume(nextBgmId);

            if (duration <= 0f)
            {
                if (from != null)
                    from.Stop();
                to.volume = toTarget;
                _activeBgmIndex = nextIndex;
                _currentBgmId = nextBgmId;
                _bgmFadeRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                if (from != null)
                    from.volume = Mathf.Lerp(fromStart, 0f, t);
                to.volume = Mathf.Lerp(0f, toTarget, t);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (from != null)
                from.Stop();
            to.volume = toTarget;
            _activeBgmIndex = nextIndex;
            _currentBgmId = nextBgmId;
            _bgmFadeRoutine = null;
        }

        private IEnumerator FadeOutBgm(AudioSource source, float duration)
        {
            float startVolume = source != null ? source.volume : 0f;

            if (duration <= 0f)
            {
                source?.Stop();
                _currentBgmId = null;
                _bgmFadeRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (source != null)
                    source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            source?.Stop();
            _currentBgmId = null;
            _bgmFadeRoutine = null;
        }

        private AudioSource GetAvailableSfxSource()
        {
            for (int i = 0; i < _sfxSources.Count; i++)
            {
                if (!_sfxSources[i].isPlaying)
                    return _sfxSources[i];
            }

            AudioSource source = _sfxSources[_sfxPoolCursor];
            _sfxPoolCursor = (_sfxPoolCursor + 1) % _sfxSources.Count;
            return source;
        }

        private float GetCurrentBgmTargetVolume(string bgmId)
        {
            if (_catalog != null && _catalog.TryGetEntry(bgmId, out SoundCatalogSO.SoundEntry entry))
                return GetSourceVolume(SoundBus.Bgm, Mathf.Clamp01(entry.DefaultVolume));

            return GetSourceVolume(SoundBus.Bgm, 1f);
        }

        private float GetSourceVolume(SoundBus bus, float baseVolume)
        {
            if (_audioMixer != null)
                return baseVolume;

            return baseVolume * GetEffectiveLinearVolume(SoundBus.Master) * GetEffectiveLinearVolume(bus);
        }

        private void LoadVolumePrefs()
        {
            for (int i = 0; i < VolumeBuses.Length; i++)
            {
                SoundBus bus = VolumeBuses[i];
                _volumes[bus] = Mathf.Clamp01(PlayerPrefs.GetFloat(GetVolumeKey(bus), 1f));
                _mutes[bus] = PlayerPrefs.GetInt(GetMuteKey(bus), 0) == 1;
            }
        }

        private void ApplyAllMixerVolumes()
        {
            for (int i = 0; i < VolumeBuses.Length; i++)
                ApplyMixerVolume(VolumeBuses[i]);
        }

        private void ApplyMixerVolume(SoundBus bus)
        {
            if (_audioMixer == null)
                return;

            string parameter = GetMixerParameter(bus);
            float decibels = LinearToDecibels(GetEffectiveLinearVolume(bus));

            if (!_audioMixer.SetFloat(parameter, decibels))
                WarnOnce($"mixer-param:{parameter}", $"[SoundManager] AudioMixer exposed parameter not found: '{parameter}'.");
        }

        private void ApplyRuntimeSourceVolumes()
        {
            if (_audioMixer != null || _bgmSources == null)
                return;

            if (!string.IsNullOrWhiteSpace(_currentBgmId) && _catalog != null && _catalog.TryGetEntry(_currentBgmId, out SoundCatalogSO.SoundEntry entry))
                _bgmSources[_activeBgmIndex].volume = GetSourceVolume(SoundBus.Bgm, Mathf.Clamp01(entry.DefaultVolume));
        }

        private float GetEffectiveLinearVolume(SoundBus bus)
        {
            return IsMuted(bus) ? 0f : GetVolume(bus);
        }

        private static float LinearToDecibels(float value)
        {
            if (value <= MinLinearVolume)
                return SilentDecibels;

            return Mathf.Log10(value) * 20f;
        }

        private static bool IsVolumeBus(SoundBus bus)
        {
            return bus == SoundBus.Master || bus == SoundBus.Bgm || bus == SoundBus.Sfx || bus == SoundBus.Ui;
        }

        private static string GetMixerParameter(SoundBus bus)
        {
            switch (bus)
            {
                case SoundBus.Master:
                    return "MasterVolume";
                case SoundBus.Bgm:
                    return "BgmVolume";
                case SoundBus.Sfx:
                    return "SfxVolume";
                case SoundBus.Ui:
                    return "UiVolume";
                default:
                    return "MasterVolume";
            }
        }

        private static string GetVolumeKey(SoundBus bus)
        {
            return $"FFF.Sound.{bus}.Volume";
        }

        private static string GetMuteKey(SoundBus bus)
        {
            return $"FFF.Sound.{bus}.Mute";
        }

        private void BuildSceneBgmLookup()
        {
            _sceneBgmLookup.Clear();

            for (int i = 0; i < _sceneBgmBindings.Count; i++)
            {
                SceneBgmBinding binding = _sceneBgmBindings[i];
                if (binding == null || string.IsNullOrWhiteSpace(binding.SceneName) || string.IsNullOrWhiteSpace(binding.SoundId))
                    continue;

                if (_sceneBgmLookup.ContainsKey(binding.SceneName))
                {
                    WarnOnce($"scene-bgm:{binding.SceneName}", $"[SoundManager] Duplicate scene BGM binding for '{binding.SceneName}'. The first binding will be used.");
                    continue;
                }

                _sceneBgmLookup.Add(binding.SceneName, binding);
            }
        }

        private void OnValidate()
        {
            _sfxPoolSize = Mathf.Max(1, _sfxPoolSize);
            _defaultBgmFadeSeconds = Mathf.Max(0f, _defaultBgmFadeSeconds);
        }
    }
}
