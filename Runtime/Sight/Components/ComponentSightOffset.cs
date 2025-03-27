using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Adds offset to the entity's position during calculations.
    /// </summary>
    public struct ComponentSightOffset : IComponentData
    {
        public float3 Value;
    }
}