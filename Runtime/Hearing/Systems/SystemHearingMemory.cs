using Unity.Collections;
using Unity.Entities;

namespace Perception
{
    [UpdateInGroup(typeof(HearingSystemGroup))]
    public partial struct SystemHearingMemory : ISystem
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

            foreach (var receiver in SystemAPI.QueryBuilder()
                         .WithAll<TagHearingReceiver, ComponentHearingMemory>()
                         .WithNone<BufferHearingMemory>()
                         .Build().ToEntityArray(Allocator.Temp))
            {
                commands.AddBuffer<BufferHearingMemory>(receiver);
            }

            commands.Playback(state.EntityManager);

            var buffersMemory = SystemAPI.GetBufferLookup<BufferHearingMemory>();
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var receiver in SystemAPI.QueryBuilder()
                         .WithAll<TagHearingReceiver, BufferHearingMemory>()
                         .Build().ToEntityArray(Allocator.Temp))
            {
                var bufferMemory = buffersMemory[receiver];

                for (var j = bufferMemory.Length - 1; j >= 0; j--)
                {
                    bufferMemory.ElementAt(j).Time -= deltaTime;

                    if (bufferMemory[j].Time <= 0)
                    {
                        bufferMemory.RemoveAtSwapBack(j);
                    }
                }
            }
        }
    }
}