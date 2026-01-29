using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UI.Tutor
{
    public interface ITutorScenario
    {
        int TargetLevelIndex { get; }
        Task RunAsync(ITutorContext context, CancellationToken cancellationToken);
    }

    public interface ITutorService
    {
        bool IsRunning { get; }
        bool HasScenarioForLevel(int levelIndex);
        Task StartForLevelAsync(int levelId, CancellationToken externalToken = default);
        void Stop();
    }

    public interface ITutorContext
    {
        // Finger
        void ShowFinger();
        void HideFinger();
        Task MoveFingerToAsync(Transform targetTransform, bool loopMovement, CancellationToken ct);
        Task PulseFingerAsync(bool loopPulse, CancellationToken ct);

        // Hints
        void ShowHint(HintType hintType, Action onClosed = null);
        void HideHint(HintType hintType);

        // Waiting for interactions
        Task WaitClickOnTutorObjectAsync(int tutorObjectId, CancellationToken ct);

        // Resolving targets
        Transform ResolveTransformByTutorId(int tutorObjectId);
    }

    // Проектно-специфичный интерфейс управления вводом (для этого проекта — блокировка столбцов)
    public interface ITutorProjectContext : ITutorContext
    {
        void BlockAllColumnsExceptByTutorIds(int[] allowedTutorObjectIds);
        void UnblockAllColumns();
    }
}


