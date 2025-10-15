using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Loading
{
    public sealed class LoadingScreenView : MonoBehaviour
    {
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private TMP_Text _progressPercentageText;

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            if (_progressSlider != null)
            {
                _progressSlider.minValue = 0f;
                _progressSlider.maxValue = 1f;
            }
            SetProgress(0f);
            Hide();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetProgress(float normalizedProgress)
        {
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
    }
}






