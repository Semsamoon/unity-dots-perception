using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Contains sources perceived by the receiver.
    /// </summary>
    public struct BufferHearingPerceive : IBufferElementData
    {
        public float3 Position;
        public Entity Source;
    }

    public static class BufferHearingPerceiveExtensions
    {
        public static bool Contains(this in DynamicBuffer<BufferHearingPerceive> bufferPerceive, in Entity entity, int length, out int index, out BufferHearingPerceive perceive)
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

        public static void RemoveAtSwapBack(this ref DynamicBuffer<BufferHearingPerceive> bufferPerceive, int index, int swapBackIndex)
        {
            bufferPerceive[index] = bufferPerceive[swapBackIndex];
            bufferPerceive.RemoveAtSwapBack(swapBackIndex);
        }

        public static void ToMemories(this ref DynamicBuffer<BufferHearingPerceive> bufferPerceive, int length, ref DynamicBuffer<BufferHearingMemory> bufferMemory, float time)
        {
            if (length <= 0)
            {
                return;
            }

            for (var j = 0; j < length; j++)
            {
                var perceive = bufferPerceive[j];
                bufferMemory.Add(new BufferHearingMemory() { Position = perceive.Position, Source = perceive.Source, Time = time });
            }

            bufferPerceive.RemoveRange(0, length);
        }
    }
}