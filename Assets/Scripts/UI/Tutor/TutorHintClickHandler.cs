using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Tutor
{
    public class TutorHintClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private Action _onClicked;
        private bool _deactivateOnClick;

        public void Initialize(Action onClicked, bool deactivateOnClick = true)
        {
            _onClicked = onClicked;
            _deactivateOnClick = deactivateOnClick;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onClicked?.Invoke();

            if (_deactivateOnClick)
            {
                gameObject.SetActive(false);
            }
        }
    }
}