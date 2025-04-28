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
}