using System;
using System.Collections.Generic;
using UnityEngine;

namespace App
{
    public sealed class ScreenController : MonoBehaviour
    {
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _gamePanel;
        [SerializeField] private GameObject _winPanel;

        private readonly Dictionary<ScreenId, GameObject> _panelsById = new Dictionary<ScreenId, GameObject>();

        private readonly Dictionary<ScreenId, Dictionary<Type, Component>> _viewCacheByScreen =
            new Dictionary<ScreenId, Dictionary<Type, Component>>();

        public event Action<ScreenId> OnScreenShown;

        private void Awake()
        {
            FillPanelsDict();
        }

        public void Show(ScreenId id)
        {
            if (_panelsById.Count == 0)
            {
                FillPanelsDict();
            }

            SetActiveForPanel(ScreenId.Main, id == ScreenId.Main);
            SetActiveForPanel(ScreenId.Game, id == ScreenId.Game);
            SetActiveForPanel(ScreenId.Win,  id == ScreenId.Win);

            OnScreenShown?.Invoke(id);
        }

        private void SetActiveForPanel(ScreenId id, bool isActive)
        {
            if (_panelsById.TryGetValue(id, out GameObject panel) && panel != null)
            {
                panel.SetActive(isActive);
            }
        }

        public T GetViewOnPanel<T>(ScreenId id) where T : Component
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

            if (!_panelsById.TryGetValue(id, out GameObject panel) || panel == null)
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

        public void InvalidateViewCache(ScreenId id)
        {
            _viewCacheByScreen.Remove(id);
        }

        private void FillPanelsDict()
        {
            _panelsById.Clear();

            if (_mainMenuPanel != null)
            {
                _panelsById[ScreenId.Main] = _mainMenuPanel;
            }

            if (_gamePanel != null)
            {
                _panelsById[ScreenId.Game] = _gamePanel;
            }

            if (_winPanel != null)
            {
                _panelsById[ScreenId.Win] = _winPanel;
            }
        }
    }

    public enum ScreenId
    {
        Main,
        Game,
        Win
    }

}