using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FFF.Audio;
using FFF.Core;

namespace FFF.UI.Ending
{
    public sealed class EndingSceneController : MonoBehaviour
    {
        [Header("Scene UI")]
        [SerializeField] private Image _fadePanel;
        [SerializeField] private Image _fadeoutImage;
        [SerializeField] private GameObject _dialoguePanel;
        [SerializeField] private TMP_Text[] _dialogueTexts;
        [SerializeField] private TMP_Text _endingTitle;
        [SerializeField] private GameObject _buttonPanel;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _exitButton;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _initialBlackHoldDuration = 0.6f;
        [SerializeField, Min(0f)] private float _openingFadeDuration = 1.5f;
        [SerializeField, Min(0f)] private float _dialogueFadeDuration = 0.6f;
        [SerializeField, Min(0f)] private float _dialogueVisibleDuration = 2.5f;
        [SerializeField, Min(0f)] private float _betweenDialogueDelay = 0.3f;
        [SerializeField, Min(0f)] private float _endingFadeDuration = 1.5f;
        [SerializeField, Min(0f)] private float _titleToButtonsDelay = 1.5f;

        private bool _isLeavingScene;

        private void Awake()
        {
            if (_mainMenuButton != null)
                _mainMenuButton.onClick.AddListener(ReturnToMainMenu);

            if (_exitButton != null)
                _exitButton.onClick.AddListener(QuitGame);

            PrepareInitialState();
        }

        private void Start()
        {
            if (HasRequiredReferences())
                StartCoroutine(PlayEndingSequence());
        }

        private void OnDestroy()
        {
            if (_mainMenuButton != null)
                _mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

            if (_exitButton != null)
                _exitButton.onClick.RemoveListener(QuitGame);
        }

        private void PrepareInitialState()
        {
            SetGraphicState(_fadePanel, true, 1f);
            SetGraphicState(_fadeoutImage, false, 0f);

            if (_dialoguePanel != null)
                _dialoguePanel.SetActive(true);

            if (_dialogueTexts != null)
            {
                foreach (TMP_Text dialogueText in _dialogueTexts)
                    SetGraphicState(dialogueText, false, 0f);
            }

            SetGraphicState(_endingTitle, false, 1f);

            if (_buttonPanel != null)
                _buttonPanel.SetActive(false);

            SetButtonsInteractable(false);
        }

        private IEnumerator PlayEndingSequence()
        {
            yield return WaitRealtime(_initialBlackHoldDuration);
            yield return FadeGraphic(_fadePanel, 0f, _openingFadeDuration);
            _fadePanel.gameObject.SetActive(false);

            foreach (TMP_Text dialogueText in _dialogueTexts)
            {
                if (dialogueText == null)
                    continue;

                dialogueText.gameObject.SetActive(true);
                yield return FadeGraphic(dialogueText, 1f, _dialogueFadeDuration);
                yield return WaitRealtime(_dialogueVisibleDuration);
                yield return FadeGraphic(dialogueText, 0f, _dialogueFadeDuration);
                dialogueText.gameObject.SetActive(false);
                yield return WaitRealtime(_betweenDialogueDelay);
            }

            _dialoguePanel.SetActive(false);

            _fadeoutImage.gameObject.SetActive(true);
            yield return FadeGraphic(_fadeoutImage, 1f, _endingFadeDuration);

            _endingTitle.gameObject.SetActive(true);
            yield return WaitRealtime(_titleToButtonsDelay);

            _buttonPanel.SetActive(true);
            SetButtonsInteractable(true);
        }

        private void ReturnToMainMenu()
        {
            if (_isLeavingScene)
                return;

            _isLeavingScene = true;
            SetButtonsInteractable(false);
            SoundManager.PlayDefaultUiClick();
            SceneLoader.LoadScene(SceneLoader.SceneNames.MAIN);
        }

        private void QuitGame()
        {
            if (_isLeavingScene)
                return;

            _isLeavingScene = true;
            SetButtonsInteractable(false);
            SoundManager.PlayDefaultUiClick();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private bool HasRequiredReferences()
        {
            bool hasReferences =
                _fadePanel != null &&
                _fadeoutImage != null &&
                _dialoguePanel != null &&
                _dialogueTexts != null &&
                _dialogueTexts.Length > 0 &&
                _endingTitle != null &&
                _buttonPanel != null &&
                _mainMenuButton != null &&
                _exitButton != null;

            if (!hasReferences)
                Debug.LogError("[EndingSceneController] EndingScene UI reference is missing.", this);

            return hasReferences;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_mainMenuButton != null)
                _mainMenuButton.interactable = interactable;

            if (_exitButton != null)
                _exitButton.interactable = interactable;
        }

        private static void SetGraphicState(Graphic graphic, bool active, float alpha)
        {
            if (graphic == null)
                return;

            SetAlpha(graphic, alpha);
            graphic.gameObject.SetActive(active);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        private static IEnumerator FadeGraphic(Graphic graphic, float targetAlpha, float duration)
        {
            if (graphic == null)
                yield break;

            float startAlpha = graphic.color.a;
            if (duration <= 0f)
            {
                SetAlpha(graphic, targetAlpha);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(graphic, Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SetAlpha(graphic, targetAlpha);
        }

        private static IEnumerator WaitRealtime(float duration)
        {
            if (duration > 0f)
                yield return new WaitForSecondsRealtime(duration);
        }
    }
}
