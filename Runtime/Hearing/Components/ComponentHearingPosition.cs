using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Adds summarized receiver's and source's positions for calculations.
    /// </summary>
    public struct ComponentHearingPosition : IComponentData
    {
        public float3 Current;
        public float3 Previous;
    }
}