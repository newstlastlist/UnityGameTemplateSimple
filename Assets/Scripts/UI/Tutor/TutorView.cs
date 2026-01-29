using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Tutor
{
    public class TutorView : MonoBehaviour
    {
        [SerializeField] private GameObject _tutorFinger;
        [SerializeField] private TutorFingerController _fingerController;
        [SerializeField] private List<HintTypeObjectPair> _hintTypeObjectPairs;

        private void Awake()
        {
            if (_tutorFinger != null)
            {
                _tutorFinger.gameObject.SetActive(false);
            }

            foreach (var hintTypeObjectPair in _hintTypeObjectPairs)
            {
                hintTypeObjectPair.hint.gameObject.SetActive(false);
            }
        }

        public void SetFingerVisible(bool visible)
        {
            if (_fingerController != null)
            {
                _fingerController.SetVisible(visible);
                return;
            }

            if (_tutorFinger != null)
            {
                _tutorFinger.SetActive(visible);
            }
        }

        public TutorFingerController FingerController => _fingerController;

        public GameObject GetHintGO(HintType type)
        {
            if (_hintTypeObjectPairs == null)
            {
                return null;
            }

            for (int i = 0; i < _hintTypeObjectPairs.Count; i++)
            {
                if (_hintTypeObjectPairs[i] != null && _hintTypeObjectPairs[i].hintType == type)
                {
                    return _hintTypeObjectPairs[i].hint;
                }
            }

            return null;
        }

        public void SetHintActive(HintType type, bool active)
        {
            var go = GetHintGO(type);
            if (go != null)
            {
                go.SetActive(active);
            }
        }
    }

    [Serializable]
    public class HintTypeObjectPair
    {
        public HintType hintType;
        public GameObject hint;
    }

    public enum HintType
    {
        BoxColumnHint,
        BoosterColumnHint,
        CategoryTips1,
        CategoryTips2
    }
}