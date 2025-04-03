using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Contains sources inside receiver's view cone.
    /// </summary>
    public struct BufferSightCone : IBufferElementData
    {
        public float3 Position;
        public Entity Source;
    }
}