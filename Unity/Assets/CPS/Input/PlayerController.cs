using UnityEngine;

namespace CPS.KitchenCatch
{
    public sealed class PlayerController : MonoBehaviour
    {
        private const float MoveSpeed = 7;
        private const float FieldHalfWidth = 8;

        [SerializeField, Min(0)] private float mascotHalfWidth = 0.5f;
        [SerializeField] private Vector3 catchZoneOffset = new Vector3(0, 0.5f, 0);
        [SerializeField] private Vector3 catchZoneHalfExtents = new Vector3(1f, 0.4f, 0.1f);

        public int MovementDirection { get; private set; }
        private float MovementLimit => FieldHalfWidth - Mathf.Clamp(mascotHalfWidth, 0, FieldHalfWidth);
        public Vector2 ReachableCatchCenterRange =>
            new Vector2(catchZoneOffset.x - MovementLimit, catchZoneOffset.x + MovementLimit);
        public Bounds CatchZone => new Bounds(transform.position + catchZoneOffset,
            new Vector3(Mathf.Abs(catchZoneHalfExtents.x), Mathf.Abs(catchZoneHalfExtents.y),
                Mathf.Abs(catchZoneHalfExtents.z)) * 2);

        public void Step(InputSnapshot input, float dt)
        {
            MovementDirection = 0;
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0)
                return;

            bool left = input.Left.Valid && input.Left.Pressed;
            bool right = input.Right.Valid && input.Right.Pressed;
            MovementDirection = (right ? 1 : 0) - (left ? 1 : 0);
            float limit = MovementLimit;
            var position = transform.position;
            position.x = Mathf.Clamp(position.x + MovementDirection * MoveSpeed * dt, -limit, limit);
            transform.position = position;
        }

        public void ResetPosition()
        {
            var position = transform.position;
            position.x = 0;
            transform.position = position;
            MovementDirection = 0;
        }
    }
}
