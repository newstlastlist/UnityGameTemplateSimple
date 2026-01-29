using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Loading
{
    public sealed class LoadingScreenView : MonoBehaviour
    {
        [SerializeField] private bool _showProgressBar = false;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private TextMeshProUGUI _loadingText;
        [SerializeField] private TMP_Text _progressPercentageText;
        [SerializeField] private float _loadingDotsIntervalSeconds = 0.3f;

        private const string LoadingBaseText = "LOADING";
        private const int LoadingDotsMaxCount = 3;

        private Coroutine _loadingTextAnimationCoroutine;
        private int _loadingDotsCount;

        public bool IsVisible => gameObject.activeSelf;
        public bool IsProgressBarEnabled => _showProgressBar;

        private void Awake()
        {
            if (_progressSlider != null)
            {
                _progressSlider.minValue = 0f;
                _progressSlider.maxValue = 1f;
            }

            ApplyVisualMode();
            if (_showProgressBar)
            {
                SetProgress(0f);
            }
            Hide();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            ApplyVisualMode();
            StartLoadingTextAnimationIfNeeded();
        }

        public void Hide()
        {
            StopLoadingTextAnimation();
            gameObject.SetActive(false);
        }

        public void SetProgress(float normalizedProgress)
        {
            if (!_showProgressBar)
            {
                return;
            }

            float clamped = Mathf.Clamp01(normalizedProgress);
            if (_progressSlider != null)
            {
                _progressSlider.value = clamped;
            }
            if (_progressPercentageText != null)
            {
                int percent = Mathf.RoundToInt(clamped * 100f);
                _progressPercentageText.text = percent + "%";
            }
        }

        private void ApplyVisualMode()
        {
            if (_progressSlider != null)
            {
                _progressSlider.gameObject.SetActive(_showProgressBar);
            }

            if (_progressPercentageText != null)
            {
                _progressPercentageText.gameObject.SetActive(_showProgressBar);
            }

            if (_loadingText != null)
            {
                _loadingText.gameObject.SetActive(!_showProgressBar);
                if (!_showProgressBar)
                {
                    _loadingText.text = LoadingBaseText;
                }
            }
        }

        private void StartLoadingTextAnimationIfNeeded()
        {
            if (_showProgressBar)
            {
                return;
            }

            if (_loadingText == null)
            {
                return;
            }

            if (_loadingTextAnimationCoroutine != null)
            {
                return;
            }

            _loadingDotsCount = 0;
            _loadingTextAnimationCoroutine = StartCoroutine(LoadingTextAnimationCoroutine());
        }

        private void StopLoadingTextAnimation()
        {
            if (_loadingTextAnimationCoroutine == null)
            {
                return;
            }

            StopCoroutine(_loadingTextAnimationCoroutine);
            _loadingTextAnimationCoroutine = null;
            _loadingDotsCount = 0;

            if (_loadingText != null)
            {
                _loadingText.text = LoadingBaseText;
            }
        }

        private System.Collections.IEnumerator LoadingTextAnimationCoroutine()
        {
            const float minimumIntervalSeconds = 0.10f;
            const float maximumIntervalSeconds = 0.50f;

            float intervalSeconds = _loadingDotsIntervalSeconds;
            if (intervalSeconds <= 0f)
            {
                intervalSeconds = 0.25f;
            }

            // Защита от неверных значений в префабе: чтобы точки успевали появляться
            // за короткое StartupLoadingMinSeconds (например 1.5 сек).
            intervalSeconds = Mathf.Clamp(intervalSeconds, minimumIntervalSeconds, maximumIntervalSeconds);

            while (true)
            {
                if (_loadingText == null)
                {
                    yield break;
                }

                string dots = _loadingDotsCount <= 0 ? string.Empty : new string('.', _loadingDotsCount);
                _loadingText.text = LoadingBaseText + dots;

                _loadingDotsCount++;
                if (_loadingDotsCount > LoadingDotsMaxCount)
                {
                    _loadingDotsCount = 0;
                }

                yield return new WaitForSecondsRealtime(intervalSeconds);
            }
        }
    }
}


