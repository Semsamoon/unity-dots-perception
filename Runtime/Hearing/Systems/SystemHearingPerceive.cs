using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
    [UpdateAfter(typeof(SystemHearingSphere)), UpdateAfter(typeof(SystemHearingMemory))]
    public partial struct SystemHearingPerceive : ISystem
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
                         .WithAll<TagHearingReceiver>()
                         .WithNone<BufferHearingPerceive>()
                         .Build().ToEntityArray(Allocator.Temp))
            {
                commands.AddBuffer<BufferHearingPerceive>(receiver);
            }

            commands.Playback(state.EntityManager);

            var buffersPerceive = SystemAPI.GetBufferLookup<BufferHearingPerceive>();
            var buffersMemory = SystemAPI.GetBufferLookup<BufferHearingMemory>();

            foreach (var (positionRO, memoryRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentHearingPosition>, RefRO<ComponentHearingMemory>>()
                         .WithAll<TagHearingReceiver>()
                         .WithEntityAccess())
            {
                var bufferPerceive = buffersPerceive[receiver];
                var bufferMemory = buffersMemory[receiver];
                ref readonly var position = ref positionRO.ValueRO;

                var perceiveLength = bufferPerceive.Length;

                foreach (var (sourcePositionRO, radiusRO, source) in SystemAPI
                             .Query<RefRO<ComponentHearingPosition>, RefRO<ComponentHearingRadius>>()
                             .WithAll<TagHearingSource>()
                             .WithEntityAccess())
                {
                    var isPerceived = bufferPerceive.Contains(in source, perceiveLength, out var index, out var perceive);

                    if (isPerceived)
                    {
                        bufferPerceive.RemoveAtSwapBack(index, --perceiveLength);
                    }

                    ref readonly var sourcePosition = ref sourcePositionRO.ValueRO;
                    ref readonly var radius = ref radiusRO.ValueRO;

                    var distanceCurrentSquared = math.distancesq(position.Current, sourcePosition.Current);
                    var distancePreviousSquared = math.distancesq(position.Previous, sourcePosition.Previous);

                    if ((distanceCurrentSquared <= radius.CurrentSquared && distanceCurrentSquared >= radius.InternalCurrentSquared)
                        || (distanceCurrentSquared > radius.CurrentSquared && distancePreviousSquared < radius.InternalPreviousSquared)
                        || (distanceCurrentSquared < radius.InternalCurrentSquared && distancePreviousSquared > radius.PreviousSquared))
                    {
                        bufferPerceive.Add(new BufferHearingPerceive
                        {
                            Position = sourcePosition.Current,
                            Source = source,
                        });
                        bufferMemory.Remove(source);
                        continue;
                    }

                    if (isPerceived)
                    {
                        bufferMemory.Add(new BufferHearingMemory { Position = perceive.Position, Source = perceive.Source, Time = memoryRO.ValueRO.Time });
                    }
                }

                bufferPerceive.ToMemories(perceiveLength, ref bufferMemory, memoryRO.ValueRO.Time);
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentHearingPosition>>()
                         .WithAll<TagHearingReceiver>()
                         .WithNone<ComponentHearingMemory>()
                         .WithEntityAccess())
            {
                var bufferPerceive = buffersPerceive[receiver];
                ref readonly var position = ref positionRO.ValueRO;

                bufferPerceive.Clear();

                foreach (var (sourcePositionRO, radiusRO, source) in SystemAPI
                             .Query<RefRO<ComponentHearingPosition>, RefRO<ComponentHearingRadius>>()
                             .WithAll<TagHearingSource>()
                             .WithEntityAccess())
                {
                    ref readonly var sourcePosition = ref sourcePositionRO.ValueRO;
                    ref readonly var radius = ref radiusRO.ValueRO;

                    var distanceCurrentSquared = math.distancesq(position.Current, sourcePosition.Current);
                    var distancePreviousSquared = math.distancesq(position.Previous, sourcePosition.Previous);

                    if ((distanceCurrentSquared <= radius.CurrentSquared && distanceCurrentSquared >= radius.InternalCurrentSquared)
                        || (distanceCurrentSquared > radius.CurrentSquared && distancePreviousSquared < radius.InternalPreviousSquared)
                        || (distanceCurrentSquared < radius.InternalCurrentSquared && distancePreviousSquared > radius.PreviousSquared))
                    {
                        bufferPerceive.Add(new BufferHearingPerceive
                        {
                            Position = sourcePosition.Current,
                            Source = source,
                        });
                    }
                }
            }
        }
    }
}