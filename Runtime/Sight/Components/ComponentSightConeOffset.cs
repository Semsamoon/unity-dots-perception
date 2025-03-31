using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    public struct ComponentSightConeOffset : IComponentData
    {
        public float3 Value;
    }
}