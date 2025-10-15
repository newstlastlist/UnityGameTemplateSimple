using System;
using System.Threading.Tasks;
using Infrastructure.Resources;
using UnityEngine;

namespace UI.Loading
{
    // LoadingService — системный сервис показа загрузочного экрана.
    // Как работает (две фазы):
        // 1) При первом обращении лениво инстанцирует префаб из Resources и размещает под переданный UI-родитель (или в корне).
        // 2) Фаза 1: по вызову Run/RunUntil первые N секунд прогресс растёт только от таймера до 95%.
        // 3) Фаза 2: если операция к этому моменту не завершена — экран удерживается на 95% (Task) либо отображаются последние 5% по реальному прогрессу (AsyncOperation 0.9→1.0 маппится в 95→100).
        // 4) После выполнения условий (минимум времени + окончание операции) прогресс анимируется до 100% и экран скрывается.
    // Использование:
        // - Run(Func<IProgress<float>, Task>) — для операций с ручным прогрессом (0..1), минимум времени встроен.
        // - RunUntil(Task operation, float minSeconds) — минимум minSeconds; если operation не завершена — экран держится на 95%.
        // - RunUntil(AsyncOperation op, float minSeconds) — минимум minSeconds; последние 5% отображают реальный прогресс op.
        // - Show()/Hide() — ручное управление при необходимости.
    public sealed class LoadingService : ILoadingService
    {
        private readonly IResourceService _resources;
        private readonly string _resourcesPath;

        private LoadingScreenView _view;
        private LoadingScreenPresenter _presenter;
        private Transform _uiRoot;

        public bool IsShowing => _view != null && _view.IsVisible;

        public LoadingService(IResourceService resources, string resourcesPath, Transform uiRoot)
        {
            _resources = resources;
            _resourcesPath = resourcesPath;
            _uiRoot = uiRoot;
        }

        public void Show()
        {
            EnsureView();
            _view.Show();
        }

        public void Hide()
        {
            if (_view != null)
            {
                _view.Hide();
            }
        }

        public async Task Run(Func<IProgress<float>, Task> loadOperation)
        {
            EnsureView();
            await _presenter.Run(loadOperation);
        }

        public async Task RunUntil(Task operation, float minimumSeconds)
        {
            EnsureView();
            await _presenter.RunUntil(operation, minimumSeconds);
        }

        public async Task RunUntil(AsyncOperation operation, float minimumSeconds)
        {
            EnsureView();
            await _presenter.RunUntil(operation, minimumSeconds);
        }

        private void EnsureView()
        {
            if (_view != null)
            {
                return;
            }

            var prefabComponent = _resources.LoadPrefab<LoadingScreenView>(_resourcesPath);
            if (prefabComponent == null)
            {
                Debug.LogError($"LoadingScreen prefab not found at Resources path: {_resourcesPath}");
                return;
            }

            GameObject instance = _uiRoot != null
                ? UnityEngine.Object.Instantiate(prefabComponent.gameObject, _uiRoot)
                : UnityEngine.Object.Instantiate(prefabComponent.gameObject);

            _view = instance.GetComponent<LoadingScreenView>();
            _presenter = new LoadingScreenPresenter(_view);
        }
    }
}


