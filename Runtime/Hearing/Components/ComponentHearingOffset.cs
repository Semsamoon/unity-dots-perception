using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    public struct ComponentHearingOffset : IComponentData
    {
        public float3 Value;
    }
}