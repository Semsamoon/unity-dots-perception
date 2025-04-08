using Unity.Collections;
using Unity.Entities;

namespace Perception
{
    public partial struct SystemSightMemory : ISystem
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

            foreach (var receiver in SystemAPI
                         .QueryBuilder()
                         .WithAll<TagSightReceiver, ComponentSightMemory>()
                         .WithNone<BufferSightMemory>()
                         .Build()
                         .ToEntityArray(Allocator.Temp))
            {
                commands.AddBuffer<BufferSightMemory>(receiver);
            }

            var buffersMemory = SystemAPI.GetBufferLookup<BufferSightMemory>();

            foreach (var receiver in SystemAPI
                         .QueryBuilder()
                         .WithAll<TagSightReceiver, BufferSightMemory>()
                         .Build()
                         .ToEntityArray(Allocator.Temp))
            {
                var bufferMemory = buffersMemory[receiver];

                for (var i = bufferMemory.Length - 1; i >= 0; i--)
                {
                    bufferMemory.ElementAt(i).Time -= SystemAPI.Time.DeltaTime;

                    if (bufferMemory[i].Time <= 0)
                    {
                        bufferMemory.RemoveAt(i);
                    }
                }
            }

            commands.Playback(state.EntityManager);
        }
    }
}