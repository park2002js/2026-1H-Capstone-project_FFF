using UnityEngine;
using UnityEngine.UI;

namespace FFF.Audio
{
    [RequireComponent(typeof(Button))]
    public class UIButtonSoundEmitter : MonoBehaviour
    {
        [SerializeField] private string _soundId = SoundIds.UiClick;
        [SerializeField, Min(0f)] private float _volumeScale = 1f;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            _button.onClick.AddListener(PlayClickSound);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(PlayClickSound);
        }

        private void PlayClickSound()
        {
            if (!SoundManager.TryGetInstance(out SoundManager soundManager))
                return;

            string soundId = string.IsNullOrWhiteSpace(_soundId) ? soundManager.DefaultUiClickId : _soundId;
            soundManager.PlayUi(soundId, _volumeScale);
        }
    }
}
