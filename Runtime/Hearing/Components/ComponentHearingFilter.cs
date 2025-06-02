using Unity.Entities;

namespace Perception
{
    public struct ComponentHearingFilter : IComponentData
    {
        public uint BelongsTo;
        public uint Perceives;
    }
}