using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    public struct ComponentSightPosition : IComponentData
    {
        public float3 Value;
    }
}