using Unity.Entities;

namespace Perception
{
    public struct ComponentHearingRadius : IComponentData
    {
        public float CurrentSquared;
        public float PreviousSquared;
        public float InternalCurrentSquared;
        public float InternalPreviousSquared;
    }
}