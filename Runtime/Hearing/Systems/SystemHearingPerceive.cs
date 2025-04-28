using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Perception
{
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

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentHearingPosition>>()
                         .WithAll<TagHearingReceiver>()
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