using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Adds offset to the receiver's cone of vision.
    /// </summary>
    public struct ComponentSightConeOffset : IComponentData
    {
        public float3 Value;
    }
}