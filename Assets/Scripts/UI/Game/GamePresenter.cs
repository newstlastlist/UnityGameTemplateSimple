using System;
using App;
using Infrastructure;
using Shared;

namespace UI.Game
{
     public sealed class GamePresenter
    {
        private readonly GameView _view;
        private readonly IScreenNavigator _screenNavigator;
        private readonly IProgressService _progressService;
        private readonly ILevelRepository _levelRepository;

         public GamePresenter(GameView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _levelRepository = Services.Get<ILevelRepository>();
            _progressService = Services.Get<IProgressService>();
            _screenNavigator = Services.Get<IScreenNavigator>();
        }

        public void Open()
        {

        }

        public void Close()
        {
            
        }
    }
}