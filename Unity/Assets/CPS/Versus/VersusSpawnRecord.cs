using System;

namespace CPS.KitchenCatch
{
    public readonly struct VersusSpawnRecord : IEquatable<VersusSpawnRecord>
    {
        public long Id { get; }
        public ObjectKind Kind { get; }
        public int DefinitionIndex { get; }
        public float NormalizedX { get; }
        public float SpawnTime { get; }
        public float FallSpeed { get; }

        public VersusSpawnRecord(long id, ObjectKind kind, int definitionIndex,
            float normalizedX, float spawnTime, float fallSpeed)
        {
            Id = id;
            Kind = kind;
            DefinitionIndex = definitionIndex;
            NormalizedX = normalizedX;
            SpawnTime = spawnTime;
            FallSpeed = fallSpeed;
        }

        public bool Equals(VersusSpawnRecord other) =>
            Id == other.Id && Kind == other.Kind && DefinitionIndex == other.DefinitionIndex &&
            NormalizedX.Equals(other.NormalizedX) && SpawnTime.Equals(other.SpawnTime) &&
            FallSpeed.Equals(other.FallSpeed);

        public override bool Equals(object obj) => obj is VersusSpawnRecord other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, Kind, DefinitionIndex,
            NormalizedX, SpawnTime, FallSpeed);
    }
}
