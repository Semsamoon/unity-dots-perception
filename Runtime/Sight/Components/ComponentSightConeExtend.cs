using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Adds extension of the cone of vision for visible sources.
    /// </summary>
    public struct ComponentSightConeExtend : IComponentData
    {
        public float2 AnglesTan;
        public float RadiusSquared;
    }
}