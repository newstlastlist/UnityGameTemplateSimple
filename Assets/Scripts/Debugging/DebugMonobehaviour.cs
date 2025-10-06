using Shared;
using Sirenix.OdinInspector;
using UI.Popup;
using UnityEngine;

namespace Debugging
{
    public class DebugMonobehaviour : MonoBehaviour
    {
        [Button]
        public void TestPopup()
        {
            var popupService = Services.Get<IPopupService>();
            popupService.Show(PopupId.PrivacySettings);
        }
    }
}