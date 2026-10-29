using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace CPS.KitchenCatch
{
    public sealed class MascotPresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer hat;
        [SerializeField] private Sprite rightFacingHat;
        [SerializeField] private Sprite leftFacingHat;
        [SerializeField] private Animator animator;
        [SerializeField] private FoodBurst burstPrefab;
        [SerializeField] private Sprite[] foodSprites;
        [SerializeField] private ParticleSystem dissolve;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip eatClip;
        [SerializeField] private AudioClip hurtClip;
        [SerializeField] private AudioClip triumphClip;
        [SerializeField] private AudioClip deathClip;
        private readonly List<FoodBurst> bursts = new List<FoodBurst>(4);
        private ObjectPool<FoodBurst> burstPool;
        private float terminalSeconds;
        private float reactionSeconds;
        private int movement;
        private bool terminal;
        private bool dying;
        private bool burstStarted;
        private bool dissolveStarted;
        private Vector3 originalScale;
        private Quaternion originalRotation;
        private Vector3 originalPosition;
        private string clipState;
        public static void NotifyUserInteraction() => LocalDemoAudio.NotifyUserInteraction();

        private void Awake()
        {
            originalScale = transform.localScale;
            originalRotation = transform.localRotation;
            originalPosition = transform.localPosition;
            if (eatClip == null) eatClip = LocalDemoAudio.LoadClip("Chomping");
            if (triumphClip == null) triumphClip = LocalDemoAudio.LoadClip("Triumph");
            if (deathClip == null) deathClip = LocalDemoAudio.LoadClip("Death");
            LocalDemoAudio.ConfigureSfxSource(audioSource);
        }

        private void Update()
        {
            StepPresentation(Time.unscaledDeltaTime);
        }

        public void SetMovement(int direction)
        {
            if (terminal) return;
            movement = direction < 0 ? -1 : direction > 0 ? 1 : 0;
            if (body != null) body.flipX = movement < 0;
            ApplyHatFacing();
            if (reactionSeconds <= 0) PlayAnimation(movement == 0 ? "Idle" : "Run");
            LocalDemoAudio.SetMascotRunning(movement != 0);
        }

        private void LateUpdate()
        {
            ApplyHatFacing();
        }

        private void ApplyHatFacing()
        {
            if (hat == null) return;
            bool facesLeft = movement < 0;
            hat.flipX = false;
            Sprite facingSprite = facesLeft ? leftFacingHat : rightFacingHat;
            if (facingSprite != null) hat.sprite = facingSprite;
            Vector3 position = hat.transform.localPosition;
            position.x = Mathf.Abs(position.x) * (facesLeft ? 1f : -1f);
            hat.transform.localPosition = position;
        }

        public void PlayEat() => React("Eat", eatClip);
        public void PlayHurt() => React("Hurt", hurtClip);

        private void React(string state, AudioClip clip)
        {
            if (terminal) return;
            reactionSeconds = .26f;
            PlayAnimation(state);
            PlaySound(clip);
        }

        public void PlayTerminal(EndReason reason)
        {
            if (terminal) return;
            terminal = true;
            dying = reason == EndReason.HpDepleted;
            terminalSeconds = 0;
            burstStarted = false;
            dissolveStarted = false;
            reactionSeconds = 0;
            LocalDemoAudio.SetMascotRunning(false);
            PlayAnimation(dying ? "Hurt" : "Triumph");
            PlaySound(dying ? deathClip : triumphClip);
        }

        public void StepPresentation(float seconds)
        {
            if (!float.IsFinite(seconds) || seconds <= 0) return;
            if (!terminal)
            {
                if (reactionSeconds > 0)
                {
                    reactionSeconds = Mathf.Max(0, reactionSeconds - seconds);
                    if (reactionSeconds == 0) PlayAnimation(movement == 0 ? "Idle" : "Run");
                }
                return;
            }
            terminalSeconds += seconds;
            if (dying) StepDeath(seconds);
            else
            {
                float lift = terminalSeconds < .6f ? Mathf.Sin(Mathf.PI * terminalSeconds / .6f) * .55f : 0;
                transform.localPosition = originalPosition + Vector3.up * lift;
            }
        }

        private void StepDeath(float seconds)
        {
            if (body != null && hat != null)
            {
                Color tint = terminalSeconds < .12f ? new Color(1f, .32f, .25f, 1f) : Color.white;
                float fade = terminalSeconds < .72f ? 1 : Mathf.Clamp01((1.4f - terminalSeconds) / .68f);
                tint.a = fade;
                body.color = tint;
                hat.color = new Color(1, 1, 1, fade);
            }
            if (terminalSeconds >= .12f && terminalSeconds < .4f)
            {
                float fall = Mathf.InverseLerp(.12f, .4f, terminalSeconds);
                transform.localRotation = Quaternion.Euler(0, 0, -35f * fall);
                transform.localScale = Vector3.Scale(originalScale, new Vector3(1 + .12f * fall, 1 - .18f * fall, 1));
            }
            if (terminalSeconds >= .4f && clipState != "Dead")
            {
                transform.localRotation = originalRotation;
                transform.localScale = originalScale;
                PlayAnimation("Dead");
            }
            if (!burstStarted && terminalSeconds >= .52f)
            {
                burstStarted = true;
                SpawnBurst();
            }
            for (int i = bursts.Count - 1; i >= 0; i--)
            {
                bursts[i].Step(seconds);
                if (terminalSeconds < .95f) continue;
                burstPool.Release(bursts[i]);
                bursts.RemoveAt(i);
            }
            if (!dissolveStarted && terminalSeconds >= .72f)
            {
                dissolveStarted = true;
                if (dissolve != null) dissolve.Play();
            }
            if (terminalSeconds >= 1.4f)
            {
                if (body != null) body.enabled = false;
                if (hat != null) hat.enabled = false;
            }
        }

        private void SpawnBurst()
        {
            if (burstPrefab == null) return;
            burstPool ??= new ObjectPool<FoodBurst>(() => Instantiate(burstPrefab, transform), null,
                item => item.Clear(), item => Destroy(item.gameObject), true, 4, 4);
            for (int i = 0; i < 4; i++)
            {
                var item = burstPool.Get();
                Sprite sprite = foodSprites != null && foodSprites.Length > 0 ? foodSprites[i % foodSprites.Length] : null;
                item.Launch(sprite, transform.position + Vector3.up * .5f,
                    new Vector2((i - 1.5f) * 2.1f, i % 2 == 0 ? 2.8f : 3.5f));
                bursts.Add(item);
            }
        }

        public void ResetPresentation()
        {
            terminal = false;
            dying = false;
            movement = 0;
            terminalSeconds = reactionSeconds = 0;
            burstStarted = dissolveStarted = false;
            LocalDemoAudio.SetMascotRunning(false);
            if (audioSource != null) audioSource.Stop();
            foreach (var item in bursts) burstPool.Release(item);
            bursts.Clear();
            if (dissolve != null) dissolve.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            transform.localScale = originalScale;
            if (body != null) { body.color = Color.white; body.enabled = true; body.flipX = false; }
            if (hat != null) { hat.color = Color.white; hat.enabled = true; hat.flipX = false; }
            ApplyHatFacing();
            PlayAnimation("Idle");
        }

        private void PlayAnimation(string state)
        {
            if (clipState == state) return;
            clipState = state;
            if (animator != null) animator.Play(state, 0, 0);
        }

        private void PlaySound(AudioClip clip)
        {
            if (LocalDemoAudio.AudioUnlocked && audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }

        private void OnDisable() => LocalDemoAudio.SetMascotRunning(false);

        private void OnDestroy()
        {
            LocalDemoAudio.SetMascotRunning(false);
            bursts.Clear();
            burstPool?.Clear();
        }
    }
}
