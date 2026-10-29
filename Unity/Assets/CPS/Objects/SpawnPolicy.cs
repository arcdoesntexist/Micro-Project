using System;
using System.Collections.Generic;
using UnityEngine;

namespace CPS.KitchenCatch
{
    public readonly struct SpawnChoice
    {
        public ObjectKind Kind { get; }
        public int DefinitionIndex { get; }
        public float X { get; }
        public float ArrivalTime { get; }
        public SpawnChoice(ObjectKind kind, int index, float x, float arrival)
        { Kind = kind; DefinitionIndex = index; X = x; ArrivalTime = arrival; }
    }

    public sealed class SpawnPolicy
    {
        private readonly float speed;
        private readonly float interval;
        private readonly float trashProbability;
        private readonly float[] lanes;
        private readonly bool cyclic;
        private readonly float window;
        private readonly System.Random random;
        private readonly List<SpawnChoice> foods = new List<SpawnChoice>();
        private readonly List<SpawnChoice> trash = new List<SpawnChoice>();
        private float nextSpawn;
        private int laneIndex;
        internal float NextSpawnTime => nextSpawn;

        public SpawnPolicy(DifficultyConfig config, int seed, float catchHalfHeight = 0.25f)
        {
            if (config == null) throw new ArgumentException("Difficulty config is missing.");
            config.Validate();
            if (float.IsNaN(catchHalfHeight) || float.IsInfinity(catchHalfHeight) || catchHalfHeight < 0)
                throw new ArgumentException("Catch half-height must be finite and nonnegative.");
            speed = config.speed;
            interval = config.interval;
            trashProbability = config.trashProbability;
            lanes = (float[])config.lanes.Clone();
            cyclic = config.difficulty == DifficultyId.Easy;
            window = catchHalfHeight / speed;
            random = new System.Random(seed);
            foods.Add(new SpawnChoice(ObjectKind.Food, 0, 0, 0));
        }

        public bool TryChoose(float now, float playerSpeed, float catchHalfWidth, out SpawnChoice choice)
        {
            choice = default;
            if (float.IsNaN(now) || float.IsInfinity(now) || now < 0 ||
                !DifficultyConfig.FinitePositive(playerSpeed) || float.IsNaN(catchHalfWidth) ||
                float.IsInfinity(catchHalfWidth) || catchHalfWidth < 0)
                throw new ArgumentException("Spawn time, player speed or catch half-width is invalid.");
            if (now < nextSpawn) return false;
            while (foods.Count > 1 && foods[1].ArrivalTime + window < now) foods.RemoveAt(0);
            trash.RemoveAll(t => t.ArrivalTime + window < now);
            float arrival = now + 7 / speed;
            for (int attempt = 0; attempt < (cyclic ? 1 : 16); attempt++)
            {
                var kind = !cyclic && random.NextDouble() < trashProbability ? ObjectKind.Trash : ObjectKind.Food;
                float x = cyclic ? lanes[laneIndex] : lanes[random.Next(lanes.Length)];
                bool safe = kind == ObjectKind.Food ? FoodSafe(x, arrival, playerSpeed, catchHalfWidth) :
                    TrashSafe(x, arrival, catchHalfWidth);
                if (!safe) continue;
                choice = new SpawnChoice(kind, random.Next(kind == ObjectKind.Food ? 3 : 2), x, arrival);
                if (kind == ObjectKind.Food) foods.Add(choice); else trash.Add(choice);
                if (cyclic) laneIndex = (laneIndex + 1) % lanes.Length;
                nextSpawn = now + interval;
                return true;
            }
            return false;
        }

        private bool FoodSafe(float x, float arrival, float playerSpeed, float halfWidth)
        {
            var before = foods[foods.Count - 1];
            float departure = Mathf.Max(before.ArrivalTime + window, arrival - 7 / speed);
            if (Mathf.Abs(x - before.X) > playerSpeed * (arrival - window - departure)) return false;
            var after = new SpawnChoice(ObjectKind.Food, 0, x, arrival);
            foreach (var hazard in trash)
                if (!SegmentSafe(before, after, hazard.X, hazard.ArrivalTime, halfWidth)) return false;
            return true;
        }

        private bool TrashSafe(float x, float arrival, float halfWidth)
        {
            for (int i = 1; i < foods.Count; i++)
                if (!SegmentSafe(foods[i - 1], foods[i], x, arrival, halfWidth)) return false;
            var last = foods[foods.Count - 1];
            return arrival + window < last.ArrivalTime || Mathf.Abs(x - last.X) > halfWidth;
        }

        private bool SegmentSafe(SpawnChoice before, SpawnChoice after, float x, float arrival, float halfWidth)
        {
            float start = Mathf.Max(before.ArrivalTime, arrival - window);
            float end = Mathf.Min(after.ArrivalTime, arrival + window);
            if (start > end) return true;
            float startX = PathX(before, after, start);
            float endX = PathX(before, after, end);
            return x < Mathf.Min(startX, endX) - halfWidth || x > Mathf.Max(startX, endX) + halfWidth;
        }

        private float PathX(SpawnChoice before, SpawnChoice after, float time)
        {
            float departure = Mathf.Max(before.ArrivalTime + window, after.ArrivalTime - 7 / speed);
            float travelDuration = after.ArrivalTime - window - departure;
            if (travelDuration <= 0) return before.X;
            return Mathf.Lerp(before.X, after.X, Mathf.Clamp01((time - departure) / travelDuration));
        }
    }
}
