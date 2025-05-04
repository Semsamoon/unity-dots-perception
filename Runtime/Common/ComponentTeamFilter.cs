using Unity.Burst;
using Unity.Entities;

namespace Perception
{
    /// <summary>
    /// Adds team filter to receivers and sources.
    /// </summary>
    [BurstCompile]
    public struct ComponentTeamFilter : IComponentData
    {
        public uint BelongsTo;
        public uint Perceives;

        [BurstCompile]
        public bool CanPerceive(in ComponentTeamFilter filter)
        {
            return (Perceives & filter.BelongsTo) > 0;
        }
    }
}