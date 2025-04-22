using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Contains sources holding in receiver's memory.
    /// </summary>
    public struct BufferSightMemory : IBufferElementData
    {
        public float3 Position;
        public Entity Source;
        public float Time;
    }

    [BurstCompile]
    public static class BufferSightMemoryExtensions
    {
        [BurstCompile]
        public static bool Contains(this in DynamicBuffer<BufferSightMemory> bufferMemory, in Entity entity)
        {
            foreach (var memory in bufferMemory)
            {
                if (memory.Source == entity)
                {
                    return true;
                }
            }

            return false;
        }

        [BurstCompile]
        public static void Remove(this ref DynamicBuffer<BufferSightMemory> bufferMemory, in Entity entity)
        {
            for (var i = 0; i < bufferMemory.Length; i++)
            {
                if (bufferMemory[i].Source == entity)
                {
                    bufferMemory.RemoveAtSwapBack(i);
                    return;
                }
            }
        }
    }
}