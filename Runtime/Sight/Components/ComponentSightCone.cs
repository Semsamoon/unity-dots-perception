using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    public struct ComponentSightCone : IComponentData
    {
        public float2 AnglesTan;
        public float RadiusSquared;
    }
}