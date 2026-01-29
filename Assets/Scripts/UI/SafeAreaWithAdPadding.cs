using UnityEngine;
using Infrastructure;
using Infrastructure.Settings;
using Shared;
using WS.Core.SDK.AppLovin;

namespace Project.Runtime.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaWithAdPadding : MonoBehaviour
    {
        private const string AutoContainerName = "__SafeAreaRoot";

        [SerializeField] private Canvas _canvas;

        [Tooltip(
            "Какой RectTransform реально поджимать под safe-area и баннер.\n" +
            "Важно: НЕ рекомендуется указывать корневой Canvas (его RectTransform может быть перезаписан Unity).\n" +
            "Если поле пустое и скрипт висит на корневом Canvas — будет автоматически создан контейнер-потомок."
        )]
        [SerializeField] private RectTransform _targetRectTransform;

        [SerializeField] private bool _applyTop = true;
        [SerializeField] private bool _applyBottom = true;

        [SerializeField] private bool _useBannerPadding = true;
        [SerializeField] private float _bannerHeightDp = 50f;
        [Tooltip("Если > 0, используется как высота баннера в пикселях экрана и перекрывает расчёт по dp.")]
        [SerializeField] private float _bannerHeightPixelsOverride;

        private RectTransform _selfRectTransform;
        private RectTransform _appliedRectTransform;
        private Rect _lastSafeArea;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private float _lastCanvasScaleFactor;
        private float _lastBannerHeightDp;
        private float _lastBannerHeightPixelsOverride;
        private bool _lastIsBannerPaddingEnabled;
        private bool _isInitialized;

        private void Awake()
        {
            InitializeInternal();
            Apply();
        }

        private void OnEnable()
        {
            InitializeInternal();
            Apply();
        }

        private void Update()
        {
            if (IsReapplyRequiredInternal())
            {
                Apply();
            }
        }

        private void OnValidate()
        {
            // В редакторе не модифицируем иерархию автоматически.
            // Применение корректно отработает в Play Mode / билде.
            if (!Application.isPlaying)
            {
                return;
            }

            InitializeInternal();
            Apply();
        }

        private void InitializeInternal()
        {
            if (_isInitialized && _selfRectTransform != null && _appliedRectTransform != null)
            {
                return;
            }

            _selfRectTransform = GetComponent<RectTransform>();

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            if (_targetRectTransform == null)
            {
                _targetRectTransform = _selfRectTransform;
            }

            _appliedRectTransform = ResolveAppliedRectTransformInternal(_targetRectTransform);
            _isInitialized = true;
        }

        private RectTransform ResolveAppliedRectTransformInternal(RectTransform requestedTarget)
        {
            if (requestedTarget == null)
            {
                return null;
            }

            // Корневой Canvas (Screen Space) часто принудительно растягивается Unity и игнорирует ручные изменения.
            // Если скрипт повешен на него — создаём дочерний контейнер и применяем safe-area уже к нему.
            if (requestedTarget.GetComponent<Canvas>() != null && requestedTarget.transform.parent == null)
            {
                var container = requestedTarget.Find(AutoContainerName) as RectTransform;
                if (container == null)
                {
                    if (!Application.isPlaying)
                    {
                        return requestedTarget;
                    }

                    container = CreateAutoContainerInternal(requestedTarget);
                }

                return container;
            }

            return requestedTarget;
        }

        private static RectTransform CreateAutoContainerInternal(RectTransform canvasRectTransform)
        {
            var containerGameObject = new GameObject(AutoContainerName, typeof(RectTransform));
            var container = containerGameObject.GetComponent<RectTransform>();
            container.SetParent(canvasRectTransform, worldPositionStays: false);

            container.anchorMin = Vector2.zero;
            container.anchorMax = Vector2.one;
            container.offsetMin = Vector2.zero;
            container.offsetMax = Vector2.zero;
            container.localScale = Vector3.one;

            // Переносим все текущие children Canvas в контейнер (кроме контейнера),
            // СОХРАНЯЯ порядок sibling index (иначе `PopupRoot` может оказаться под панелями и попапы станут "невидимыми").
            int originalChildCount = canvasRectTransform.childCount;
            var childrenToMove = new Transform[Mathf.Max(0, originalChildCount - 1)];
            int moveIndex = 0;
            for (int i = 0; i < originalChildCount; i++)
            {
                Transform child = canvasRectTransform.GetChild(i);
                if (child == container)
                {
                    continue;
                }

                childrenToMove[moveIndex] = child;
                moveIndex++;
            }

            for (int i = 0; i < moveIndex; i++)
            {
                Transform child = childrenToMove[i];
                if (child == null)
                {
                    continue;
                }

                child.SetParent(container, worldPositionStays: false);
                child.SetSiblingIndex(i);
            }

            // Доп. страховка: если есть PopupRoot — держим его последним (самый верх по отрисовке).
            var popupRoot = container.Find("PopupRoot");
            if (popupRoot != null)
            {
                popupRoot.SetAsLastSibling();
            }

            return container;
        }

        private void Apply()
        {
            InitializeInternal();

            if (_appliedRectTransform == null)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            _lastSafeArea = safeArea;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            _lastBannerHeightDp = _bannerHeightDp;
            _lastBannerHeightPixelsOverride = _bannerHeightPixelsOverride;
            _lastCanvasScaleFactor = ResolveCanvasScaleFactorInternal();
            _lastIsBannerPaddingEnabled = IsBannerPaddingEnabledInternal();

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMax.x /= Screen.width;

            if (_applyBottom)
            {
                anchorMin.y /= Screen.height;
            }
            else
            {
                anchorMin.y = 0f;
            }

            if (_applyTop)
            {
                anchorMax.y /= Screen.height;
            }
            else
            {
                anchorMax.y = 1f;
            }

            _appliedRectTransform.anchorMin = anchorMin;
            _appliedRectTransform.anchorMax = anchorMax;
            _appliedRectTransform.offsetMin = Vector2.zero;
            _appliedRectTransform.offsetMax = Vector2.zero;

            if (_lastIsBannerPaddingEnabled)
            {
                ApplyBannerPadding();
            }
        }

        private void ApplyBannerPadding()
        {
            float bannerPixels;

            if (_bannerHeightPixelsOverride > 0f)
            {
                bannerPixels = _bannerHeightPixelsOverride;
            }
            else
            {
                float dpi = Screen.dpi;
                if (dpi <= 0f)
                {
                    dpi = 480f;
                }

                bannerPixels = _bannerHeightDp * (dpi / 160f);
            }

            float bottomOffsetInCanvas = bannerPixels / ResolveCanvasScaleFactorInternal();

            Vector2 offsetMin = _appliedRectTransform.offsetMin;
            offsetMin.y = bottomOffsetInCanvas;
            _appliedRectTransform.offsetMin = offsetMin;
        }

        private float ResolveCanvasScaleFactorInternal()
        {
            if (_canvas != null && _canvas.renderMode != RenderMode.WorldSpace)
            {
                return Mathf.Max(0.0001f, _canvas.scaleFactor);
            }

            return 1f;
        }

        private bool IsReapplyRequiredInternal()
        {
            if (!_isInitialized)
            {
                return true;
            }

            if (Screen.safeArea != _lastSafeArea)
            {
                return true;
            }

            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                return true;
            }

            if (!Mathf.Approximately(_lastBannerHeightDp, _bannerHeightDp) ||
                !Mathf.Approximately(_lastBannerHeightPixelsOverride, _bannerHeightPixelsOverride))
            {
                return true;
            }

            if (_lastIsBannerPaddingEnabled != IsBannerPaddingEnabledInternal())
            {
                return true;
            }

            float scaleFactor = ResolveCanvasScaleFactorInternal();
            if (!Mathf.Approximately(_lastCanvasScaleFactor, scaleFactor))
            {
                return true;
            }

            return false;
        }

        private bool IsBannerPaddingEnabledInternal()
        {
            if (!_useBannerPadding)
            {
                return false;
            }

            if (!Services.TryGet<IProjectSettingsService>(out var projectSettings) || projectSettings == null)
            {
                return false;
            }

            if (!Services.TryGet<IProgressService>(out var progressService) || progressService == null)
            {
                return false;
            }

            int currentLevelNumber = progressService.LastCompletedLevelIndex + 2;
            if (currentLevelNumber < projectSettings.BannerStartLevel)
            {
                return false;
            }

            if (!Services.TryGet<AppLovinMaxAdService>(out var adService) || adService == null)
            {
                return false;
            }

            return adService.IsBannerVisible;
        }
    }
}


