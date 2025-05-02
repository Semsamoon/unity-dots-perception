using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Specifies limits for calculations per frame.
    /// Must be a singleton.
    /// </summary>
    [BurstCompile]
    public struct ComponentHearingLimit : IComponentData
    {
        public int ChunksAmountPosition;
        public int ChunksAmountMemory;
        public int ChunksAmountSphere;
        public int ChunksAmountPerceive;

        [BurstCompile]
        public static void CalculateRanges(int limit, in NativeArray<int>.ReadOnly amounts, ref NativeArray<int2> ranges, ref int2 chunkIndexRange)
        {
            var sum = 0;
            foreach (var amount in amounts)
            {
                sum += amount;
            }

            chunkIndexRange = chunkIndexRange.y >= sum ? new int2(0, limit) : new int2(chunkIndexRange.y, chunkIndexRange.y + limit);

            for (var i = ranges.Length - 1; i >= 0; i--)
            {
                sum -= amounts[i];
                ranges[i] = chunkIndexRange - sum;
            }
        }
    }
}