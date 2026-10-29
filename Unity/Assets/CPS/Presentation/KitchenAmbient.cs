using UnityEngine;

namespace CPS.KitchenCatch
{
    public sealed class KitchenAmbient : MonoBehaviour
    {
        [SerializeField] private Transform ivy;
        [SerializeField] private Transform whisk;
        [SerializeField] private Transform ladle;
        [SerializeField] private ParticleSystem steam;
        private float seconds;

        private void OnEnable()
        {
            if (steam != null && !steam.isPlaying) steam.Play();
        }

        private void Update() => StepAmbient(Time.unscaledDeltaTime);

        public void StepAmbient(float elapsed)
        {
            if (!float.IsFinite(elapsed) || elapsed < 0) return;
            seconds += elapsed;
            if (ivy != null) ivy.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(seconds * 1.2f) * 2.5f);
            if (whisk != null) whisk.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(seconds * .9f + .8f) * 2f);
            if (ladle != null) ladle.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(seconds * .8f + 1.7f) * 1.5f);
        }
    }
}
