using System;
using UnityEngine;

namespace Perception
{
    [Serializable]
    public struct TeamFilterSerializable
    {
        public uint BelongsTo;
        public uint Perceives;
    }
}