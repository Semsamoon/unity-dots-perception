using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    public struct BufferSightInsideCone : IBufferElementData
    {
        public float3 Position;
        public Entity Source;
    }
}