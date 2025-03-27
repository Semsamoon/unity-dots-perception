using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Contains summarized entity's position for calculations.
    /// </summary>
    public struct ComponentSightPosition : IComponentData
    {
        public float3 Value;
    }
}