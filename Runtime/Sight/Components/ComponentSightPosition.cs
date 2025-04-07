using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Adds summarized receiver's cone of vision position
    /// and source's position for calculations.
    /// </summary>
    public struct ComponentSightPosition : IComponentData
    {
        public float3 Receiver;
        public float3 Source;
    }
}