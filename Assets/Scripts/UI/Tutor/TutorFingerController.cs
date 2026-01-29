using DG.Tweening;
using UnityEngine;

namespace UI.Tutor
{
    public sealed class TutorFingerController : MonoBehaviour
    {
        [SerializeField] private RectTransform _fingerRect;
        [SerializeField] private float _moveDuration = 0.8f;
        [SerializeField] private float _pulseScale = 1.2f;
        [SerializeField] private float _pulseDuration = 0.6f;

        private Tweener _moveTween;
        private Tweener _pulseTween;

        public void SetVisible(bool visible)
        {
            if (_fingerRect == null)
            {
                return;
            }
            _fingerRect.gameObject.SetActive(visible);
            if (!visible)
            {
                StopAll();
            }
        }

        public void StopAll()
        {
            if (_moveTween != null) { _moveTween.Kill(); _moveTween = null; }
            if (_pulseTween != null) { _pulseTween.Kill(); _pulseTween = null; }
        }

        public void SetScreenPosition(Vector2 screenPosition)
        {
            if (_fingerRect == null)
            {
                return;
            }

            StopMove();
            _fingerRect.position = new Vector3(screenPosition.x, screenPosition.y, _fingerRect.position.z);
        }

        public void MoveTo(Transform target, bool loop)
        {
            if (_fingerRect == null || target == null)
            {
                return;
            }
            StopMove();
            Vector3 endPos = target.position;
            _moveTween = _fingerRect.DOMove(endPos, _moveDuration).SetEase(Ease.InOutSine);
            if (loop)
            {
                _moveTween.SetLoops(-1, LoopType.Yoyo);
            }
        }

        public void Pulse(bool loop)
        {
            if (_fingerRect == null)
            {
                return;
            }
            StopPulse();
            Vector3 baseScale = Vector3.one;
            _fingerRect.localScale = baseScale;
            _pulseTween = _fingerRect.DOScale(_pulseScale, _pulseDuration).SetEase(Ease.InOutSine);
            if (loop)
            {
                _pulseTween.SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void StopMove()
        {
            if (_moveTween != null)
            {
                _moveTween.Kill();
                _moveTween = null;
            }
        }

        private void StopPulse()
        {
            if (_pulseTween != null)
            {
                _pulseTween.Kill();
                _pulseTween = null;
            }
        }
    }
}


