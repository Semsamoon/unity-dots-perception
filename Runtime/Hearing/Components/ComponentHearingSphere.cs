using Unity.Entities;

namespace Perception
{
    /// <summary>
    /// Add sphere of wave propagation to the source.
    /// </summary>
    public struct ComponentHearingSphere : IComponentData
    {
        public float Speed;
        public float RangeSquared;
        public float Duration;
    }
}