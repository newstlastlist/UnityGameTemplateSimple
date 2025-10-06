using System;

namespace App
{
    public sealed class ScreenNavigatorService : IScreenNavigator
    {
        private readonly ScreenController _controller;

        public ScreenNavigatorService(ScreenController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public void Show(ScreenId id)
        {
            _controller.Show(id);
        }
    }
}