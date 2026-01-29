#if UNITY_EDITOR || DEVELOPMENT_BUILD
using LunarConsolePlugin;
using UnityEngine;

namespace Dev
{
    public sealed class DevLunarConsoleTwoFingerTripleTapOpener : MonoBehaviour
    {
        private const int RequiredTapCount = 3;

        private const float MaxIntervalBetweenTapsSeconds = 0.35f;
        private const float MaxGestureDurationSeconds = 0.9f;

        private int _tapCount;
        private float _firstTapTime;
        private float _lastTapTime;

        private void Update()
        {
            if (!TryConsumeTwoFingerTap())
            {
                ResetIfTimedOut();
                return;
            }

            float now = Time.unscaledTime;

            if (_tapCount == 0)
            {
                _firstTapTime = now;
                _lastTapTime = now;
                _tapCount = 1;
                return;
            }

            if (now - _lastTapTime > MaxIntervalBetweenTapsSeconds)
            {
                _firstTapTime = now;
                _lastTapTime = now;
                _tapCount = 1;
                return;
            }

            _tapCount++;
            _lastTapTime = now;

            if (_tapCount >= RequiredTapCount && now - _firstTapTime <= MaxGestureDurationSeconds)
            {
                LunarConsole.Show();
                ResetState();
            }
        }

        private bool TryConsumeTwoFingerTap()
        {
            if (Input.touchCount != 2)
            {
                return false;
            }

            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            return touch0.phase == TouchPhase.Began && touch1.phase == TouchPhase.Began;
        }

        private void ResetIfTimedOut()
        {
            if (_tapCount <= 0)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now - _lastTapTime > MaxIntervalBetweenTapsSeconds || now - _firstTapTime > MaxGestureDurationSeconds)
            {
                ResetState();
            }
        }

        private void ResetState()
        {
            _tapCount = 0;
            _firstTapTime = 0f;
            _lastTapTime = 0f;
        }
    }
}
#endif


