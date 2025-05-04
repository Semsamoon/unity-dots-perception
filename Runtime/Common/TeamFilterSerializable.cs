using System;
using UnityEngine;

namespace Perception
{
    [Serializable]
    public struct TeamFilterSerializable
    {
        [TeamFilterMask] public uint BelongsTo;
        [TeamFilterMask] public uint Perceives;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class TeamFilterMaskAttribute : PropertyAttribute
    {
    }
}