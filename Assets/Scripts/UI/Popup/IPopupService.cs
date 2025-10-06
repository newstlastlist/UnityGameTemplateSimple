using System;
using System.Threading.Tasks;
using Infrastructure.Resources;

namespace UI.Popup
{
    public interface IPopupService
    {
        Task<BasePopupView> Show(PopupId popupId);
        Task<T> Show<T>(PopupId popupId) where T : BasePopupView;
        void CloseCurrent();
        bool HasActivePopup { get; }
    }
}
