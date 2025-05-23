﻿using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    [BurstCompile, UpdateInGroup(typeof(HearingSystemGroup)), UpdateAfter(typeof(SystemHearingPosition))]
    public partial struct SystemHearingSphere : ISystem
    {
        private EntityQuery _queryWithoutRadius;

        private EntityQuery _query;

        private EntityTypeHandle _handleEntity;

        private ComponentTypeHandle<ComponentHearingRadius> _handleRadius;
        private ComponentTypeHandle<ComponentHearingSphere> _handleSphere;

        private int2 _chunkIndexRange;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

            _queryWithoutRadius = SystemAPI.QueryBuilder().WithAll<TagHearingSource, ComponentHearingSphere>().WithNone<ComponentHearingRadius>().Build();

            _query = SystemAPI.QueryBuilder().WithAll<TagHearingSource, ComponentHearingSphere>().Build();

            _handleEntity = SystemAPI.GetEntityTypeHandle();

            _handleRadius = SystemAPI.GetComponentTypeHandle<ComponentHearingRadius>();
            _handleSphere = SystemAPI.GetComponentTypeHandle<ComponentHearingSphere>(isReadOnly: true);
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
                commands.AddComponent(source, new ComponentHearingRadius { CurrentDuration = SystemAPI.GetComponent<ComponentHearingSphere>(source).Duration });
            }

            commands.Playback(state.EntityManager);

            _handleEntity.Update(ref state);

            _handleRadius.Update(ref state);
            _handleSphere.Update(ref state);

            var commandsParallel = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ranges = new NativeArray<int2>(1, Allocator.Temp);

            if (SystemAPI.TryGetSingleton(out ComponentHearingLimit limit) && limit.ChunksAmountSphere > 0)
            {
                var amounts = new NativeArray<int>(ranges.Length, Allocator.Temp);
                amounts[0] = _query.CalculateChunkCountWithoutFiltering();

                ComponentHearingLimit.CalculateRanges(limit.ChunksAmountSphere, amounts.AsReadOnly(), ref ranges, ref _chunkIndexRange);
            }
            else
            {
                for (var i = 0; i < ranges.Length; i++)
                {
                    ranges[i] = new int2(0, int.MaxValue);
                }
            }

            state.Dependency = new JobUpdateSphere
            {
                Commands = commandsParallel,
                HandleEntity = _handleEntity,
                HandleRadius = _handleRadius, HandleSphere = _handleSphere,
                ChunkIndexRange = ranges[0], DeltaTime = deltaTime,
            }.ScheduleParallel(_query, state.Dependency);
        }

        [BurstCompile]
        private struct JobUpdateSphere : IJobChunk
        {
            [WriteOnly] public EntityCommandBuffer.ParallelWriter Commands;

            [ReadOnly] public EntityTypeHandle HandleEntity;

            public ComponentTypeHandle<ComponentHearingRadius> HandleRadius;
            [ReadOnly] public ComponentTypeHandle<ComponentHearingSphere> HandleSphere;

            [ReadOnly] public int2 ChunkIndexRange;
            [ReadOnly] public float DeltaTime;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (unfilteredChunkIndex < ChunkIndexRange.x || unfilteredChunkIndex >= ChunkIndexRange.y)
                {
                    return;
                }

                var entities = chunk.GetNativeArray(HandleEntity);
                var radii = chunk.GetNativeArray(ref HandleRadius);
                var spheres = chunk.GetNativeArray(ref HandleSphere);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var radius = radii[i];
                    var sphere = spheres[i];

                    radius.PreviousSquared = radius.CurrentSquared;
                    radius.CurrentSquared = ComponentHearingRadius.CalculateCurrent(radius.CurrentSquared, sphere, DeltaTime);

                    if ((radius.CurrentDuration -= DeltaTime) > 0)
                    {
                        radii[i] = radius;
                        continue;
                    }

                    radius.InternalPreviousSquared = radius.InternalCurrentSquared;
                    radius.InternalCurrentSquared = ComponentHearingRadius.CalculateCurrent(radius.InternalCurrentSquared, sphere, DeltaTime);

                    radii[i] = radius;

                    if (radius.InternalCurrentSquared == sphere.RangeSquared)
                    {
                        Commands.DestroyEntity(unfilteredChunkIndex, entities[i]);
                    }
                }
            }
        }
    }
}