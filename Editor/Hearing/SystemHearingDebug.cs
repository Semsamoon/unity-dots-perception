﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Perception.Editor
{
    [BurstCompile, UpdateInGroup(typeof(HearingSystemGroup), OrderLast = true)]
    public partial struct SystemHearingDebug : ISystem
    {
        private EntityQuery _queryWithoutDebug;

        private EntityQuery _querySphereWithTransform;
        private EntityQuery _querySphere;
        private EntityQuery _queryMemorized;
        private EntityQuery _queryPerceived;

        private ComponentLookup<ComponentHearingPosition> _lookupPosition;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ComponentHearingDebug>();

            if (!SystemAPI.HasSingleton<ComponentHearingDebug>())
            {
                state.EntityManager.CreateSingleton(ComponentHearingDebug.Default);
            }

            _queryWithoutDebug = SystemAPI.QueryBuilder().WithAny<TagHearingReceiver, TagHearingSource>().WithNone<TagHearingDebug>().Build();

            _querySphereWithTransform = SystemAPI.QueryBuilder().WithAll<TagHearingSource, TagHearingDebug>()
                .WithAll<ComponentHearingPosition, ComponentHearingRadius>().WithAll<LocalToWorld>().Build();
            _querySphere = SystemAPI.QueryBuilder().WithAll<TagHearingSource, TagHearingDebug>()
                .WithAll<ComponentHearingPosition, ComponentHearingRadius>().WithNone<LocalToWorld>().Build();
            _queryMemorized = SystemAPI.QueryBuilder().WithAll<TagHearingReceiver, TagHearingDebug>().WithAll<ComponentHearingPosition, BufferHearingMemory>().Build();
            _queryPerceived = SystemAPI.QueryBuilder().WithAll<TagHearingReceiver, TagHearingDebug>().WithAll<ComponentHearingPosition, BufferHearingPerceive>().Build();

            _lookupPosition = SystemAPI.GetComponentLookup<ComponentHearingPosition>(isReadOnly: true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commands = new EntityCommandBuffer(Allocator.Temp);

            foreach (var source in _queryWithoutDebug.ToEntityArray(Allocator.Temp))
            {
                commands.AddComponent<TagHearingDebug>(source);
                commands.SetComponentEnabled<TagHearingDebug>(source, false);
            }

            commands.Playback(state.EntityManager);

            _lookupPosition.Update(ref state);

            ref readonly var debug = ref SystemAPI.GetSingletonRW<ComponentHearingDebug>().ValueRO;

            var jobHandle = new JobDebugSphereWithTransform { Debug = debug }.ScheduleParallel(_querySphereWithTransform, state.Dependency);
            jobHandle = new JobDebugSphere { Debug = debug }.ScheduleParallel(_querySphere, jobHandle);
            jobHandle = new JobDebugMemorized { Debug = debug, LookupPosition = _lookupPosition }.ScheduleParallel(_queryMemorized, jobHandle);
            state.Dependency = new JobDebugPerceived { Debug = debug }.ScheduleParallel(_queryPerceived, jobHandle);
            state.Dependency.Complete();
        }

        [BurstCompile]
        private partial struct JobDebugSphereWithTransform : IJobEntity
        {
            [ReadOnly] public ComponentHearingDebug Debug;

            [BurstCompile]
            public void Execute(in ComponentHearingPosition position, in ComponentHearingRadius radius, in LocalToWorld transform)
            {
                DebugAdvanced.DrawSphere(in position.Current, transform.Rotation, math.sqrt(radius.CurrentSquared), in Debug.ColorSourceSphere);

                if (radius.CurrentDuration <= 0)
                {
                    DebugAdvanced.DrawSphere(in position.Current, transform.Rotation, math.sqrt(radius.InternalCurrentSquared), in Debug.ColorSourceInternal);
                }
            }
        }

        [BurstCompile]
        private partial struct JobDebugSphere : IJobEntity
        {
            [ReadOnly] public ComponentHearingDebug Debug;

            [BurstCompile]
            public void Execute(in ComponentHearingPosition position, in ComponentHearingRadius radius)
            {
                DebugAdvanced.DrawSphere(in position.Current, in quaternion.identity, math.sqrt(radius.CurrentSquared), in Debug.ColorSourceSphere);

                if (radius.CurrentDuration <= 0)
                {
                    DebugAdvanced.DrawSphere(in position.Current, in quaternion.identity, math.sqrt(radius.InternalCurrentSquared), in Debug.ColorSourceInternal);
                }
            }
        }

        [BurstCompile]
        private partial struct JobDebugMemorized : IJobEntity
        {
            [ReadOnly] public ComponentHearingDebug Debug;

            [ReadOnly] public ComponentLookup<ComponentHearingPosition> LookupPosition;

            [BurstCompile]
            public void Execute(in DynamicBuffer<BufferHearingMemory> bufferMemory, in ComponentHearingPosition position)
            {
                foreach (var memory in bufferMemory)
                {
                    UnityEngine.Debug.DrawLine(position.Current, memory.Position, Debug.ColorSourceMemorized);
                    DebugAdvanced.DrawOctahedron(in memory.Position, Debug.SizeOctahedron * Debug.ScaleOctahedronSmall, in Debug.ColorSourceMemorized);

                    if (LookupPosition.TryGetComponent(memory.Source, out var sourcePosition))
                    {
                        UnityEngine.Debug.DrawLine(memory.Position, sourcePosition.Current, Debug.ColorSourceMemorized);
                        DebugAdvanced.DrawOctahedron(in sourcePosition.Current, Debug.SizeOctahedron * Debug.ScaleOctahedronBig, in Debug.ColorSourceMemorized);
                    }
                }
            }
        }

        [BurstCompile]
        private partial struct JobDebugPerceived : IJobEntity
        {
            [ReadOnly] public ComponentHearingDebug Debug;

            [BurstCompile]
            public void Execute(in DynamicBuffer<BufferHearingPerceive> bufferPerceive, in ComponentHearingPosition position)
            {
                foreach (var perceive in bufferPerceive)
                {
                    UnityEngine.Debug.DrawLine(position.Current, perceive.Position, Debug.ColorSourcePerceived);
                    DebugAdvanced.DrawOctahedron(in perceive.Position, Debug.SizeOctahedron * Debug.ScaleOctahedronBig, in Debug.ColorSourcePerceived);
                }
            }
        }
    }
}