using System;
using System.Collections.Generic;
using UnityEngine;
using FFF.Core.Events;

namespace FFF.Audio
{
    public class SoundEventBinder : MonoBehaviour
    {
        [Serializable]
        public class SoundEventBinding
        {
            [SerializeField] private GameEvent _gameEvent;
            [SerializeField] private string _soundId;
            [SerializeField] private SoundBus _bus = SoundBus.Sfx;
            [SerializeField, Min(0f)] private float _volumeScale = 1f;
            [SerializeField, Min(0f)] private float _fadeSeconds = 0.5f;

            [NonSerialized] public Action RuntimeListener;

            public GameEvent GameEvent => _gameEvent;
            public string SoundId => _soundId;
            public SoundBus Bus => _bus;
            public float VolumeScale => _volumeScale;
            public float FadeSeconds => _fadeSeconds;
        }

        [SerializeField] private List<SoundEventBinding> _bindings = new List<SoundEventBinding>();

        private void OnEnable()
        {
            for (int i = 0; i < _bindings.Count; i++)
                Subscribe(_bindings[i]);
        }

        private void OnDisable()
        {
            for (int i = 0; i < _bindings.Count; i++)
                Unsubscribe(_bindings[i]);
        }

        private void Subscribe(SoundEventBinding binding)
        {
            if (binding == null || binding.GameEvent == null)
                return;

            binding.RuntimeListener = () => PlayBinding(binding);
            binding.GameEvent.Subscribe(binding.RuntimeListener);
        }

        private void Unsubscribe(SoundEventBinding binding)
        {
            if (binding == null || binding.GameEvent == null || binding.RuntimeListener == null)
                return;

            binding.GameEvent.Unsubscribe(binding.RuntimeListener);
            binding.RuntimeListener = null;
        }

        private void PlayBinding(SoundEventBinding binding)
        {
            SoundManager soundManager = SoundManager.EnsureExists();

            switch (binding.Bus)
            {
                case SoundBus.Bgm:
                    soundManager.PlayBgm(binding.SoundId, binding.FadeSeconds);
                    break;
                case SoundBus.Ui:
                    soundManager.PlayUi(binding.SoundId, binding.VolumeScale);
                    break;
                case SoundBus.Sfx:
                    soundManager.PlaySfx(binding.SoundId, binding.VolumeScale);
                    break;
                default:
                    Debug.LogWarning($"[SoundEventBinder] Unsupported sound bus for event sound: {binding.Bus}", this);
                    break;
            }
        }
    }
}
