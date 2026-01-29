using System;
using System.Collections.Generic;
using UnityEngine;
using UI;

namespace App
{
    public sealed class ScreenController : MonoBehaviour
    {
        [SerializeField] private List<PanelIdContainer> _panels = new List<PanelIdContainer>();

        private readonly Dictionary<PanelType, PanelBase> _panelsById = new Dictionary<PanelType, PanelBase>();

        private readonly Dictionary<PanelType, Dictionary<Type, Component>> _viewCacheByScreen =
            new Dictionary<PanelType, Dictionary<Type, Component>>();

        public event Action<PanelType> OnScreenShown;

        private void Awake()
        {
            FillPanelsDict();
        }

        public void Show(PanelType id)
        {
            if (_panelsById.Count == 0)
            {
                FillPanelsDict();
            }

            // Settings — оверлей: не выключаем текущий экран (Main/Game/Win), просто показываем Settings поверх.
            if (id == PanelType.Settings)
            {
                SetActiveForPanel(PanelType.Settings, true);
                OnScreenShown?.Invoke(id);
                return;
            }

            foreach (var kv in _panelsById)
            {
                SetActiveForPanel(kv.Key, kv.Key.Equals(id));
            }

            OnScreenShown?.Invoke(id);
        }

        private void SetActiveForPanel(PanelType id, bool isActive)
        {
            if (_panelsById.TryGetValue(id, out PanelBase panel) && panel != null)
            {
                panel.Show(isActive);
                if (isActive)
                {
                    panel.OnOpenHandler();
                }
            }
        }

        public T GetViewOnPanel<T>(PanelType id) where T : Component
        {
            if (_panelsById.Count == 0)
            {
                FillPanelsDict();
            }

            if (_viewCacheByScreen.TryGetValue(id, out Dictionary<Type, Component> typeMap))
            {
                if (typeMap.TryGetValue(typeof(T), out Component cached))
                {
                    return cached as T;
                }
            }
            else
            {
                typeMap = new Dictionary<Type, Component>();
                _viewCacheByScreen[id] = typeMap;
            }

            if (!_panelsById.TryGetValue(id, out PanelBase panel) || panel == null)
            {
                return null;
            }

            T found = panel.GetComponentInChildren<T>(true);
            if (found != null)
            {
                typeMap[typeof(T)] = found;
                return found;
            }

            return null;
        }

        public void InvalidateViewCache()
        {
            _viewCacheByScreen.Clear();
        }

        public void InvalidateViewCache(PanelType id)
        {
            _viewCacheByScreen.Remove(id);
        }

        private void FillPanelsDict()
        {
            _panelsById.Clear();

            for (int i = 0; i < _panels.Count; i++)
            {
                var c = _panels[i];
                if (c != null && c.Panel != null)
                {
                    _panelsById[c.Type] = c.Panel;
                }
            }
        }
    }

    [Serializable]
    public sealed class PanelIdContainer
    {
        public PanelType Type;
        public PanelBase Panel;
    }

    public enum PanelType
    {
        Main,
        Game,
        Win,
        Settings
    }


}