using UnityEngine.SceneManagement;

namespace Infrastructure.SceneManagement
{
    public class SceneLoader
    {
        private readonly SceneLoaderConfig _config;

        public int MainSceneIndex => _config.MainSceneIndex;
        public int MenuSceneIndex => _config.MenuSceneIndex;

        public SceneLoader(SceneLoaderConfig config)
        {
            _config = config;
        }
		
        public void LoadMainScene()
        {
            SceneManager.LoadScene(_config.MainSceneIndex);
        }

        public void LoadMenuScene()
        {
            SceneManager.LoadScene(_config.MenuSceneIndex);
        }
    }
}