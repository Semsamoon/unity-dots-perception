using Unity.Burst;
using Unity.Entities;

namespace Perception
{
    /// <summary>
    /// Adds team filter to sight receivers and sources.
    /// </summary>
    [BurstCompile]
    public struct ComponentSightFilter : IComponentData
    {
        public uint BelongsTo;
        public uint Perceives;

        [BurstCompile]
        public bool CanPerceive(in ComponentSightFilter filter)
        {
            return (Perceives & filter.BelongsTo) > 0;
        }

        public static implicit operator ComponentSightFilter(in TeamFilterSerializable serializable)
        {
            return new ComponentSightFilter { BelongsTo = serializable.BelongsTo, Perceives = serializable.Perceives };
        }

        public static implicit operator TeamFilterSerializable(in ComponentSightFilter filter)
        {
            return new TeamFilterSerializable { BelongsTo = filter.BelongsTo, Perceives = filter.Perceives };
        }
    }
}