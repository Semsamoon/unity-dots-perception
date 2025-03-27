using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    public struct ComponentSightOffset : IComponentData
    {
        public float3 Value;
    }
}