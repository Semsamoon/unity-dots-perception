using Unity.Entities;

namespace Perception
{
    /// <summary>
    /// Adds team filter to receivers and sources.
    /// </summary>
    public struct ComponentSightFilter : IComponentData
    {
        public uint BelongsTo;
        public uint Perceives;
    }
}