using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    /// <summary>
    /// Adds radius of wave propagation sphere to the source.
    /// </summary>
    [BurstCompile]
    public struct ComponentHearingRadius : IComponentData
    {
        public float CurrentSquared;
        public float PreviousSquared;
        public float InternalCurrentSquared;
        public float InternalPreviousSquared;

        [BurstCompile]
        public static float CalculateCurrent(float previousSquared, in ComponentHearingSphere sphere, float deltaTime)
        {
            var previous = math.sqrt(previousSquared);
            var result = previousSquared + 2 * previous * sphere.Speed * deltaTime + sphere.Speed * sphere.Speed * deltaTime * deltaTime;
            return math.min(result, sphere.RangeSquared);
        }
    }

    [BurstCompile]
    public static class ComponentHearingRadiusExtensions
    {
        [BurstCompile]
        public static bool IsInside(this in ComponentHearingRadius radius, float distanceCurrentSquared, float distancePreviousSquared)
        {
            return (distanceCurrentSquared <= radius.CurrentSquared && distanceCurrentSquared >= radius.InternalCurrentSquared)
                   || (distanceCurrentSquared > radius.CurrentSquared && distancePreviousSquared < radius.InternalPreviousSquared)
                   || (distanceCurrentSquared < radius.InternalCurrentSquared && distancePreviousSquared > radius.PreviousSquared);
        }
    }
}