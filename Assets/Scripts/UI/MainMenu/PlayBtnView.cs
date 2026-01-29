using Domain;
using UnityEngine;

namespace UI.MainMenu
{
    public class PlayBtnView : MonoBehaviour
    {
        [SerializeField] private GameObject _hardLevelFrame;
        [SerializeField] private GameObject _bonusLevelFrame;

        public void ChangeFrame(LevelType levelType)
        {
            switch (levelType)
            {
                case LevelType.Hard:
                    _hardLevelFrame.gameObject.SetActive(true);
                    _bonusLevelFrame.gameObject.SetActive(false);
                    break;
                case LevelType.Bonus:
                    _hardLevelFrame.gameObject.SetActive(false);
                    _bonusLevelFrame.gameObject.SetActive(true);
                    break;
                case LevelType.Default:
                    _hardLevelFrame.gameObject.SetActive(false);
                    _bonusLevelFrame.gameObject.SetActive(false);
                    break;
            }
        }
    }
}