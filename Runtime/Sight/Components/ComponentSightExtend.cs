using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Perception
{
    /// <summary>
    /// Adds extension of the cone of vision for visible sources.
    /// </summary>
    public struct ComponentSightExtend : IComponentData
    {
        public float2 AnglesCos;
        public float RadiusSquared;
        public float ClipSquared;
    }

    [BurstCompile]
    public static class ComponentSightExtendExtensions
    {
        [BurstCompile]
        public static bool IsInside(this in ComponentSightExtend extend, in float3 origin, in float3 target, in LocalToWorld transform)
        {
            var difference = target - origin;
            var distanceSquared = math.lengthsq(difference);

            if (distanceSquared > extend.RadiusSquared || distanceSquared < extend.ClipSquared)
            {
                return false;
            }

            var directionLocal = transform.Value.InverseTransformDirection(difference);
            var squared = directionLocal * directionLocal;

            return directionLocal.z / math.sqrt(squared.x + squared.z) >= extend.AnglesCos.x
                   && math.sqrt((squared.x + squared.z) / (squared.x + squared.y + squared.z)) >= extend.AnglesCos.y;
        }
    }
}