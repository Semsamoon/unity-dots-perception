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

            foreach (var (transformRO, positionRO, coneRO, coneClipRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>,
                             RefRO<ComponentSightCone>, RefRO<ComponentSightConeClip>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, positionRO.ValueRO.Value,
                    coneRO.ValueRO, coneClipRO.ValueRO.RadiusSquared, ref commands);
            }

            foreach (var (transformRO, coneRO, coneClipRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>, RefRO<ComponentSightConeClip>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, transformRO.ValueRO.Position,
                    coneRO.ValueRO, coneClipRO.ValueRO.RadiusSquared, ref commands);
            }

            foreach (var (transformRO, positionRO, coneRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>, RefRO<ComponentSightCone>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightConeClip>()
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
                         .WithNone<ComponentSightConeClip, ComponentSightPosition>()
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
            ComponentSightCone cone, float clipRadiusSquared,
            ref EntityCommandBuffer commands)
        {
            foreach (var (sourcePositionRO, source) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightSource>()
                         .WithEntityAccess())
            {
                ProcessSource(ref state, receiver, in transform, position, cone, clipRadiusSquared,
                    source, sourcePositionRO.ValueRO.Value, ref commands);
            }

            foreach (var (sourceTransformRO, source) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightSource>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                ProcessSource(ref state, receiver, in transform, position, cone, clipRadiusSquared,
                    source, sourceTransformRO.ValueRO.Position, ref commands);
            }
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

        private void ProcessSource(ref SystemState state,
            Entity receiver, in LocalToWorld transform, float3 position,
            ComponentSightCone cone, float clipRadiusSquared,
            Entity source, float3 sourcePosition,
            ref EntityCommandBuffer commands)
        {
            if (IsInsideCone(in transform, position, cone.AnglesTan,
                    clipRadiusSquared, cone.RadiusSquared, sourcePosition))
            {
                commands.AppendToBuffer(receiver, new BufferSightInsideCone
                {
                    Source = source,
                    Position = sourcePosition,
                });
            }
        }

        private bool IsInsideCone(
            in LocalToWorld receiverTransform, float3 receiverPosition,
            float2 anglesTan, float nearRadiusSquared, float farRadiusSquared, float3 sourcePosition)
        {
            var difference = sourcePosition - receiverPosition;
            var distanceSquared = math.lengthsq(difference);

            if (distanceSquared > farRadiusSquared || distanceSquared < nearRadiusSquared)
            {
                return false;
            }

            var direction = difference * math.rsqrt(distanceSquared);
            var directionLocal = receiverTransform.Value.TransformDirection(direction);

            return directionLocal.x / directionLocal.z <= anglesTan.x
                   && directionLocal.y / directionLocal.z <= anglesTan.y;
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