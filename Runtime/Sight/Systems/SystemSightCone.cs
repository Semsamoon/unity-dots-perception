using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Perception
{
    [UpdateAfter(typeof(SystemSightPosition))]
    public partial struct SystemSightCone : ISystem
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
                         .WithAll<TagSightReceiver, ComponentSightCone>()
                         .WithNone<BufferSightInsideCone>()
                         .Build()
                         .ToEntityArray(Allocator.Temp))
            {
                commands.AddBuffer<BufferSightInsideCone>(receiver);
            }

            commands.Playback(state.EntityManager);
            commands = new EntityCommandBuffer(Allocator.Temp);

            var buffersInsideCone = SystemAPI.GetBufferLookup<BufferSightInsideCone>();

            foreach (var (transformRO, positionRO, coneRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>, RefRO<ComponentSightCone>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithEntityAccess())
            {
                ref readonly var transform = ref transformRO.ValueRO;

                var position = positionRO.ValueRO.Value;
                var cone = coneRO.ValueRO;

                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transform, position, cone, ref commands);
            }

            foreach (var (transformRO, coneRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                ref readonly var transform = ref transformRO.ValueRO;

                var position = transform.Position;
                var cone = coneRO.ValueRO;

                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transform, position, cone, ref commands);
            }

            commands.Playback(state.EntityManager);
        }

        private void ProcessReceiver(ref SystemState state,
            Entity receiver, in LocalToWorld transform, float3 position,
            ComponentSightCone cone, ref EntityCommandBuffer commands)
        {
            foreach (var (sourcePositionRO, source) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightSource>()
                         .WithEntityAccess())
            {
                var sourcePosition = sourcePositionRO.ValueRO.Value;

                if (IsInsideCone(in transform, position, cone.AnglesTan, cone.RadiusSquared, sourcePosition))
                {
                    commands.AppendToBuffer(receiver, new BufferSightInsideCone
                    {
                        Source = source,
                        Position = sourcePosition,
                    });
                }
            }

            foreach (var (sourceTransformRO, source) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightSource>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                var sourcePosition = sourceTransformRO.ValueRO.Position;

                if (IsInsideCone(in transform, position, cone.AnglesTan, cone.RadiusSquared, sourcePosition))
                {
                    commands.AppendToBuffer(receiver, new BufferSightInsideCone
                    {
                        Source = source,
                        Position = sourcePosition,
                    });
                }
            }
        }

        private bool IsInsideCone(
            in LocalToWorld receiverTransform, float3 receiverPosition,
            float2 anglesTan, float radiusSquared, float3 sourcePosition)
        {
            var difference = sourcePosition - receiverPosition;
            var distanceSquared = math.lengthsq(difference);

            if (distanceSquared > radiusSquared)
            {
                return false;
            }

            var direction = difference * math.rsqrt(distanceSquared);
            var directionLocal = receiverTransform.Value.TransformDirection(direction);

            return directionLocal.x / directionLocal.z <= anglesTan.x
                   && directionLocal.y / directionLocal.z <= anglesTan.y;
        }
    }
}