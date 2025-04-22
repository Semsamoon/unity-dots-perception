using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Contains sources perceived by the receiver.
    /// </summary>
    public struct BufferSightPerceive : IBufferElementData
    {
        public float3 Position;
        public Entity Source;
    }

    [BurstCompile]
    public static class BufferSightPerceiveExtensions
    {
        [BurstCompile]
        public static bool Contains(this in DynamicBuffer<BufferSightPerceive> bufferPerceive, in Entity entity, int length, out int index, out BufferSightPerceive perceive)
        {
            for (var i = 0; i < length; i++)
            {
                perceive = bufferPerceive[i];

                if (perceive.Source == entity)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            perceive = default;
            return false;
        }

        [BurstCompile]
        public static bool Contains(this in DynamicBuffer<BufferSightPerceive> bufferPerceive, in Entity entity, int length)
        {
            return bufferPerceive.Contains(in entity, length, out _, out _);
        }

        [BurstCompile]
        public static bool Contains(this in DynamicBuffer<BufferSightPerceive> bufferPerceive, in Entity entity)
        {
            return bufferPerceive.Contains(in entity, bufferPerceive.Length, out _, out _);
        }

        [BurstCompile]
        public static void RemoveAtSwapBack(this ref DynamicBuffer<BufferSightPerceive> bufferPerceive, int index, int swapBackIndex)
        {
            bufferPerceive[index] = bufferPerceive[swapBackIndex];
            bufferPerceive.RemoveAtSwapBack(swapBackIndex);
        }

        [BurstCompile]
        public static void ToMemories(this ref DynamicBuffer<BufferSightPerceive> bufferPerceive, int length, ref DynamicBuffer<BufferSightMemory> bufferMemory, float time)
        {
            if (length <= 0)
            {
                return;
            }

            for (var j = 0; j < length; j++)
            {
                var perceive = bufferPerceive[j];
                bufferMemory.Add(new BufferSightMemory { Position = perceive.Position, Source = perceive.Source, Time = time });
            }

            bufferPerceive.RemoveRange(0, length);
        }
    }
}