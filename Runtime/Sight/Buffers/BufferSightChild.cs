using Unity.Burst;
using Unity.Entities;

namespace Perception
{
    /// <summary>
    /// Contains entities' children for ray casting.
    /// </summary>
    public struct BufferSightChild : IBufferElementData
    {
        public Entity Value;
    }

    [BurstCompile]
    public static class BufferSightChildExtensions
    {
        [BurstCompile]
        public static bool Contains(this in DynamicBuffer<BufferSightChild> bufferChild, in Entity entity)
        {
            foreach (var child in bufferChild)
            {
                if (child.Value == entity)
                {
                    return true;
                }
            }

            return false;
        }
    }
}