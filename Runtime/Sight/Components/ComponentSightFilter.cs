using Unity.Entities;
using Unity.Physics;

namespace Perception
{
    public struct ComponentSightFilter : IComponentData
    {
        public CollisionFilter Value;
    }
}