using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace Perception
{
    [BurstCompile]
    public partial struct SystemSightMemory : ISystem
    {
        private EntityQuery _queryWithoutBuffer;
        private EntityQuery _query;

        private BufferTypeHandle<BufferSightMemory> _handleBufferMemory;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _queryWithoutBuffer = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, ComponentSightMemory>()
                .WithNone<BufferSightMemory>()
                .Build();
            _query = SystemAPI.QueryBuilder()
                .WithAll<TagSightReceiver, BufferSightMemory>()
                .Build();

            _handleBufferMemory = SystemAPI.GetBufferTypeHandle<BufferSightMemory>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commands = new EntityCommandBuffer(Allocator.Temp);

            foreach (var receiver in _queryWithoutBuffer.ToEntityArray(Allocator.Temp))
            {
                commands.AddBuffer<BufferSightMemory>(receiver);
            }

            commands.Playback(state.EntityManager);

            _handleBufferMemory.Update(ref state);

            var job = new JobUpdateMemory
            {
                HandleBufferMemory = _handleBufferMemory,
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel(_query, state.Dependency);

            state.Dependency = job;
        }

        [BurstCompile]
        private struct JobUpdateMemory : IJobChunk
        {
            public BufferTypeHandle<BufferSightMemory> HandleBufferMemory;
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