using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Perception.Editor
{
    /// <summary>
    /// Specifies debug settings.
    /// Must be a singleton.
    /// </summary>
    public struct ComponentHearingDebug : IComponentData
    {
        public Color ColorSourceSphere;
        public Color ColorSourceInternal;

        public Color ColorSourcePerceived;
        public Color ColorSourceMemorized;

        public float3 SizeOctahedron;
        public float ScaleOctahedronBig;
        public float ScaleOctahedronSmall;

        public static ComponentHearingDebug Default => new()
        {
            ColorSourceSphere = Constants.ColorCyan,
            ColorSourceInternal = Constants.ColorBlue,

            ColorSourcePerceived = Constants.ColorGreen,
            ColorSourceMemorized = Constants.ColorOrange,

            SizeOctahedron = Constants.StretchedShape,
            ScaleOctahedronBig = 0.5f,
            ScaleOctahedronSmall = 0.25f,
        };
    }
}