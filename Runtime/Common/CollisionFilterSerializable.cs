using System;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace Perception
{
    [Serializable]
    public struct CollisionFilterSerializable
    {
        public LayerMask BelongsTo;
        public LayerMask CollidesWith;
        public int GroupIndex;

        public static implicit operator CollisionFilter(in CollisionFilterSerializable serializable)
        {
            return new CollisionFilter
            {
                BelongsTo = math.asuint(serializable.BelongsTo.value),
                CollidesWith = math.asuint(serializable.CollidesWith.value),
                GroupIndex = serializable.GroupIndex,
            };
        }

        public static implicit operator CollisionFilterSerializable(in CollisionFilter filter)
        {
            return new CollisionFilterSerializable
            {
                BelongsTo = math.asint(filter.BelongsTo),
                CollidesWith = math.asint(filter.CollidesWith),
                GroupIndex = filter.GroupIndex,
            };
        }
    }
}