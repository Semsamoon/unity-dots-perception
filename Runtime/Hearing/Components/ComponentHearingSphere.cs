using Unity.Entities;

namespace Perception
{
    public struct ComponentHearingSphere : IComponentData
    {
        public float Speed;
        public float RangeSquared;
    }
}