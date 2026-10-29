using System;
using UnityEngine;

namespace CPS.KitchenCatch
{
    [CreateAssetMenu(menuName = "CPS/Difficulty")]
    public sealed class DifficultyConfig : ScriptableObject
    {
        public DifficultyId difficulty;
        public float speed;
        public float interval;
        public float trashProbability;
        public float[] lanes;
        public void Validate()
        {
            if (!Enum.IsDefined(typeof(DifficultyId), difficulty)) throw new ArgumentException("Difficulty is invalid.");
            if (!FinitePositive(speed)) throw new ArgumentException("Fall speed must be finite and positive.");
            if (!FinitePositive(interval)) throw new ArgumentException("Spawn interval must be finite and positive.");
            if (float.IsNaN(trashProbability) || trashProbability < 0 || trashProbability > 1)
                throw new ArgumentException("Trash probability must be between zero and one.");
            if (lanes == null || lanes.Length == 0) throw new ArgumentException("Spawn lanes are missing.");
            foreach (float lane in lanes)
                if (float.IsNaN(lane) || float.IsInfinity(lane) || Mathf.Abs(lane) > 7.5f)
                    throw new ArgumentException("Spawn lane is outside the reachable field.");
            if (difficulty == DifficultyId.Easy && trashProbability != 0)
                throw new ArgumentException("Easy difficulty must have zero trash probability.");
        }

        internal static bool FinitePositive(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0;
    }
}
