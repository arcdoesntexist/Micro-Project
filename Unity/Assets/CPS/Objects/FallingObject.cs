using System;
using UnityEngine;

namespace CPS.KitchenCatch
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class FallingObject : MonoBehaviour
    {
        public bool IsActive { get; private set; }
        public long SpawnId { get; private set; }
        private FallingObjectDefinition definition;
        private float speed;
        private Action<long, FallingObjectDefinition, ObjectOutcome> report;

        public void BindResolution(Action<long, FallingObjectDefinition, ObjectOutcome> callback) => report = callback;

        public void Spawn(long id, FallingObjectDefinition definition, Vector2 position, float speed)
        {
            if (id <= 0 || definition == null || !DifficultyConfig.FinitePositive(speed) ||
                float.IsNaN(position.x) || float.IsInfinity(position.x) ||
                float.IsNaN(position.y) || float.IsInfinity(position.y))
                throw new ArgumentException("Falling object spawn parameters are invalid.");
            report = null;
            SpawnId = id;
            this.definition = definition;
            this.speed = speed;
            transform.position = position;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            var renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = definition.sprite;
            renderer.color = Color.white;
            renderer.flipX = renderer.flipY = false;
            IsActive = true;
            gameObject.SetActive(true);
        }

        public void Step(float dt, Bounds catchZone)
        {
            if (!IsActive || !DifficultyConfig.FinitePositive(dt)) return;
            var previous = transform.position;
            var next = previous + Vector3.down * (speed * dt);
            transform.position = next;
            if (previous.x >= catchZone.min.x && previous.x <= catchZone.max.x &&
                previous.y >= catchZone.min.y && next.y <= catchZone.max.y)
                TryResolve(SpawnId, ObjectOutcome.Caught);
            else if (next.y < catchZone.min.y) TryResolve(SpawnId, ObjectOutcome.Missed);
        }

        public bool TryResolve(long id, ObjectOutcome outcome)
        {
            if (!IsActive || id != SpawnId) return false;
            IsActive = false;
            var callback = report;
            report = null;
            callback?.Invoke(id, definition, outcome);
            return true;
        }

        public void Despawn()
        {
            IsActive = false;
            SpawnId = 0;
            speed = 0;
            definition = null;
            report = null;
            GetComponent<SpriteRenderer>().sprite = null;
            gameObject.SetActive(false);
        }
    }
}
