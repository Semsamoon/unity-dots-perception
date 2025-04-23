using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Perception
{
    [BurstCompile, UpdateInGroup(typeof(SightSystemGroup))]
    public partial struct SystemSightPosition : ISystem
    {
        private EntityQuery _queryWithoutPosition;
        private EntityQuery _queryWithoutTransform;

        private EntityQuery _queryWithOffset;
        private EntityQuery _query;

        private ComponentTypeHandle<ComponentSightPosition> _handlePosition;
        private ComponentTypeHandle<ComponentSightOffset> _handleOffset;
        private ComponentTypeHandle<LocalToWorld> _handleTransform;

        private int2 _chunkIndexRange;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _queryWithoutPosition = SystemAPI.QueryBuilder().WithAny<TagSightReceiver, TagSightSource>().WithNone<ComponentSightPosition>().Build();
            _queryWithoutTransform = SystemAPI.QueryBuilder().WithAny<TagSightReceiver, TagSightSource>().WithNone<LocalToWorld>().Build();

            _queryWithOffset = SystemAPI.QueryBuilder().WithAny<TagSightReceiver, TagSightSource>().WithAll<ComponentSightOffset>().Build();
            _query = SystemAPI.QueryBuilder().WithAny<TagSightReceiver, TagSightSource>().WithNone<ComponentSightOffset>().Build();

            _handlePosition = SystemAPI.GetComponentTypeHandle<ComponentSightPosition>();
            _handleOffset = SystemAPI.GetComponentTypeHandle<ComponentSightOffset>(isReadOnly: true);
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
                commands.AddComponent(entity, new ComponentSightPosition());
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

            if (SystemAPI.TryGetSingleton(out ComponentSightLimit limit) && limit.ChunksAmountPosition > 0)
            {
                var amounts = new NativeArray<int>(ranges.Length, Allocator.Temp);
                amounts[0] = _queryWithOffset.CalculateChunkCountWithoutFiltering();
                amounts[1] = _query.CalculateChunkCountWithoutFiltering();

                ComponentSightLimit.CalculateRanges(limit.ChunksAmountPosition, amounts.AsReadOnly(), ref ranges, ref _chunkIndexRange);
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
            public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> HandleTransform;
            [ReadOnly] public int2 ChunkIndexRange;

            [ReadOnly] public ComponentTypeHandle<ComponentSightOffset> HandleOffset;

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
                    var transform = transforms[i];

                    var offset = offsets[i];

                    positions[i] = new ComponentSightPosition
                    {
                        Receiver = transform.Value.TransformPoint(in offset.Receiver),
                        Source = transform.Value.TransformPoint(in offset.Source),
                    };
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePosition : IJobChunk
        {
            public ComponentTypeHandle<ComponentSightPosition> HandlePosition;
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
                    var position = transforms[i].Position;
                    positions[i] = new ComponentSightPosition { Receiver = position, Source = position };
                }
            }
        }
    }
}