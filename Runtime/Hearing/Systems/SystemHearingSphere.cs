using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace Perception
{
    [BurstCompile, UpdateInGroup(typeof(HearingSystemGroup)), UpdateAfter(typeof(SystemHearingPosition))]
    public partial struct SystemHearingSphere : ISystem
    {
        private EntityQuery _queryWithoutRadius;

        private EntityQuery _queryWithDuration;
        private EntityQuery _query;

        private EntityTypeHandle _handleEntity;

        private ComponentTypeHandle<ComponentHearingRadius> _handleRadius;
        private ComponentTypeHandle<ComponentHearingSphere> _handleSphere;
        private ComponentTypeHandle<ComponentHearingDuration> _handleDuration;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            _queryWithoutRadius = SystemAPI.QueryBuilder().WithAll<TagHearingSource, ComponentHearingSphere>().WithNone<ComponentHearingRadius>().Build();

            _queryWithDuration = SystemAPI.QueryBuilder().WithAll<TagHearingSource, ComponentHearingSphere>().WithAll<ComponentHearingDuration>().Build();
            _query = SystemAPI.QueryBuilder().WithAll<TagHearingSource, ComponentHearingSphere>().WithNone<ComponentHearingDuration>().Build();

            _handleEntity = SystemAPI.GetEntityTypeHandle();

            _handleRadius = SystemAPI.GetComponentTypeHandle<ComponentHearingRadius>();
            _handleSphere = SystemAPI.GetComponentTypeHandle<ComponentHearingSphere>(isReadOnly: true);
            _handleDuration = SystemAPI.GetComponentTypeHandle<ComponentHearingDuration>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commands = new EntityCommandBuffer(Allocator.Temp);

            foreach (var source in _queryWithoutRadius.ToEntityArray(Allocator.Temp))
            {
                commands.AddComponent(source, new ComponentHearingRadius());
            }

            commands.Playback(state.EntityManager);

            _handleEntity.Update(ref state);

            _handleRadius.Update(ref state);
            _handleSphere.Update(ref state);
            _handleDuration.Update(ref state);

            var commandsParallel = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var deltaTime = SystemAPI.Time.DeltaTime;

            var jobHandle = new JobUpdateSphereWithDuration
            {
                HandleRadius = _handleRadius, HandleSphere = _handleSphere, DeltaTime = deltaTime,
                Commands = commandsParallel, HandleDuration = _handleDuration, HandleEntity = _handleEntity,
            }.ScheduleParallel(_queryWithDuration, state.Dependency);

            state.Dependency = new JobUpdateSphere
            {
                HandleRadius = _handleRadius, HandleSphere = _handleSphere, DeltaTime = deltaTime,
            }.ScheduleParallel(_query, jobHandle);
        }

        [BurstCompile]
        private struct JobUpdateSphereWithDuration : IJobChunk
        {
            public ComponentTypeHandle<ComponentHearingRadius> HandleRadius;
            [ReadOnly] public ComponentTypeHandle<ComponentHearingSphere> HandleSphere;
            [ReadOnly] public float DeltaTime;

            [WriteOnly] public EntityCommandBuffer.ParallelWriter Commands;
            public ComponentTypeHandle<ComponentHearingDuration> HandleDuration;
            [ReadOnly] public EntityTypeHandle HandleEntity;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var radii = chunk.GetNativeArray(ref HandleRadius);
                var spheres = chunk.GetNativeArray(ref HandleSphere);
                var durations = chunk.GetNativeArray(ref HandleDuration);
                var entities = chunk.GetNativeArray(HandleEntity);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var radius = radii[i];
                    var sphere = spheres[i];
                    var duration = durations[i];

                    radius.PreviousSquared = radius.CurrentSquared;
                    radius.CurrentSquared = ComponentHearingRadius.CalculateCurrent(radius.CurrentSquared, sphere, DeltaTime);

                    if ((duration.Time -= DeltaTime) > 0)
                    {
                        radii[i] = radius;
                        durations[i] = duration;
                        continue;
                    }

                    radius.InternalPreviousSquared = radius.InternalCurrentSquared;
                    radius.InternalCurrentSquared = ComponentHearingRadius.CalculateCurrent(radius.InternalCurrentSquared, sphere, DeltaTime);

                    radii[i] = radius;
                    durations[i] = duration;

                    if (radius.InternalCurrentSquared == sphere.RangeSquared)
                    {
                        Commands.DestroyEntity(unfilteredChunkIndex, entities[i]);
                    }
                }
            }
        }

        [BurstCompile]
        private struct JobUpdateSphere : IJobChunk
        {
            public ComponentTypeHandle<ComponentHearingRadius> HandleRadius;
            [ReadOnly] public ComponentTypeHandle<ComponentHearingSphere> HandleSphere;
            [ReadOnly] public float DeltaTime;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var radii = chunk.GetNativeArray(ref HandleRadius);
                var spheres = chunk.GetNativeArray(ref HandleSphere);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var radiusSquared = radii[i].CurrentSquared;

                    radii[i] = new ComponentHearingRadius
                    {
                        PreviousSquared = radiusSquared,
                        CurrentSquared = ComponentHearingRadius.CalculateCurrent(radiusSquared, spheres[i], DeltaTime),
                    };
                }
            }
        }
    }
}