using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Perception.Editor
{
    /// <summary>
    /// Specifies debug settings.
    /// Must be a singleton.
    /// </summary>
    public struct ComponentSightDebug : IComponentData
    {
        public Color ColorReceiverCone;
        public Color ColorReceiverExtend;

        public Color ColorSourcePerceived;
        public Color ColorSourceMemorized;
        public Color ColorSourceHidden;

        public float3 SizeOctahedron;
        public float ScaleOctahedronBig;
        public float ScaleOctahedronSmall;

        public static ComponentSightDebug Default => new()
        {
            ColorReceiverCone = Constants.ColorCyan,
            ColorReceiverExtend = Constants.ColorBlue,

            ColorSourcePerceived = Constants.ColorGreen,
            ColorSourceMemorized = Constants.ColorOrange,
            ColorSourceHidden = Constants.ColorRed,

            SizeOctahedron = Constants.StretchedShape,
            ScaleOctahedronBig = 0.5f,
            ScaleOctahedronSmall = 0.25f,
        };
    }
}