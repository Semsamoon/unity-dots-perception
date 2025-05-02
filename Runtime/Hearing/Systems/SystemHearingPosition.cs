using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Perception
{
    [BurstCompile, UpdateInGroup(typeof(HearingSystemGroup))]
    public partial struct SystemHearingPosition : ISystem
    {
        private EntityQuery _queryWithoutPosition;
        private EntityQuery _queryWithoutTransform;

        private EntityQuery _queryWithOffset;
        private EntityQuery _query;

        private ComponentTypeHandle<ComponentHearingPosition> _handlePosition;
        private ComponentTypeHandle<ComponentHearingOffset> _handleOffset;
        private ComponentTypeHandle<LocalToWorld> _handleTransform;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _queryWithoutPosition = SystemAPI.QueryBuilder().WithAny<TagHearingReceiver, TagHearingSource>().WithNone<ComponentHearingPosition>().Build();
            _queryWithoutTransform = SystemAPI.QueryBuilder().WithAny<TagHearingReceiver>().WithNone<LocalToWorld>().Build();

            _queryWithOffset = SystemAPI.QueryBuilder().WithAny<TagHearingReceiver, TagHearingSource>().WithAll<LocalToWorld, ComponentHearingOffset>().Build();
            _query = SystemAPI.QueryBuilder().WithAny<TagHearingReceiver, TagHearingSource>().WithAll<LocalToWorld>().WithNone<ComponentHearingOffset>().Build();

            _handlePosition = SystemAPI.GetComponentTypeHandle<ComponentHearingPosition>();
            _handleOffset = SystemAPI.GetComponentTypeHandle<ComponentHearingOffset>(isReadOnly: true);
            _handleTransform = SystemAPI.GetComponentTypeHandle<LocalToWorld>(isReadOnly: true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commands = new EntityCommandBuffer(Allocator.Temp);

            foreach (var entity in _queryWithoutPosition.ToEntityArray(Allocator.Temp))
            {
                commands.AddComponent(entity, new ComponentHearingPosition());
            }

            foreach (var receiver in _queryWithoutTransform.ToEntityArray(Allocator.Temp))
            {
                commands.AddComponent(receiver, new LocalToWorld { Value = float4x4.identity });
            }

            commands.Playback(state.EntityManager);

            _handlePosition.Update(ref state);
            _handleOffset.Update(ref state);
            _handleTransform.Update(ref state);

            var jobHandle = new JobUpdatePositionWithOffset
            {
                HandlePosition = _handlePosition, HandleTransform = _handleTransform,
                HandleOffset = _handleOffset,
            }.ScheduleParallel(_queryWithOffset, state.Dependency);

            state.Dependency = new JobUpdatePosition
            {
                HandlePosition = _handlePosition, HandleTransform = _handleTransform,
            }.ScheduleParallel(_query, jobHandle);
        }

        [BurstCompile]
        private struct JobUpdatePositionWithOffset : IJobChunk
        {
            public ComponentTypeHandle<ComponentHearingPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> HandleTransform;

            [ReadOnly] public ComponentTypeHandle<ComponentHearingOffset> HandleOffset;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var positions = chunk.GetNativeArray(ref HandlePosition);
                var transforms = chunk.GetNativeArray(ref HandleTransform);

                var offsets = chunk.GetNativeArray(ref HandleOffset);

                for (var i = 0; i < chunk.Count; i++)
                {
                    positions[i] = new ComponentHearingPosition { Previous = positions[i].Current, Current = transforms[i].Value.TransformPoint(offsets[i].Value) };
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePosition : IJobChunk
        {
            public ComponentTypeHandle<ComponentHearingPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> HandleTransform;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var positions = chunk.GetNativeArray(ref HandlePosition);
                var transforms = chunk.GetNativeArray(ref HandleTransform);

                for (var i = 0; i < chunk.Count; i++)
                {
                    positions[i] = new ComponentHearingPosition { Previous = positions[i].Current, Current = transforms[i].Position };
                }
            }
        }
    }
}