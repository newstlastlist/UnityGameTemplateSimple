using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UI.Tutor
{
    // Базовый, переносимый сервис тутора без проектно-специфичных зависимостей
    public class TutorService : ITutorService, ITutorContext
    {
        private readonly TutorView _view;
        private CancellationTokenSource _cts;
        private bool _isRunning;
        private IReadOnlyList<ITutorScenario> _scenarios;

        public TutorService(TutorView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public bool IsRunning => _isRunning;

        protected virtual IReadOnlyList<ITutorScenario> CreateScenarios()
        {
            return Array.Empty<ITutorScenario>();
        }

        public bool HasScenarioForLevel(int levelIndex)
        {
            return TryGetScenarioForLevelInternal(levelIndex) != null;
        }

        public async Task StartForLevelAsync(int levelId, CancellationToken externalToken = default)
        {
            Stop();
            var scenario = TryGetScenarioForLevelInternal(levelId);
            if (scenario == null)
            {
                // предупреждение оставим, чтобы понимать, почему не стартует
                DebugLogger.Log($"[Tutor] Сценарий для уровня {levelId} не найден — пропуск");
                return;
            }

            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            _isRunning = true;
            try
            {
                await scenario.RunAsync(this, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            finally
            {
                _isRunning = false;
                _cts.Dispose();
                _cts = null;
                HideFinger();
            }
        }

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
            }
        }

        // ITutorContext
        public void ShowFinger()
        {
            _view.SetFingerVisible(true);
        }

        public void HideFinger()
        {
            _view.SetFingerVisible(false);
            if (_view.FingerController != null)
            {
                _view.FingerController.StopAll();
            }
        }

        public async Task MoveFingerToAsync(Transform targetTransform, bool loopMovement, CancellationToken ct)
        {
            if (_view.FingerController == null || targetTransform == null)
            {
                DebugLogger.LogWarning("[Tutor] Не могу переместить палец: нет FingerController или целевого Transform");
                return;
            }
            _view.FingerController.MoveTo(targetTransform, loopMovement);
            await Task.Yield();
        }

        public async Task PulseFingerAsync(bool loopPulse, CancellationToken ct)
        {
            if (_view.FingerController == null)
            {
                DebugLogger.LogWarning("[Tutor] Не могу запустить пульс пальца: нет FingerController");
                return;
            }
            _view.FingerController.Pulse(loopPulse);
            await Task.Yield();
        }

        public void ShowHint(HintType hintType, Action onClosed = null)
        {
            var go = _view.GetHintGO(hintType);
            if (go == null)
            {
                DebugLogger.LogWarning($"[Tutor] Хинт {hintType} не найден во вью");
                return;
            }
            var handler = go.GetComponent<TutorHintClickHandler>() ?? go.AddComponent<TutorHintClickHandler>();
            handler.Initialize(onClosed);
            go.SetActive(true);
        }

        public void HideHint(HintType hintType)
        {
            _view.SetHintActive(hintType, false);
        }

        public async Task WaitClickOnTutorObjectAsync(int tutorObjectId, CancellationToken ct)
        {
            var target = ResolveTransformByTutorId(tutorObjectId);
            if (target == null)
            {
                DebugLogger.LogWarning($"[Tutor] Не найден объект с TutorObjectIdentificator.objectId={tutorObjectId}");
                return;
            }
            // используем универсальный клик-хэндлер без авто-деактивации
            var relay = target.GetComponent<TutorHintClickHandler>() ?? target.gameObject.AddComponent<TutorHintClickHandler>();
            var tcs = new TaskCompletionSource<bool>();
            relay.Initialize(() =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.SetResult(true);
                }
            }, deactivateOnClick: false);

            using (ct.Register(() =>
                   {
                       if (!tcs.Task.IsCompleted) tcs.TrySetCanceled();
                   }))
            {
                await tcs.Task;
            }

            // автоматически не удаляем компонент, чтобы не дергать дом дерево под анимациями
        }

        public Transform ResolveTransformByTutorId(int tutorObjectId)
        {
            var all = UnityEngine.Object.FindObjectsOfType<TutorObjectIdentificator>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].objectId == tutorObjectId)
                {
                    return all[i].transform;
                }
            }
            DebugLogger.LogWarning($"[Tutor] Цель для id={tutorObjectId} не найдена");
            return null;
        }

        private ITutorScenario TryGetScenarioForLevelInternal(int levelIndex)
        {
            if (_scenarios == null)
            {
                _scenarios = CreateScenarios() ?? Array.Empty<ITutorScenario>();
            }

            for (int i = 0; i < _scenarios.Count; i++)
            {
                var scenario = _scenarios[i];
                if (scenario == null)
                {
                    continue;
                }

                if (scenario.TargetLevelIndex == levelIndex)
                {
                    return scenario;
                }
            }

            return null;
        }
    }
}


