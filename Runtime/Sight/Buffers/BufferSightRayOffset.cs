using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    public struct BufferSightRayOffset : IBufferElementData
    {
        public float3 Value;
    }
}