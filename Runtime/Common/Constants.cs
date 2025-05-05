using Unity.Mathematics;
using UnityEngine;

namespace Perception
{
    public static class Constants
    {
        public static readonly Color ColorRed = new(0.9f, 0.1f, 0.1f);
        public static readonly Color ColorBlue = new(0.1f, 0.1f, 0.9f);
        public static readonly Color ColorCyan = new(0.2f, 0.7f, 0.8f);
        public static readonly Color ColorGreen = new(0.1f, 0.9f, 0.1f);
        public static readonly Color ColorOrange = new(0.9f, 0.6f, 0.1f);
        public static readonly Color ColorPurple = new(0.6f, 0.2f, 0.6f);

        public static readonly float3 StretchedShape = new(0.5f, 1, 0.5f);

        public const float Epsilon = 0.001f;
    }
}