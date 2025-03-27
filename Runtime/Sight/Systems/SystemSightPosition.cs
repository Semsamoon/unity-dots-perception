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
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                commands.AddComponent(entity, new ComponentSightPosition
                {
                    Value = transformRO.ValueRO.Value.TransformPoint(in offsetRO.ValueRO.Value),
                });
            }

            foreach (var (positionRW, transformRO) in SystemAPI
                         .Query<RefRW<ComponentSightPosition>, RefRO<LocalToWorld>>()
                         .WithNone<ComponentSightOffset>())
            {
                positionRW.ValueRW.Value = transformRO.ValueRO.Position;
            }

            foreach (var (positionRW, transformRO, offsetRO) in SystemAPI
                         .Query<RefRW<ComponentSightPosition>, RefRO<LocalToWorld>, RefRO<ComponentSightOffset>>())
            {
                positionRW.ValueRW.Value = transformRO.ValueRO.Value.TransformPoint(in offsetRO.ValueRO.Value);
            }

            commands.Playback(state.EntityManager);
        }
    }
}