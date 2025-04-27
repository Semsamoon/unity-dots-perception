using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Perception
{
    public partial struct SystemHearingPosition : ISystem
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

            foreach (var entity in SystemAPI.QueryBuilder()
                         .WithAny<TagHearingReceiver, TagHearingSource>()
                         .WithNone<ComponentHearingPosition>()
                         .Build().ToEntityArray(Allocator.Temp))
            {
                commands.AddComponent(entity, new ComponentHearingPosition());
            }

            foreach (var receiver in SystemAPI.QueryBuilder()
                         .WithAny<TagHearingReceiver>()
                         .WithNone<LocalToWorld>()
                         .Build().ToEntityArray(Allocator.Temp))
            {
                commands.AddComponent(receiver, new LocalToWorld { Value = float4x4.identity });
            }

            commands.Playback(state.EntityManager);

            foreach (var (positionRW, transformRO, offsetRO) in SystemAPI
                         .Query<RefRW<ComponentHearingPosition>, RefRO<LocalToWorld>, RefRO<ComponentHearingOffset>>()
                         .WithAny<TagHearingReceiver, TagHearingSource>())
            {
                ref var position = ref positionRW.ValueRW;
                ref readonly var transform = ref transformRO.ValueRO;
                ref readonly var offset = ref offsetRO.ValueRO;

                position.Previous = position.Current;
                position.Current = transform.Value.TransformPoint(in offset.Value);
            }

            foreach (var (positionRW, transformRO) in SystemAPI
                         .Query<RefRW<ComponentHearingPosition>, RefRO<LocalToWorld>>()
                         .WithAny<TagHearingReceiver, TagHearingSource>()
                         .WithNone<ComponentHearingOffset>())
            {
                ref var position = ref positionRW.ValueRW;
                ref readonly var transform = ref transformRO.ValueRO;

                position.Previous = position.Current;
                position.Current = transform.Position;
            }
        }
    }
}