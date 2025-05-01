using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace Perception
{
    [BurstCompile, UpdateInGroup(typeof(HearingSystemGroup))]
    public partial struct SystemHearingMemory : ISystem
    {
        private EntityQuery _queryWithoutMemory;

        private EntityQuery _query;

        private BufferTypeHandle<BufferHearingMemory> _handleBufferMemory;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _queryWithoutMemory = SystemAPI.QueryBuilder().WithAll<TagHearingReceiver, ComponentHearingMemory>().WithNone<BufferHearingMemory>().Build();

            _query = SystemAPI.QueryBuilder().WithAll<TagHearingReceiver, ComponentHearingMemory>().Build();

            _handleBufferMemory = SystemAPI.GetBufferTypeHandle<BufferHearingMemory>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commands = new EntityCommandBuffer(Allocator.Temp);

            foreach (var receiver in _queryWithoutMemory.ToEntityArray(Allocator.Temp))
            {
                commands.AddBuffer<BufferHearingMemory>(receiver);
            }

            commands.Playback(state.EntityManager);

            _handleBufferMemory.Update(ref state);

            state.Dependency = new JobUpdateMemory
            {
                HandleBufferMemory = _handleBufferMemory, DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel(_query, state.Dependency);
        }

        [BurstCompile]
        private struct JobUpdateMemory : IJobChunk
        {
            public BufferTypeHandle<BufferHearingMemory> HandleBufferMemory;
            [ReadOnly] public float DeltaTime;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var buffersMemory = chunk.GetBufferAccessor(ref HandleBufferMemory);

                for (var i = 0; i < chunk.Count; i++)
                {
                    var bufferMemory = buffersMemory[i];

                    for (var j = bufferMemory.Length - 1; j >= 0; j--)
                    {
                        bufferMemory.ElementAt(j).Time -= DeltaTime;

                        if (bufferMemory[j].Time <= 0)
                        {
                            bufferMemory.RemoveAtSwapBack(j);
                        }
                    }
                }
            }
        }
    }
}