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
                         .WithNone<BufferSightCone>()
                         .Build()
                         .ToEntityArray(Allocator.Temp))
            {
                commands.AddBuffer<BufferSightCone>(receiver);
            }

            commands.Playback(state.EntityManager);
            commands = new EntityCommandBuffer(Allocator.Temp);

            var buffersCone = SystemAPI.GetBufferLookup<BufferSightCone>();
            var buffersPerceive = SystemAPI.GetBufferLookup<BufferSightPerceive>();

            foreach (var (transformRO, positionRO, coneRO, clipRO, extendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightClip>, RefRO<ComponentSightExtend>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithEntityAccess())
            {
                buffersCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, positionRO.ValueRO.Value,
                    coneRO.ValueRO, extendRO.ValueRO, clipRO.ValueRO.RadiusSquared,
                    buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, positionRO, coneRO, extendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>,
                             RefRO<ComponentSightCone>, RefRO<ComponentSightExtend>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightClip>()
                         .WithEntityAccess())
            {
                buffersCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, positionRO.ValueRO.Value,
                    coneRO.ValueRO, extendRO.ValueRO, buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, positionRO, coneRO, clipRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>,
                             RefRO<ComponentSightCone>, RefRO<ComponentSightClip>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightExtend>()
                         .WithEntityAccess())
            {
                buffersCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, positionRO.ValueRO.Value,
                    coneRO.ValueRO, clipRO.ValueRO.RadiusSquared, ref commands);
            }

            foreach (var (transformRO, positionRO, coneRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>, RefRO<ComponentSightCone>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightExtend, ComponentSightClip>()
                         .WithEntityAccess())
            {
                buffersCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    positionRO.ValueRO.Value, coneRO.ValueRO, ref commands);
            }

            commands.Playback(state.EntityManager);
        }

        private void ProcessReceiver(ref SystemState state,
            Entity receiver, in LocalToWorld transform, float3 position,
            ComponentSightCone cone, ComponentSightExtend extend, float clipRadiusSquared,
            DynamicBuffer<BufferSightPerceive> bufferPerceive, ref EntityCommandBuffer commands)
        {
            foreach (var (sourcePositionRO, source) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightSource>()
                         .WithEntityAccess())
            {
                ProcessSource(ref state, receiver, in transform, position, cone, extend,
                    clipRadiusSquared, source, sourcePositionRO.ValueRO.Value,
                    bufferPerceive, ref commands);
            }
        }

        private void ProcessReceiver(ref SystemState state,
            Entity receiver, in LocalToWorld transform, float3 position,
            ComponentSightCone cone, ComponentSightExtend extend,
            DynamicBuffer<BufferSightPerceive> bufferPerceive, ref EntityCommandBuffer commands)
        {
            foreach (var (sourcePositionRO, source) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightSource>()
                         .WithEntityAccess())
            {
                ProcessSource(ref state, receiver, in transform, position, cone,
                    extend, source, sourcePositionRO.ValueRO.Value,
                    bufferPerceive, ref commands);
            }
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
                ProcessSource(ref state, receiver, in transform, position, cone,
                    source, sourcePositionRO.ValueRO.Value, ref commands);
            }
        }

        private void ProcessSource(ref SystemState state,
            Entity receiver, in LocalToWorld transform, float3 position,
            ComponentSightCone cone, ComponentSightExtend extend, float clipRadiusSquared,
            Entity source, float3 sourcePosition, DynamicBuffer<BufferSightPerceive> bufferPerceive,
            ref EntityCommandBuffer commands)
        {
            var (anglesTan, radiusSquared) = PickCone(bufferPerceive, source, cone, extend);

            if (IsInsideCone(in transform, position, anglesTan, clipRadiusSquared, radiusSquared, sourcePosition))
            {
                commands.AppendToBuffer(receiver, new BufferSightCone
                {
                    Source = source,
                    Position = sourcePosition,
                });
            }
        }

        private void ProcessSource(ref SystemState state,
            Entity receiver, in LocalToWorld transform, float3 position, ComponentSightCone cone,
            ComponentSightExtend extend, Entity source, float3 sourcePosition,
            DynamicBuffer<BufferSightPerceive> bufferPerceive, ref EntityCommandBuffer commands)
        {
            var (anglesTan, radiusSquared) = PickCone(bufferPerceive, source, cone, extend);

            if (IsInsideCone(in transform, position, anglesTan, radiusSquared, sourcePosition))
            {
                commands.AppendToBuffer(receiver, new BufferSightCone
                {
                    Source = source,
                    Position = sourcePosition,
                });
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
                commands.AppendToBuffer(receiver, new BufferSightCone
                {
                    Source = source,
                    Position = sourcePosition,
                });
            }
        }

        private void ProcessSource(ref SystemState state,
            Entity receiver, in LocalToWorld transform, float3 position,
            ComponentSightCone cone, Entity source, float3 sourcePosition,
            ref EntityCommandBuffer commands)
        {
            if (IsInsideCone(in transform, position, cone.AnglesTan, cone.RadiusSquared, sourcePosition))
            {
                commands.AppendToBuffer(receiver, new BufferSightCone
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

        private (float2 anglesTan, float radiusSquared) PickCone(
            DynamicBuffer<BufferSightPerceive> bufferPerceive, Entity source,
            ComponentSightCone cone, ComponentSightExtend extend)
        {
            foreach (var perceive in bufferPerceive)
            {
                if (perceive.Source == source)
                {
                    return (extend.AnglesTan, extend.RadiusSquared);
                }
            }

            return (cone.AnglesTan, cone.RadiusSquared);
        }
    }
}