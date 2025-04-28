using Unity.Entities;

namespace Perception
{
    /// <summary>
    /// Adds duration of wave propagation to the source.
    /// </summary>
    public struct ComponentHearingDuration : IComponentData
    {
        public float Time;
    }
}