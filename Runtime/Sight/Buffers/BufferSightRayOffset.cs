using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Contains rays' offsets for multiple ray casting.
    /// </summary>
    public struct BufferSightRayOffset : IBufferElementData
    {
        public float3 Value;
    }
}