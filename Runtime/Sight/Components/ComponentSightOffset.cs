using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Adds offset to the receiver's cone of vision position and source's position.
    /// </summary>
    public struct ComponentSightOffset : IComponentData
    {
        public float3 Receiver;
        public float3 Source;
    }
}