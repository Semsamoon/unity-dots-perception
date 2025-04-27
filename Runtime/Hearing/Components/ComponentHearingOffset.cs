using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Adds offset to the receiver's and source's positions.
    /// </summary>
    public struct ComponentHearingOffset : IComponentData
    {
        public float3 Value;
    }
}