using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UI.Loading
{
    public interface ILoadingService
    {
        bool IsShowing { get; }
        void Show();
        void Hide();
        Task Run(Func<IProgress<float>, Task> loadOperation);
        Task RunUntil(Task operation, float minimumSeconds);
        Task RunUntil(AsyncOperation operation, float minimumSeconds);
    }
}
