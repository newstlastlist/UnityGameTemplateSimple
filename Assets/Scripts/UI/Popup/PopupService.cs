using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Resources;
using Shared;
using UnityEngine;

namespace UI.Popup
{
    /*
     * Как добавить новый попап:
    1) Префаб
        Создайте префаб под Assets/Resources/..., например: Assets/Resources/UI/Popups/ConfirmPopup.prefab.
        На корне префаба должен быть компонент‑вью, унаследованный от BasePopupView (например, ConfirmPopupView), в нём реализуйте Init() для биндинга кнопок и локальной инициализации.
    2) Идентификатор
        Добавьте значение в PopupId, например Confirm.
    3) Контроллер
        На сцене откройте PopupController.
        В список _popups добавьте элемент: Id = PopupId.Confirm, Prefab = ваш PrefabFakeReference (перетащите префаб — фейковая ссылка сохранит путь/Guid, в рантайме грузится через Resources).
        В PopupController.CreatePresenter добавьте кейс:
        case PopupId.Confirm: return new ConfirmPopupPresenter((ConfirmPopupView)view);
    4) Презентор
        Создайте презентор (класс с IDisposable), который принимает конкретную вью: подписывается на её события, дергает доменные сервисы. В Dispose() снимает подписки.
        
    Как показывать попап:
    Из любого презентора/класса приложения:
    Если пользователь нажимает кнопку закрытия — BasePopupView вызовет OnClosed, PopupService сам уничтожит вью, вызовет Dispose() презентора и перейдёт к следующему попапу в очереди.
        */
    public sealed class PopupService : IPopupService
    {
        private readonly IPopupController _controller;
        private readonly IResourceService _resourceService;

        private readonly Queue<Func<Task>> _queue = new Queue<Func<Task>>();
        private bool _isShowing;
        private BasePopupView _currentInstance;
        private IDisposable _currentPresenter;

        public PopupService(IPopupController controller)
        {
            _controller = controller;
            _resourceService = Services.Get<IResourceService>();
        }

        public bool HasActivePopup => _currentInstance != null;

        public Task<BasePopupView> Show(PopupId popupId)
        {
            var tcs = new TaskCompletionSource<BasePopupView>();

            async Task TaskBody()
            {
                try
                {
                    if (!_controller.TryGetReference(popupId, out var prefabReference))
                    {
                        tcs.SetException(new InvalidOperationException($"PopupId '{popupId}' is not mapped in PopupController"));
                        return;
                    }

                    string path = prefabReference.GetResourcesRelativePath();
                    var prefab = await _resourceService.LoadGameObjectAsync(path);
                    if (prefab == null)
                    {
                        tcs.SetException(new InvalidOperationException($"Popup prefab not found at '{path}'"));
                        return;
                    }

                    var instanceGo = UnityEngine.Object.Instantiate(prefab, _controller.PopupRoot);
                    _currentInstance = instanceGo.GetComponent<BasePopupView>();
                    if (_currentInstance == null)
                    {
                        UnityEngine.Object.Destroy(instanceGo);
                        tcs.SetException(new InvalidOperationException($"Popup instance does not contain BasePopupView"));
                        return;
                    }

                    void ClosedHandler()
                    {
                        _currentInstance.OnClosed -= ClosedHandler;
                        if (_currentPresenter != null)
                        {
                            _currentPresenter.Dispose();
                            _currentPresenter = null;
                        }
                        UnityEngine.Object.Destroy(_currentInstance.gameObject);
                        _currentInstance = null;
                        _isShowing = false;
                        TryRunNext();
                    }

                    _currentInstance.OnClosed += ClosedHandler;
                    _currentPresenter = _controller.CreatePresenter(popupId, _currentInstance);
                    tcs.SetResult(_currentInstance);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }

            _queue.Enqueue(TaskBody);
            TryRunNext();

            return tcs.Task;
        }

        public async Task<T> Show<T>(PopupId popupId) where T : BasePopupView
        {
            var view = await Show(popupId);
            return view as T;
        }

        public void CloseCurrent()
        {
            if (_currentInstance != null)
            {
                _currentInstance.Close();
            }
        }

        private async void TryRunNext()
        {
            if (_isShowing)
            {
                return;
            }

            if (_queue.Count == 0)
            {
                return;
            }

            _isShowing = true;
            var next = _queue.Dequeue();
            await next();
        }
    }
}
