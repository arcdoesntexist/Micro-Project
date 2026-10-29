using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace CPS.KitchenCatch
{
    public sealed class FallingObjectSpawner : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private FallingObject prefab;
        [SerializeField] private FallingObjectDefinition[] foodDefinitions;
        [SerializeField] private FallingObjectDefinition[] trashDefinitions;
        [SerializeField] private int seed = 1;
        private readonly List<FallingObject> active = new List<FallingObject>();
        private ObjectPool<FallingObject> pool;
        private SpawnPolicy policy;
        private DifficultyConfig config;
        private Action<long, FallingObjectDefinition, ObjectOutcome> report;
        private float now;
        private long nextId;
        private int generation;
        public int ActiveCount => active.Count;
        public PlayerController Player => player;

        public string GetValidationError(DifficultyConfig difficulty)
        {
            if (player == null) return "FallingObjectSpawner player reference is missing.";
            if (prefab == null) return "FallingObjectSpawner prefab reference is missing.";
            if (difficulty == null) return "Difficulty config is missing.";
            try { difficulty.Validate(); } catch (ArgumentException error) { return error.Message; }
            if (!DefinitionsValid(foodDefinitions, 3, ObjectKind.Food)) return "Three food definitions are required.";
            if (!DefinitionsValid(trashDefinitions, 2, ObjectKind.Trash)) return "Two trash definitions are required.";
            var bounds = player.CatchZone;
            if (!Finite(bounds.center) || !Finite(bounds.extents) || bounds.extents.y >= 7)
                return "Catch zone must be finite with half-height smaller than spawn distance.";
            var range = player.ReachableCatchCenterRange;
            if (float.IsNaN(range.x) || float.IsInfinity(range.x) || float.IsNaN(range.y) || float.IsInfinity(range.y))
                return "Player movement bounds must be finite.";
            if (range.x + range.y != 0)
                return "Catch zone horizontal offset must be zero for centered spawn paths.";
            foreach (float lane in difficulty.lanes)
                if (lane < range.x || lane > range.y)
                    return "Spawn lane is outside the player's reachable catcher-center range.";
            return null;
        }

        private static bool Finite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        private static bool DefinitionsValid(FallingObjectDefinition[] definitions, int count, ObjectKind kind)
        {
            if (definitions == null || definitions.Length != count) return false;
            foreach (var definition in definitions)
                if (definition == null || definition.kind != kind || definition.points < 0) return false;
            return true;
        }

        public void Begin(DifficultyConfig config, Action<long, FallingObjectDefinition, ObjectOutcome> report)
        {
            string error = GetValidationError(config);
            if (error != null) throw new InvalidOperationException(error);
            if (report == null) throw new ArgumentNullException(nameof(report));
            StopAndClear();
            this.config = config;
            this.report = report;
            now = 0;
            policy = new SpawnPolicy(config, seed, player.CatchZone.extents.y);
            if (pool == null)
                pool = new ObjectPool<FallingObject>(() => Instantiate(prefab, transform),
                    null, item => item.Despawn(), item => Destroy(item.gameObject), true, 16, 16);
        }

        public void Step(float dt)
        {
            if (policy == null || !DifficultyConfig.FinitePositive(dt)) return;
            var bounds = player.CatchZone;
            double remaining = dt;
            int currentGeneration = generation;
            while (remaining > 0)
            {
                if (active.Count < 16 && policy.TryChoose(now, 7, bounds.extents.x, out var choice))
                {
                    var item = pool.Get();
                    item.Spawn(++nextId, choice.Kind == ObjectKind.Food ? foodDefinitions[choice.DefinitionIndex] :
                        trashDefinitions[choice.DefinitionIndex], new Vector2(choice.X, bounds.center.y + 7), config.speed);
                    item.BindResolution(report);
                    active.Add(item);
                }
                float untilSpawn = policy.NextSpawnTime - now;
                float slice = (float)Math.Min(remaining, untilSpawn > 0 ? untilSpawn : 1f / 60);
                for (int i = active.Count - 1; i >= 0; i--)
                {
                    var item = active[i];
                    item.Step(slice, bounds);
                    if (generation != currentGeneration) return;
                    if (!item.IsActive) { active.RemoveAt(i); pool.Release(item); }
                }
                now += slice;
                remaining -= slice;
            }
        }

        public void StopAndClear()
        {
            generation++;
            policy = null;
            report = null;
            foreach (var item in active) pool.Release(item);
            active.Clear();
        }

        private void OnDestroy() { StopAndClear(); pool?.Clear(); }
    }
}
