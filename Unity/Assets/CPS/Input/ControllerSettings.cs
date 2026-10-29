using UnityEngine;
namespace CPS.KitchenCatch
{
    [CreateAssetMenu(menuName = "CPS/Controller Settings")]
    public sealed class ControllerSettings : ScriptableObject
    {
        public float fsrLeftPress = 20;
        public float fsrLeftRelease = 15;
        public float fsrRightPress = 20;
        public float fsrRightRelease = 15;
        public float loadLeftPressN = .5f;
        public float loadLeftReleaseN = .3f;
        public float loadRightPressN = .5f;
        public float loadRightReleaseN = .3f;
        public float staleSeconds = .75f;
        public string ValidationError
        {
            get
            {
                if (!Pair(fsrLeftPress, fsrLeftRelease, 100) || !Pair(fsrRightPress, fsrRightRelease, 100) ||
                    !Pair(loadLeftPressN, loadLeftReleaseN, float.MaxValue) || !Pair(loadRightPressN, loadRightReleaseN, float.MaxValue))
                    return "Controller threshold settings are invalid: release must be below press.";
                if (!Finite(staleSeconds) || staleSeconds <= 0 || staleSeconds > .75f) return "USB timeout must be greater than 0 and at most 0.75 seconds.";
                return null;
            }
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Pair(float press, float release, float maximum) => Finite(press) && Finite(release) && release >= 0 && press > release && press <= maximum;
    }
}
