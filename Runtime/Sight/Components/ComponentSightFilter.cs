using Unity.Entities;
using Unity.Physics;

namespace Perception
{
    /// <summary>
    /// Adds collision filter to receiver for ray casting.
    /// </summary>
    public struct ComponentSightFilter : IComponentData
    {
        public CollisionFilter Value;
    }
}