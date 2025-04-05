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

            var buffersInsideCone = SystemAPI.GetBufferLookup<BufferSightCone>();
            var buffersPerceive = SystemAPI.GetBufferLookup<BufferSightPerceive>();

            foreach (var (transformRO, offsetRO, coneRO, clipRO, coneOffsetRO, extendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightOffset>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightClip>, RefRO<ComponentSightConeOffset>, RefRO<ComponentSightExtend>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Value.TransformPoint(offsetRO.ValueRO.Value + coneOffsetRO.ValueRO.Value),
                    coneRO.ValueRO, extendRO.ValueRO, clipRO.ValueRO.RadiusSquared,
                    buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, coneRO, clipRO, coneOffsetRO, extendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>, RefRO<ComponentSightClip>,
                             RefRO<ComponentSightConeOffset>, RefRO<ComponentSightExtend>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightOffset>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Value.TransformPoint(coneOffsetRO.ValueRO.Value),
                    coneRO.ValueRO, extendRO.ValueRO, clipRO.ValueRO.RadiusSquared,
                    buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, offsetRO, coneRO, coneOffsetRO, extendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightOffset>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightConeOffset>, RefRO<ComponentSightExtend>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightClip>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Value.TransformPoint(offsetRO.ValueRO.Value + coneOffsetRO.ValueRO.Value),
                    coneRO.ValueRO, extendRO.ValueRO, buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, coneRO, coneOffsetRO, extendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightConeOffset>, RefRO<ComponentSightExtend>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightClip, ComponentSightOffset>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Value.TransformPoint(coneOffsetRO.ValueRO.Value),
                    coneRO.ValueRO, extendRO.ValueRO, buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, positionRO, coneRO, clipRO, extendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightClip>, RefRO<ComponentSightExtend>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightConeOffset>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, positionRO.ValueRO.Value,
                    coneRO.ValueRO, extendRO.ValueRO, clipRO.ValueRO.RadiusSquared,
                    buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, coneRO, clipRO, extendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightClip>, RefRO<ComponentSightExtend>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightConeOffset, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, transformRO.ValueRO.Position,
                    coneRO.ValueRO, extendRO.ValueRO, clipRO.ValueRO.RadiusSquared,
                    buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, positionRO, coneRO, extendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>,
                             RefRO<ComponentSightCone>, RefRO<ComponentSightExtend>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightConeOffset, ComponentSightClip>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, positionRO.ValueRO.Value,
                    coneRO.ValueRO, extendRO.ValueRO, buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, coneRO, extendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>, RefRO<ComponentSightExtend>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightConeOffset, ComponentSightClip, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, transformRO.ValueRO.Position,
                    coneRO.ValueRO, extendRO.ValueRO, buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, offsetRO, coneRO, clipRO, coneOffsetRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightOffset>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightClip>, RefRO<ComponentSightConeOffset>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightExtend>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Value.TransformPoint(offsetRO.ValueRO.Value + coneOffsetRO.ValueRO.Value),
                    coneRO.ValueRO, clipRO.ValueRO.RadiusSquared, ref commands);
            }

            foreach (var (transformRO, coneRO, clipRO, coneOffsetRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightClip>, RefRO<ComponentSightConeOffset>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightExtend, ComponentSightOffset>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Value.TransformPoint(coneOffsetRO.ValueRO.Value),
                    coneRO.ValueRO, clipRO.ValueRO.RadiusSquared, ref commands);
            }

            foreach (var (transformRO, offsetRO, coneRO, coneOffsetRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightOffset>,
                             RefRO<ComponentSightCone>, RefRO<ComponentSightConeOffset>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightExtend, ComponentSightClip>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Value.TransformPoint(offsetRO.ValueRO.Value + coneOffsetRO.ValueRO.Value),
                    coneRO.ValueRO, ref commands);
            }

            foreach (var (transformRO, coneRO, coneOffsetRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>, RefRO<ComponentSightConeOffset>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightExtend, ComponentSightClip, ComponentSightOffset>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Value.TransformPoint(coneOffsetRO.ValueRO.Value),
                    coneRO.ValueRO, ref commands);
            }

            foreach (var (transformRO, positionRO, coneRO, clipRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>,
                             RefRO<ComponentSightCone>, RefRO<ComponentSightClip>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightExtend, ComponentSightConeOffset>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, positionRO.ValueRO.Value,
                    coneRO.ValueRO, clipRO.ValueRO.RadiusSquared, ref commands);
            }

            foreach (var (transformRO, coneRO, clipRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>, RefRO<ComponentSightClip>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightExtend, ComponentSightConeOffset, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, transformRO.ValueRO.Position,
                    coneRO.ValueRO, clipRO.ValueRO.RadiusSquared, ref commands);
            }

            foreach (var (transformRO, positionRO, coneRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>, RefRO<ComponentSightCone>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightExtend, ComponentSightConeOffset, ComponentSightClip>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    positionRO.ValueRO.Value, coneRO.ValueRO, ref commands);
            }

            foreach (var (transformRO, coneRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>>()
                         .WithAll<TagSightReceiver, BufferSightCone>()
                         .WithNone<ComponentSightExtend, ComponentSightConeOffset>()
                         .WithNone<ComponentSightClip, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Position, coneRO.ValueRO, ref commands);
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

            foreach (var (sourceTransformRO, source) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightSource>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                ProcessSource(ref state, receiver, in transform, position, cone, extend,
                    clipRadiusSquared, source, sourceTransformRO.ValueRO.Position,
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

            foreach (var (sourceTransformRO, source) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightSource>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                ProcessSource(ref state, receiver, in transform, position, cone,
                    extend, source, sourceTransformRO.ValueRO.Position,
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
                ProcessSource(ref state, receiver, in transform, position, cone,
                    source, sourcePositionRO.ValueRO.Value, ref commands);
            }

            foreach (var (sourceTransformRO, source) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightSource>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                ProcessSource(ref state, receiver, in transform, position, cone,
                    source, sourceTransformRO.ValueRO.Position, ref commands);
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