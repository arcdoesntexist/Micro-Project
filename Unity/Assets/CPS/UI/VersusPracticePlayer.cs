using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CPS.KitchenCatch
{
    public sealed class VersusPracticePlayer : MonoBehaviour
    {
        [SerializeField] private VersusKeyboardInput input;
        [SerializeField] private RectTransform mascot;
        [SerializeField] private Image body;
        [SerializeField] private Image hat;
        [SerializeField] private Image shoeSwatch;
        [SerializeField] private Sprite idle;
        [SerializeField] private Sprite[] runFrames;
        [SerializeField] private Sprite rightHat;
        [SerializeField] private Sprite leftHat;
        [SerializeField] private HatVariant[] hatVariants = Array.Empty<HatVariant>();
        [SerializeField] private float leftLimit = -250f;
        [SerializeField] private float rightLimit = 250f;
        [SerializeField] private float speed = 360f;
        private float animationTime;
        private Sprite selectedRightHat;
        private Sprite selectedLeftHat;
        private bool facesRight = true;
        private string shoeColorHex = "#F57C32";

        [Serializable]
        public struct HatVariant
        {
            public Color Color;
            public Sprite Right;
            public Sprite Left;
        }

        public event Action<bool> ReadyChanged;
        public bool IsReady { get; private set; }
        public float PositionX => mascot == null ? 0 : mascot.anchoredPosition.x;

        public void Initialize(VersusKeyboardInput playerInput, RectTransform mascotTransform, Image bodyImage,
            float minimumX, float maximumX, float movementSpeed)
        {
            input = playerInput;
            mascot = mascotTransform;
            body = bodyImage;
            leftLimit = minimumX;
            rightLimit = maximumX;
            speed = movementSpeed;
        }

        public void SetColors(Color hatColor, Color shoeColor)
        {
            selectedRightHat = rightHat;
            selectedLeftHat = leftHat;
            foreach (var variant in hatVariants)
                if (ColorUtility.ToHtmlStringRGB(variant.Color) == ColorUtility.ToHtmlStringRGB(hatColor))
                {
                    selectedRightHat = variant.Right;
                    selectedLeftHat = variant.Left;
                    break;
                }
            ApplyHat();
            shoeColorHex = "#" + ColorUtility.ToHtmlStringRGB(hatColor);
            if (shoeSwatch != null) shoeSwatch.color = hatColor;
            if (body != null) body.material = null;
            RefreshShoeSprite();
        }

        private void RefreshShoeSprite()
        {
            if (body != null) body.sprite = MascotBodyPalette.ForFrame(body.sprite, shoeColorHex);
        }

        private void ApplyHat()
        {
            if (hat == null) return;
            hat.color = Color.white;
            hat.sprite = facesRight ? selectedRightHat ?? rightHat : selectedLeftHat ?? leftHat;
            var position = hat.rectTransform.anchoredPosition;
            position.x = Mathf.Abs(position.x) * (facesRight ? -1 : 1);
            hat.rectTransform.anchoredPosition = position;
        }

        public void SetReady(bool ready)
        {
            if (IsReady == ready) return;
            IsReady = ready;
            ReadyChanged?.Invoke(ready);
        }

        private void Update()
        {
            var selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            if (selected != null && selected.GetComponent<InputField>() != null) return;
            if (input != null && Application.isFocused)
                StepPractice(input.Read(), Time.unscaledDeltaTime);
        }

        public void StepPractice(InputSnapshot snapshot, float seconds)
        {
            if (mascot == null || !float.IsFinite(seconds) || seconds <= 0) return;
            bool left = snapshot.Left.Valid && snapshot.Left.Pressed;
            bool right = snapshot.Right.Valid && snapshot.Right.Pressed;
            if (left && right)
            {
                SetReady(true);
                return;
            }

            float direction = left ? -1 : right ? 1 : 0;
            var position = mascot.anchoredPosition;
            position.x = Mathf.Clamp(position.x + direction * speed * seconds, leftLimit, rightLimit);
            mascot.anchoredPosition = position;
            if (body == null) return;
            if (Mathf.Approximately(direction, 0))
            {
                animationTime = 0;
                if (idle != null) body.sprite = idle;
                RefreshShoeSprite();
                return;
            }

            facesRight = direction > 0;
            body.rectTransform.localScale = new Vector3(facesRight ? 1 : -1, 1, 1);
            ApplyHat();
            animationTime += seconds;
            if (runFrames != null && runFrames.Length > 0)
                body.sprite = runFrames[Mathf.FloorToInt(animationTime * 10) % runFrames.Length];
            RefreshShoeSprite();
        }
    }
}
