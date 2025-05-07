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

        private int2 _chunkIndexRange;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _queryWithoutPosition = SystemAPI.QueryBuilder().WithAny<TagHearingReceiver, TagHearingSource>().WithNone<ComponentHearingPosition>().Build();
            _queryWithoutTransform = SystemAPI.QueryBuilder().WithAny<TagHearingReceiver, TagHearingSource>().WithNone<LocalToWorld>().Build();

            _queryWithOffset = SystemAPI.QueryBuilder().WithAny<TagHearingReceiver, TagHearingSource>().WithAll<ComponentHearingOffset>().Build();
            _query = SystemAPI.QueryBuilder().WithAny<TagHearingReceiver, TagHearingSource>().WithNone<ComponentHearingOffset>().Build();

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

            foreach (var entity in _queryWithoutTransform.ToEntityArray(Allocator.Temp))
            {
                commands.AddComponent(entity, new LocalToWorld { Value = float4x4.identity });
            }

            commands.Playback(state.EntityManager);

            _handlePosition.Update(ref state);
            _handleOffset.Update(ref state);
            _handleTransform.Update(ref state);

            var ranges = new NativeArray<int2>(2, Allocator.Temp);

            if (SystemAPI.TryGetSingleton(out ComponentHearingLimit limit) && limit.ChunksAmountPosition > 0)
            {
                var amounts = new NativeArray<int>(ranges.Length, Allocator.Temp);
                amounts[0] = _queryWithOffset.CalculateChunkCountWithoutFiltering();
                amounts[1] = _query.CalculateChunkCountWithoutFiltering();

                ComponentHearingLimit.CalculateRanges(limit.ChunksAmountPosition, amounts.AsReadOnly(), ref ranges, ref _chunkIndexRange);
            }
            else
            {
                for (var i = 0; i < ranges.Length; i++)
                {
                    ranges[i] = new int2(0, int.MaxValue);
                }
            }

            var jobHandle = new JobUpdatePositionWithOffset
            {
                HandlePosition = _handlePosition, HandleTransform = _handleTransform, ChunkIndexRange = ranges[0],
                HandleOffset = _handleOffset,
            }.ScheduleParallel(_queryWithOffset, state.Dependency);

            state.Dependency = new JobUpdatePosition
            {
                HandlePosition = _handlePosition, HandleTransform = _handleTransform, ChunkIndexRange = ranges[1],
            }.ScheduleParallel(_query, jobHandle);
        }

        [BurstCompile]
        private struct JobUpdatePositionWithOffset : IJobChunk
        {
            public ComponentTypeHandle<ComponentHearingPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> HandleTransform;
            [ReadOnly] public int2 ChunkIndexRange;

            [ReadOnly] public ComponentTypeHandle<ComponentHearingOffset> HandleOffset;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (unfilteredChunkIndex < ChunkIndexRange.x || unfilteredChunkIndex >= ChunkIndexRange.y)
                {
                    return;
                }

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
            [ReadOnly] public int2 ChunkIndexRange;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (unfilteredChunkIndex < ChunkIndexRange.x || unfilteredChunkIndex >= ChunkIndexRange.y)
                {
                    return;
                }

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