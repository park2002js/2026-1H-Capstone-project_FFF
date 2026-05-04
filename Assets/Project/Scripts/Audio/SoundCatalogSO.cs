using System;
using System.Collections.Generic;
using UnityEngine;

namespace FFF.Audio
{
    [CreateAssetMenu(fileName = "SoundCatalog", menuName = "FFF/Audio/Sound Catalog")]
    public class SoundCatalogSO : ScriptableObject
    {
        [Serializable]
        public class SoundEntry
        {
            [SerializeField] private string _soundId;
            [SerializeField] private AudioClip _clip;
            [SerializeField] private SoundBus _bus = SoundBus.Sfx;
            [SerializeField, Range(0f, 1f)] private float _defaultVolume = 1f;
            [SerializeField] private bool _loop;

            public string SoundId => _soundId;
            public AudioClip Clip => _clip;
            public SoundBus Bus => _bus;
            public float DefaultVolume => _defaultVolume;
            public bool Loop => _loop;
        }

        [SerializeField] private List<SoundEntry> _entries = new List<SoundEntry>();

        private Dictionary<string, SoundEntry> _lookup;

        public IReadOnlyList<SoundEntry> Entries => _entries;

        public bool TryGetEntry(string soundId, out SoundEntry entry)
        {
            if (_lookup == null)
                RebuildLookup(logValidation: true);

            if (string.IsNullOrWhiteSpace(soundId))
            {
                entry = null;
                return false;
            }

            return _lookup.TryGetValue(soundId, out entry);
        }

        public void ValidateCatalog()
        {
            RebuildLookup(logValidation: true);
        }

        private void RebuildLookup(bool logValidation)
        {
            _lookup = new Dictionary<string, SoundEntry>();
            var duplicateCheck = new HashSet<string>();

            for (int i = 0; i < _entries.Count; i++)
            {
                SoundEntry entry = _entries[i];
                if (entry == null)
                {
                    if (logValidation)
                        Debug.LogWarning($"[SoundCatalog] Entry index {i} is null.", this);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.SoundId))
                {
                    if (logValidation)
                        Debug.LogWarning($"[SoundCatalog] Entry index {i} has an empty soundId.", this);
                    continue;
                }

                if (entry.Bus == SoundBus.Master)
                {
                    if (logValidation)
                        Debug.LogWarning($"[SoundCatalog] '{entry.SoundId}' uses Master bus. Use Bgm, Sfx, or Ui for playable clips.", this);
                    continue;
                }

                if (!duplicateCheck.Add(entry.SoundId))
                {
                    if (logValidation)
                        Debug.LogError($"[SoundCatalog] Duplicate soundId found: '{entry.SoundId}'. The first entry will be used.", this);
                    continue;
                }

                if (entry.Clip == null && logValidation)
                    Debug.LogWarning($"[SoundCatalog] '{entry.SoundId}' has no AudioClip assigned.", this);

                _lookup.Add(entry.SoundId, entry);
            }
        }

        private void OnValidate()
        {
            _lookup = null;
        }
    }
}
