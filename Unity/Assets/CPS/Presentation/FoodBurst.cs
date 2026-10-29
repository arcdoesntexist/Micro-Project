using UnityEngine;

namespace CPS.KitchenCatch
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class FoodBurst : MonoBehaviour
    {
        private Vector2 origin;
        private Vector2 velocity;
        private float age;
        private SpriteRenderer visual;

        public void Launch(Sprite sprite, Vector2 start, Vector2 initialVelocity)
        {
            visual ??= GetComponent<SpriteRenderer>();
            visual.sprite = sprite;
            visual.color = Color.white;
            origin = start;
            velocity = initialVelocity;
            age = 0;
            transform.position = origin;
            transform.rotation = Quaternion.identity;
            gameObject.SetActive(true);
        }

        public void Step(float seconds)
        {
            age += seconds;
            transform.position = origin + velocity * age + Vector2.down * (6f * age * age);
            if (visual != null)
            {
                var color = visual.color;
                color.a = Mathf.Clamp01((.43f - age) * 8f);
                visual.color = color;
            }
        }

        public void Clear()
        {
            if (visual != null) visual.sprite = null;
            gameObject.SetActive(false);
        }
    }
}
