using UnityEngine;
using UnityEngine.SceneManagement;

namespace CPS.KitchenCatch
{
    public sealed class SceneNavigator : MonoBehaviour
    {
        public bool IsTransitioning { get; private set; }
        public bool GoTo(string sceneName)
        {
            if (IsTransitioning || string.IsNullOrWhiteSpace(sceneName) ||
                sceneName == SceneManager.GetActiveScene().name || !Application.CanStreamedLevelBeLoaded(sceneName))
                return false;
            IsTransitioning = true;
            var operation = SceneManager.LoadSceneAsync(sceneName);
            operation.completed += _ => { if (this != null) IsTransitioning = false; };
            return true;
        }
    }
}
