using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Adds cone of vision to the receiver.
    /// </summary>
    public struct ComponentSightCone : IComponentData
    {
        public float2 AnglesTan;
        public float RadiusSquared;
    }
}