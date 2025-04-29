using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Contains sources holding in receiver's memory.
    /// </summary>
    public struct BufferHearingMemory : IBufferElementData
    {
        public float3 Position;
        public Entity Source;
        public float Time;
    }

    public static class BufferHearingMemoryExtensions
    {
        public static void Remove(this ref DynamicBuffer<BufferHearingMemory> bufferMemory, in Entity entity)
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