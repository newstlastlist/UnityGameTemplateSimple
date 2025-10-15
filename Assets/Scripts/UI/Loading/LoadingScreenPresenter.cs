using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UI.Loading
{
    public sealed class LoadingScreenPresenter : IDisposable
    {
        private readonly LoadingScreenView _view;
        private CancellationTokenSource _cts;

        private const float MaxDisplayedProgressBeforeReady = 0.95f;
        private const float MinimumVisibleSeconds = 2f;

        public LoadingScreenPresenter(LoadingScreenView view)
        {
            _view = view;
        }

        public void Dispose()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        public async Task Run(Func<IProgress<float>, Task> loadOperation)
        {
            CancelCurrent();
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            _view.SetProgress(0f);
            _view.Show();

            var displayedProgress = 0f;
            var timeProgress = 0f;
            var realProgress = 0f;
            float startTime = Time.realtimeSinceStartup;

            var progressReporter = new Progress<float>(p =>
            {
                realProgress = Mathf.Clamp01(p);
            });

            Task loadTask = loadOperation != null ? loadOperation(progressReporter) : Task.CompletedTask;

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                float elapsed = Time.realtimeSinceStartup - startTime;
                timeProgress = Mathf.Clamp01(elapsed / MinimumVisibleSeconds) * MaxDisplayedProgressBeforeReady;

                float mappedReal = Mathf.Clamp01(realProgress / 0.9f) * MaxDisplayedProgressBeforeReady;
                float target = Mathf.Max(timeProgress, mappedReal);

                // Smooth step towards target
                displayedProgress = Mathf.MoveTowards(displayedProgress, target, Time.unscaledDeltaTime);
                _view.SetProgress(displayedProgress);

                bool minimumTimePassed = elapsed >= MinimumVisibleSeconds;
                if (minimumTimePassed && loadTask.IsCompleted)
                {
                    break;
                }

                await Task.Yield();
            }

            // Animate to 100%
            while (displayedProgress < 1f)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                displayedProgress = Mathf.MoveTowards(displayedProgress, 1f, Time.unscaledDeltaTime * 2f);
                _view.SetProgress(displayedProgress);
                await Task.Yield();
            }

            _view.Hide();
        }

        public async Task RunUntil(Task operation, float minimumSeconds)
        {
            CancelCurrent();
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            float minSeconds = Mathf.Max(minimumSeconds, MinimumVisibleSeconds);

            _view.SetProgress(0f);
            _view.Show();

            float displayedProgress = 0f;
            float timeProgress = 0f;
            float startTime = Time.realtimeSinceStartup;

            // Phase 1: purely time-based progress up to 95%
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                float elapsed = Time.realtimeSinceStartup - startTime;
                timeProgress = Mathf.Clamp01(elapsed / minSeconds) * MaxDisplayedProgressBeforeReady;
                float target = timeProgress; // ignore operation until minimum time

                displayedProgress = Mathf.MoveTowards(displayedProgress, target, Time.unscaledDeltaTime);
                _view.SetProgress(displayedProgress);

                bool minimumTimePassed = elapsed >= minSeconds;
                if (minimumTimePassed)
                {
                    break;
                }

                await Task.Yield();
            }

            // Phase 2: wait for operation completion holding at 95%
            while (operation != null && !operation.IsCompleted)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                displayedProgress = Mathf.MoveTowards(displayedProgress, MaxDisplayedProgressBeforeReady, Time.unscaledDeltaTime);
                _view.SetProgress(displayedProgress);
                await Task.Yield();
            }

            // Animate to 100%
            while (displayedProgress < 1f)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                displayedProgress = Mathf.MoveTowards(displayedProgress, 1f, Time.unscaledDeltaTime * 2f);
                _view.SetProgress(displayedProgress);
                await Task.Yield();
            }

            _view.Hide();
        }

        public async Task RunUntil(AsyncOperation operation, float minimumSeconds)
        {
            CancelCurrent();
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            float minSeconds = Mathf.Max(minimumSeconds, MinimumVisibleSeconds);

            _view.SetProgress(0f);
            _view.Show();

            float displayedProgress = 0f;
            float startTime = Time.realtimeSinceStartup;

            // Phase 1: purely time-based progress up to 95%
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                float elapsed = Time.realtimeSinceStartup - startTime;
                float target = Mathf.Clamp01(elapsed / minSeconds) * MaxDisplayedProgressBeforeReady;
                displayedProgress = Mathf.MoveTowards(displayedProgress, target, Time.unscaledDeltaTime);
                _view.SetProgress(displayedProgress);

                if (elapsed >= minSeconds)
                {
                    break;
                }

                await Task.Yield();
            }

            // Phase 2: show real progress in the last 5%
            while (operation != null && !operation.isDone)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                float mapped = Mathf.Clamp01((operation.progress) / 0.9f); // 0..1
                float target = MaxDisplayedProgressBeforeReady + mapped * (1f - MaxDisplayedProgressBeforeReady);
                displayedProgress = Mathf.MoveTowards(displayedProgress, target, Time.unscaledDeltaTime);
                _view.SetProgress(displayedProgress);
                await Task.Yield();
            }

            while (displayedProgress < 1f)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                displayedProgress = Mathf.MoveTowards(displayedProgress, 1f, Time.unscaledDeltaTime * 2f);
                _view.SetProgress(displayedProgress);
                await Task.Yield();
            }

            _view.Hide();
        }

        private void CancelCurrent()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }
    }
}


