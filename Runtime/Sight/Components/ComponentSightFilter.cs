using Unity.Entities;

namespace Perception
{
    public struct ComponentSightFilter : IComponentData
    {
        public uint BelongsTo;
        public uint Perceives;
    }
}