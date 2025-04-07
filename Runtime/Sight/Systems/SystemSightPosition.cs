using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Perception
{
    public partial struct SystemSightPosition : ISystem
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

            foreach (var (transformRO, offsetRO, entity) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightOffset>>()
                         .WithAll<TagSightReceiver, TagSightSource>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                commands.AddComponent(entity, new ComponentSightPosition
                {
                    Receiver = transformRO.ValueRO.Value.TransformPoint(in offsetRO.ValueRO.Receiver),
                    Source = transformRO.ValueRO.Value.TransformPoint(in offsetRO.ValueRO.Source),
                });
            }

            foreach (var (transformRO, offsetRO, entity) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightOffset>>()
                         .WithAll<TagSightSource>()
                         .WithNone<ComponentSightPosition, TagSightReceiver>()
                         .WithEntityAccess())
            {
                commands.AddComponent(entity, new ComponentSightPosition
                {
                    Source = transformRO.ValueRO.Value.TransformPoint(in offsetRO.ValueRO.Source),
                });
            }

            foreach (var (transformRO, offsetRO, entity) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightOffset>>()
                         .WithAll<TagSightReceiver>()
                         .WithNone<ComponentSightPosition, TagSightSource>()
                         .WithEntityAccess())
            {
                commands.AddComponent(entity, new ComponentSightPosition
                {
                    Receiver = transformRO.ValueRO.Value.TransformPoint(in offsetRO.ValueRO.Receiver),
                });
            }

            foreach (var (transformRO, entity) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAny<TagSightReceiver, TagSightSource>()
                         .WithNone<ComponentSightPosition, ComponentSightOffset>()
                         .WithEntityAccess())
            {
                commands.AddComponent(entity, new ComponentSightPosition
                {
                    Receiver = transformRO.ValueRO.Position,
                    Source = transformRO.ValueRO.Position,
                });
            }

            foreach (var (positionRW, transformRO, offsetRO) in SystemAPI
                         .Query<RefRW<ComponentSightPosition>, RefRO<LocalToWorld>, RefRO<ComponentSightOffset>>()
                         .WithAll<TagSightReceiver, TagSightSource>())
            {
                positionRW.ValueRW.Receiver = transformRO.ValueRO.Value.TransformPoint(in offsetRO.ValueRO.Receiver);
                positionRW.ValueRW.Source = transformRO.ValueRO.Value.TransformPoint(in offsetRO.ValueRO.Source);
            }

            foreach (var (positionRW, transformRO, offsetRO) in SystemAPI
                         .Query<RefRW<ComponentSightPosition>, RefRO<LocalToWorld>, RefRO<ComponentSightOffset>>()
                         .WithAll<TagSightSource>()
                         .WithNone<TagSightReceiver>())
            {
                positionRW.ValueRW.Source = transformRO.ValueRO.Value.TransformPoint(in offsetRO.ValueRO.Source);
            }

            foreach (var (positionRW, transformRO, offsetRO) in SystemAPI
                         .Query<RefRW<ComponentSightPosition>, RefRO<LocalToWorld>, RefRO<ComponentSightOffset>>()
                         .WithAll<TagSightReceiver>()
                         .WithNone<TagSightSource>())
            {
                positionRW.ValueRW.Receiver = transformRO.ValueRO.Value.TransformPoint(in offsetRO.ValueRO.Receiver);
            }

            foreach (var (positionRW, transformRO) in SystemAPI
                         .Query<RefRW<ComponentSightPosition>, RefRO<LocalToWorld>>()
                         .WithAny<TagSightReceiver, TagSightSource>()
                         .WithNone<ComponentSightOffset>())
            {
                positionRW.ValueRW.Receiver = transformRO.ValueRO.Position;
                positionRW.ValueRW.Source = transformRO.ValueRO.Position;
            }

            commands.Playback(state.EntityManager);
        }
    }
}