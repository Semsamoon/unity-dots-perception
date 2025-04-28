using Unity.Entities;

namespace Perception
{
    /// <summary>
    /// Adds radius of wave propagation sphere to the source.
    /// </summary>
    public struct ComponentHearingRadius : IComponentData
    {
        public float CurrentSquared;
        public float PreviousSquared;
        public float InternalCurrentSquared;
        public float InternalPreviousSquared;
    }
}