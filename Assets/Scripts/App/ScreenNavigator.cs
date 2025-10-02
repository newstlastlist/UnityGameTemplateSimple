using System;

namespace App
{
    public sealed class ScreenNavigator : IScreenNavigator
    {
        private readonly ScreenController _controller;

        public ScreenNavigator(ScreenController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public void Show(ScreenId id)
        {
            _controller.Show(id);
        }
    }
}