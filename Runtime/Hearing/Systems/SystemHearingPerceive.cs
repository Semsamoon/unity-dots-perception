using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    [BurstCompile, UpdateInGroup(typeof(HearingSystemGroup)), UpdateAfter(typeof(SystemHearingSphere)), UpdateAfter(typeof(SystemHearingMemory))]
    public partial struct SystemHearingPerceive : ISystem
    {
        private EntityQuery _queryWithoutPerceive;
        private EntityQuery _queryWithoutFilter;

        private EntityQuery _querySources;

        private EntityQuery _queryWithMemory;
        private EntityQuery _query;

        private BufferTypeHandle<BufferHearingPerceive> _handleBufferPerceive;
        private BufferTypeHandle<BufferHearingMemory> _handleBufferMemory;

        private ComponentTypeHandle<ComponentHearingPosition> _handlePosition;
        private ComponentTypeHandle<ComponentHearingMemory> _handleMemory;
        private ComponentTypeHandle<ComponentHearingFilter> _handleFilter;

        private ComponentLookup<ComponentHearingPosition> _lookupPosition;
        private ComponentLookup<ComponentHearingRadius> _lookupRadius;
        private ComponentLookup<ComponentHearingFilter> _lookupFilter;

        private int2 _chunkIndexRange;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _queryWithoutPerceive = SystemAPI.QueryBuilder().WithAll<TagHearingReceiver>().WithNone<BufferHearingPerceive>().Build();
            _queryWithoutFilter = SystemAPI.QueryBuilder().WithAny<TagHearingReceiver, TagHearingSource>().WithNone<ComponentHearingFilter>().Build();

            _querySources = SystemAPI.QueryBuilder().WithAll<TagHearingSource>().Build();

            _queryWithMemory = SystemAPI.QueryBuilder().WithAll<TagHearingReceiver>().WithAll<ComponentHearingMemory>().Build();
            _query = SystemAPI.QueryBuilder().WithAll<TagHearingReceiver>().WithNone<ComponentHearingMemory>().Build();

            _handleBufferPerceive = SystemAPI.GetBufferTypeHandle<BufferHearingPerceive>();
            _handleBufferMemory = SystemAPI.GetBufferTypeHandle<BufferHearingMemory>();

            _handlePosition = SystemAPI.GetComponentTypeHandle<ComponentHearingPosition>(isReadOnly: true);
            _handleMemory = SystemAPI.GetComponentTypeHandle<ComponentHearingMemory>(isReadOnly: true);
            _handleFilter = SystemAPI.GetComponentTypeHandle<ComponentHearingFilter>(isReadOnly: true);

            _lookupPosition = SystemAPI.GetComponentLookup<ComponentHearingPosition>(isReadOnly: true);
            _lookupRadius = SystemAPI.GetComponentLookup<ComponentHearingRadius>(isReadOnly: true);
            _lookupFilter = SystemAPI.GetComponentLookup<ComponentHearingFilter>(isReadOnly: true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commands = new EntityCommandBuffer(Allocator.Temp);

            foreach (var receiver in _queryWithoutPerceive.ToEntityArray(Allocator.Temp))
            {
                commands.AddBuffer<BufferHearingPerceive>(receiver);
            }

            foreach (var entity in _queryWithoutFilter.ToEntityArray(Allocator.Temp))
            {
                commands.AddComponent(entity, new ComponentHearingFilter { BelongsTo = uint.MaxValue, Perceives = uint.MaxValue });
            }

            commands.Playback(state.EntityManager);

            _handleBufferPerceive.Update(ref state);
            _handleBufferMemory.Update(ref state);

            _handlePosition.Update(ref state);
            _handleMemory.Update(ref state);
            _handleFilter.Update(ref state);

            _lookupPosition.Update(ref state);
            _lookupRadius.Update(ref state);
            _lookupFilter.Update(ref state);

            var sources = _querySources.ToEntityArray(Allocator.TempJob);

            var commonHandles = new CommonHandles
            {
                HandlePosition = _handlePosition, HandleFilter = _handleFilter,
                LookupPosition = _lookupPosition, LookupRadius = _lookupRadius, LookupFilter = _lookupFilter,
                Sources = sources.AsReadOnly(),
            };

            var ranges = new NativeArray<int2>(2, Allocator.Temp);

            if (SystemAPI.TryGetSingleton(out ComponentHearingLimit limit) && limit.ChunksAmountPerceive > 0)
            {
                var amounts = new NativeArray<int>(ranges.Length, Allocator.Temp);
                amounts[0] = _queryWithMemory.CalculateChunkCountWithoutFiltering();
                amounts[1] = _query.CalculateChunkCountWithoutFiltering();

                ComponentHearingLimit.CalculateRanges(limit.ChunksAmountPerceive, amounts.AsReadOnly(), ref ranges, ref _chunkIndexRange);
            }
            else
            {
                for (var i = 0; i < ranges.Length; i++)
                {
                    ranges[i] = new int2(0, int.MaxValue);
                }
            }

            var jobHandle = new JobUpdatePerceiveWithMemory
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles, ChunkIndexRange = ranges[0],
                HandleBufferMemory = _handleBufferMemory, HandleMemory = _handleMemory,
            }.ScheduleParallel(_queryWithMemory, state.Dependency);

            jobHandle = new JobUpdatePerceive
            {
                HandleBufferPerceive = _handleBufferPerceive, CommonHandles = commonHandles, ChunkIndexRange = ranges[0],
            }.ScheduleParallel(_query, jobHandle);

            state.Dependency = sources.Dispose(jobHandle);
        }

        [BurstCompile]
        private struct JobUpdatePerceiveWithMemory : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferHearingPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;
            [ReadOnly] public int2 ChunkIndexRange;

            [WriteOnly] public BufferTypeHandle<BufferHearingMemory> HandleBufferMemory;
            [ReadOnly] public ComponentTypeHandle<ComponentHearingMemory> HandleMemory;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (unfilteredChunkIndex < ChunkIndexRange.x || unfilteredChunkIndex >= ChunkIndexRange.y)
                {
                    return;
                }

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);
                var memories = chunk.GetNativeArray(ref HandleMemory);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var position, out var filter);

                    var bufferMemory = buffersMemory[i];
                    var memory = memories[i];
                    var perceiveLength = bufferPerceive.Length;

                    foreach (var source in CommonHandles.Sources)
                    {
                        if (!filter.CanPerceive(CommonHandles.LookupFilter[source]))
                        {
                            continue;
                        }

                        var radius = CommonHandles.LookupRadius[source];
                        var isPerceived = bufferPerceive.Contains(in source, perceiveLength, out var index, out var perceive);

                        if (isPerceived)
                        {
                            bufferPerceive.RemoveAtSwapBack(index, --perceiveLength);
                        }

                        var sourcePosition = CommonHandles.LookupPosition[source];
                        var distanceCurrentSquared = math.distancesq(position.Current, sourcePosition.Current);
                        var distancePreviousSquared = math.distancesq(position.Previous, sourcePosition.Previous);

                        if (radius.IsInside(distanceCurrentSquared, distancePreviousSquared))
                        {
                            bufferPerceive.Add(new BufferHearingPerceive { Position = sourcePosition.Current, Source = source });
                            bufferMemory.Remove(source);
                            continue;
                        }

                        if (isPerceived)
                        {
                            bufferMemory.Add(new BufferHearingMemory { Position = perceive.Position, Source = perceive.Source, Time = memory.Time });
                        }
                    }

                    bufferPerceive.ToMemories(perceiveLength, ref bufferMemory, memory.Time);
                }
            }
        }

        [BurstCompile]
        private struct JobUpdatePerceive : IJobChunk
        {
            [WriteOnly] public BufferTypeHandle<BufferHearingPerceive> HandleBufferPerceive;
            [ReadOnly] public CommonHandles CommonHandles;
            [ReadOnly] public int2 ChunkIndexRange;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (unfilteredChunkIndex < ChunkIndexRange.x || unfilteredChunkIndex >= ChunkIndexRange.y)
                {
                    return;
                }

                var buffersPerceive = chunk.GetBufferAccessor(ref HandleBufferPerceive);
                CommonHandles.Get(in chunk, out var arrays);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferPerceive = buffersPerceive[i];
                    arrays.Get(i, out var position, out var filter);

                    bufferPerceive.Clear();

                    foreach (var source in CommonHandles.Sources)
                    {
                        if (!filter.CanPerceive(CommonHandles.LookupFilter[source]))
                        {
                            continue;
                        }

                        var radius = CommonHandles.LookupRadius[source];
                        var sourcePosition = CommonHandles.LookupPosition[source];
                        var distanceCurrentSquared = math.distancesq(position.Current, sourcePosition.Current);
                        var distancePreviousSquared = math.distancesq(position.Previous, sourcePosition.Previous);

                        if (radius.IsInside(distanceCurrentSquared, distancePreviousSquared))
                        {
                            bufferPerceive.Add(new BufferHearingPerceive { Position = sourcePosition.Current, Source = source });
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct CommonHandles
        {
            public ComponentTypeHandle<ComponentHearingPosition> HandlePosition;
            public ComponentTypeHandle<ComponentHearingFilter> HandleFilter;

            public ComponentLookup<ComponentHearingPosition> LookupPosition;
            public ComponentLookup<ComponentHearingRadius> LookupRadius;
            public ComponentLookup<ComponentHearingFilter> LookupFilter;

            public NativeArray<Entity>.ReadOnly Sources;

            [BurstCompile]
            public void Get(in ArchetypeChunk chunk, out CommonArrays arrays)
            {
                arrays = new CommonArrays
                {
                    Positions = chunk.GetNativeArray(ref HandlePosition),
                    Filters = chunk.GetNativeArray(ref HandleFilter),
                };
            }
        }

        [BurstCompile]
        private struct CommonArrays
        {
            public NativeArray<ComponentHearingPosition> Positions;
            public NativeArray<ComponentHearingFilter> Filters;

            [BurstCompile]
            public void Get(int index, out ComponentHearingPosition position, out ComponentHearingFilter filter)
            {
                position = Positions[index];
                filter = Filters[index];
            }
        }
    }
}