using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Adds extension of the cone of vision for visible sources.
    /// </summary>
    public struct ComponentSightExtend : IComponentData
    {
        public float2 AnglesCos;
        public float RadiusSquared;
    }
}