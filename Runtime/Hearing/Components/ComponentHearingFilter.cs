using Unity.Burst;
using Unity.Entities;

namespace Perception
{
    /// <summary>
    /// Adds team filter to hearing receivers and sources.
    /// </summary>
    [BurstCompile]
    public struct ComponentHearingFilter : IComponentData
    {
        public uint BelongsTo;
        public uint Perceives;

        [BurstCompile]
        public bool CanPerceive(in ComponentHearingFilter filter)
        {
            return (Perceives & filter.BelongsTo) > 0;
        }

        public static implicit operator ComponentHearingFilter(in TeamFilterSerializable serializable)
        {
            return new ComponentHearingFilter { BelongsTo = serializable.BelongsTo, Perceives = serializable.Perceives };
        }

        public static implicit operator TeamFilterSerializable(in ComponentHearingFilter filter)
        {
            return new TeamFilterSerializable { BelongsTo = filter.BelongsTo, Perceives = filter.Perceives };
        }
    }
}