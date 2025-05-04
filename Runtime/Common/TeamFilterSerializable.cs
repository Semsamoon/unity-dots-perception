using System;
using UnityEngine;

namespace Perception
{
    [Serializable]
    public struct TeamFilterSerializable
    {
        [TeamFilterMask] public uint BelongsTo;
        [TeamFilterMask] public uint Perceives;

        public static implicit operator ComponentTeamFilter(in TeamFilterSerializable serializable)
        {
            return new ComponentTeamFilter { BelongsTo = serializable.BelongsTo, Perceives = serializable.Perceives };
        }

        public static implicit operator TeamFilterSerializable(in ComponentTeamFilter filter)
        {
            return new TeamFilterSerializable { BelongsTo = filter.BelongsTo, Perceives = filter.Perceives };
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class TeamFilterMaskAttribute : PropertyAttribute
    {
    }
}