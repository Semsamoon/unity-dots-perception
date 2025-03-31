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
            var buffersPerceive = SystemAPI.GetBufferLookup<BufferSightPerceive>();

            foreach (var (transformRO, offsetRO, coneRO, coneClipRO, coneOffsetRO, coneExtendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightOffset>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightConeClip>, RefRO<ComponentSightConeOffset>, RefRO<ComponentSightConeExtend>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Value.TransformPoint(offsetRO.ValueRO.Value + coneOffsetRO.ValueRO.Value),
                    coneRO.ValueRO, coneExtendRO.ValueRO, coneClipRO.ValueRO.RadiusSquared,
                    buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, coneRO, coneClipRO, coneOffsetRO, coneExtendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>, RefRO<ComponentSightConeClip>,
                             RefRO<ComponentSightConeOffset>, RefRO<ComponentSightConeExtend>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightOffset>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Value.TransformPoint(coneOffsetRO.ValueRO.Value),
                    coneRO.ValueRO, coneExtendRO.ValueRO, coneClipRO.ValueRO.RadiusSquared,
                    buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, offsetRO, coneRO, coneOffsetRO, coneExtendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightOffset>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightConeOffset>, RefRO<ComponentSightConeExtend>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightConeClip>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Value.TransformPoint(offsetRO.ValueRO.Value + coneOffsetRO.ValueRO.Value),
                    coneRO.ValueRO, coneExtendRO.ValueRO, buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, coneRO, coneOffsetRO, coneExtendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightConeOffset>, RefRO<ComponentSightConeExtend>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightConeClip, ComponentSightOffset>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Value.TransformPoint(coneOffsetRO.ValueRO.Value),
                    coneRO.ValueRO, coneExtendRO.ValueRO, buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, positionRO, coneRO, coneClipRO, coneExtendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightConeClip>, RefRO<ComponentSightConeExtend>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightConeOffset>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, positionRO.ValueRO.Value,
                    coneRO.ValueRO, coneExtendRO.ValueRO, coneClipRO.ValueRO.RadiusSquared,
                    buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, coneRO, coneClipRO, coneExtendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightConeClip>, RefRO<ComponentSightConeExtend>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightConeOffset, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, transformRO.ValueRO.Position,
                    coneRO.ValueRO, coneExtendRO.ValueRO, coneClipRO.ValueRO.RadiusSquared,
                    buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, positionRO, coneRO, coneExtendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>,
                             RefRO<ComponentSightCone>, RefRO<ComponentSightConeExtend>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightConeOffset, ComponentSightConeClip>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, positionRO.ValueRO.Value,
                    coneRO.ValueRO, coneExtendRO.ValueRO, buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, coneRO, coneExtendRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>, RefRO<ComponentSightConeExtend>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightConeOffset, ComponentSightConeClip, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, transformRO.ValueRO.Position,
                    coneRO.ValueRO, coneExtendRO.ValueRO, buffersPerceive[receiver], ref commands);
            }

            foreach (var (transformRO, offsetRO, coneRO, coneClipRO, coneOffsetRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightOffset>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightConeClip>, RefRO<ComponentSightConeOffset>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightConeExtend>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Value.TransformPoint(offsetRO.ValueRO.Value + coneOffsetRO.ValueRO.Value),
                    coneRO.ValueRO, coneClipRO.ValueRO.RadiusSquared, ref commands);
            }

            foreach (var (transformRO, coneRO, coneClipRO, coneOffsetRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>,
                             RefRO<ComponentSightConeClip>, RefRO<ComponentSightConeOffset>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightConeExtend, ComponentSightOffset>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Value.TransformPoint(coneOffsetRO.ValueRO.Value),
                    coneRO.ValueRO, coneClipRO.ValueRO.RadiusSquared, ref commands);
            }

            foreach (var (transformRO, offsetRO, coneRO, coneOffsetRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightOffset>,
                             RefRO<ComponentSightCone>, RefRO<ComponentSightConeOffset>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightConeExtend, ComponentSightConeClip>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Value.TransformPoint(offsetRO.ValueRO.Value + coneOffsetRO.ValueRO.Value),
                    coneRO.ValueRO, ref commands);
            }

            foreach (var (transformRO, coneRO, coneOffsetRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>, RefRO<ComponentSightConeOffset>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightConeExtend, ComponentSightConeClip, ComponentSightOffset>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    transformRO.ValueRO.Value.TransformPoint(coneOffsetRO.ValueRO.Value),
                    coneRO.ValueRO, ref commands);
            }

            foreach (var (transformRO, positionRO, coneRO, coneClipRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>,
                             RefRO<ComponentSightCone>, RefRO<ComponentSightConeClip>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightConeExtend, ComponentSightConeOffset>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, positionRO.ValueRO.Value,
                    coneRO.ValueRO, coneClipRO.ValueRO.RadiusSquared, ref commands);
            }

            foreach (var (transformRO, coneRO, coneClipRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>, RefRO<ComponentSightConeClip>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightConeExtend, ComponentSightConeOffset, ComponentSightPosition>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO, transformRO.ValueRO.Position,
                    coneRO.ValueRO, coneClipRO.ValueRO.RadiusSquared, ref commands);
            }

            foreach (var (transformRO, positionRO, coneRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightPosition>, RefRO<ComponentSightCone>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightConeExtend, ComponentSightConeOffset, ComponentSightConeClip>()
                         .WithEntityAccess())
            {
                buffersInsideCone[receiver].Clear();
                ProcessReceiver(ref state, receiver, in transformRO.ValueRO,
                    positionRO.ValueRO.Value, coneRO.ValueRO, ref commands);
            }

            foreach (var (transformRO, coneRO, receiver) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentSightCone>>()
                         .WithAll<TagSightReceiver, BufferSightInsideCone>()
                         .WithNone<ComponentSightConeExtend, ComponentSightConeOffset>()
                         .WithNone<ComponentSightConeClip, ComponentSightPosition>()
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
            ComponentSightCone cone, ComponentSightConeExtend coneExtend, float clipRadiusSquared,
            DynamicBuffer<BufferSightPerceive> bufferPerceive, ref EntityCommandBuffer commands)
        {
            foreach (var (sourcePositionRO, source) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightSource>()
                         .WithEntityAccess())
            {
                ProcessSource(ref state, receiver, in transform, position, cone, coneExtend,
                    clipRadiusSquared, source, sourcePositionRO.ValueRO.Value,
                    bufferPerceive, ref commands);
            }

            foreach (var (sourceTransformRO, source) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightSource>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                ProcessSource(ref state, receiver, in transform, position, cone, coneExtend,
                    clipRadiusSquared, source, sourceTransformRO.ValueRO.Position,
                    bufferPerceive, ref commands);
            }
        }

        private void ProcessReceiver(ref SystemState state,
            Entity receiver, in LocalToWorld transform, float3 position,
            ComponentSightCone cone, ComponentSightConeExtend coneExtend,
            DynamicBuffer<BufferSightPerceive> bufferPerceive, ref EntityCommandBuffer commands)
        {
            foreach (var (sourcePositionRO, source) in SystemAPI
                         .Query<RefRO<ComponentSightPosition>>()
                         .WithAll<TagSightSource>()
                         .WithEntityAccess())
            {
                ProcessSource(ref state, receiver, in transform, position, cone,
                    coneExtend, source, sourcePositionRO.ValueRO.Value,
                    bufferPerceive, ref commands);
            }

            foreach (var (sourceTransformRO, source) in SystemAPI
                         .Query<RefRO<LocalToWorld>>()
                         .WithAll<TagSightSource>()
                         .WithNone<ComponentSightPosition>()
                         .WithEntityAccess())
            {
                ProcessSource(ref state, receiver, in transform, position, cone,
                    coneExtend, source, sourceTransformRO.ValueRO.Position,
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
            ComponentSightCone cone, ComponentSightConeExtend coneExtend, float clipRadiusSquared,
            Entity source, float3 sourcePosition, DynamicBuffer<BufferSightPerceive> bufferPerceive,
            ref EntityCommandBuffer commands)
        {
            var (anglesTan, radiusSquared) = PickCone(bufferPerceive, source, cone, coneExtend);

            if (IsInsideCone(in transform, position, anglesTan, clipRadiusSquared, radiusSquared, sourcePosition))
            {
                commands.AppendToBuffer(receiver, new BufferSightInsideCone
                {
                    Source = source,
                    Position = sourcePosition,
                });
            }
        }

        private void ProcessSource(ref SystemState state,
            Entity receiver, in LocalToWorld transform, float3 position, ComponentSightCone cone,
            ComponentSightConeExtend coneExtend, Entity source, float3 sourcePosition,
            DynamicBuffer<BufferSightPerceive> bufferPerceive, ref EntityCommandBuffer commands)
        {
            var (anglesTan, radiusSquared) = PickCone(bufferPerceive, source, cone, coneExtend);

            if (IsInsideCone(in transform, position, anglesTan, radiusSquared, sourcePosition))
            {
                commands.AppendToBuffer(receiver, new BufferSightInsideCone
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
                commands.AppendToBuffer(receiver, new BufferSightInsideCone
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

        private (float2 anglesTan, float radiusSquared) PickCone(
            DynamicBuffer<BufferSightPerceive> bufferPerceive, Entity source,
            ComponentSightCone cone, ComponentSightConeExtend coneExtend)
        {
            foreach (var perceive in bufferPerceive)
            {
                if (perceive.Source == source)
                {
                    return (coneExtend.AnglesTan, coneExtend.RadiusSquared);
                }
            }

            return (cone.AnglesTan, cone.RadiusSquared);
        }
    }
}