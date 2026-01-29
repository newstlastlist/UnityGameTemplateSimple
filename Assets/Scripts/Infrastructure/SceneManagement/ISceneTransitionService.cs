using Cysharp.Threading.Tasks;

namespace Infrastructure.SceneManagement
{
    public interface ISceneTransitionService
    {
        UniTask LoadMainWithSplashAsync ();
        UniTask LoadMenuWithSplashAsync ();
    }
    
}