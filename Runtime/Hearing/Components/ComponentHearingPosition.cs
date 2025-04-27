using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    public struct ComponentHearingPosition : IComponentData
    {
        public float3 Current;
        public float3 Previous;
    }
}