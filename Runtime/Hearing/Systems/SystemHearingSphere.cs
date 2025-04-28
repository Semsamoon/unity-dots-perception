using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    public partial struct SystemHearingSphere : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var commands = new EntityCommandBuffer(Allocator.Temp);

            foreach (var source in SystemAPI.QueryBuilder()
                         .WithAll<ComponentHearingSphere>()
                         .WithNone<ComponentHearingRadius>()
                         .Build().ToEntityArray(Allocator.Temp))
            {
                commands.AddComponent(source, new ComponentHearingRadius());
            }

            commands.Playback(state.EntityManager);
            commands = new EntityCommandBuffer(Allocator.Temp);

            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (sphereRO, radiusRW, durationRW, source) in SystemAPI
                         .Query<RefRO<ComponentHearingSphere>, RefRW<ComponentHearingRadius>, RefRW<ComponentHearingDuration>>()
                         .WithAll<TagHearingSource>()
                         .WithEntityAccess())
            {
                ref readonly var sphere = ref sphereRO.ValueRO;
                ref var radius = ref radiusRW.ValueRW;
                ref var duration = ref durationRW.ValueRW;

                radius.PreviousSquared = radius.CurrentSquared;
                radius.CurrentSquared = math.min(AddSquared(radius.CurrentSquared, sphere.Speed * deltaTime), sphere.RangeSquared);

                if ((duration.Time -= deltaTime) > 0)
                {
                    continue;
                }

                radius.InternalPreviousSquared = radius.InternalCurrentSquared;
                radius.InternalCurrentSquared = math.min(AddSquared(radius.InternalCurrentSquared, sphere.Speed * deltaTime), sphere.RangeSquared);

                if (radius.InternalPreviousSquared == sphere.RangeSquared)
                {
                    commands.DestroyEntity(source);
                }
            }

            foreach (var (sphereRO, radiusRW) in SystemAPI
                         .Query<RefRO<ComponentHearingSphere>, RefRW<ComponentHearingRadius>>()
                         .WithAll<TagHearingSource>()
                         .WithNone<ComponentHearingDuration>())
            {
                ref readonly var sphere = ref sphereRO.ValueRO;
                ref var radius = ref radiusRW.ValueRW;

                radius.PreviousSquared = radius.CurrentSquared;
                radius.CurrentSquared = math.min(AddSquared(radius.CurrentSquared, sphere.Speed * deltaTime), sphere.RangeSquared);
            }

            commands.Playback(state.EntityManager);
        }

        private static float AddSquared(float aSquared, float b)
        {
            var a = math.sqrt(aSquared);
            return aSquared + 2 * a * b + b * b;
        }
    }
}