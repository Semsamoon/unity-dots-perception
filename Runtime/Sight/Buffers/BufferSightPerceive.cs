using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    public struct BufferSightPerceive : IBufferElementData
    {
        public float3 Position;
        public Entity Source;
    }
}