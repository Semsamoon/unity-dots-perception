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
}