using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    public struct BufferHearingMemory : IBufferElementData
    {
        public float3 Position;
        public Entity Source;
        public float Time;
    }
}