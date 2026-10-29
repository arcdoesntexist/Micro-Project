using UnityEngine;

namespace CPS.KitchenCatch
{
    public sealed class BootRoute : MonoBehaviour
    {
        [SerializeField] private SceneNavigator navigator;
        private void Start() => navigator.GoTo("ModeSelect");
    }
}
