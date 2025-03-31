using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    public struct ComponentSightConeExtend : IComponentData
    {
        public float2 AnglesTan;
        public float RadiusSquared;
    }
}