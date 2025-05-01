using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Perception.Editor
{
    [UpdateInGroup(typeof(HearingSystemGroup), OrderLast = true)]
    public partial struct SystemHearingDebug : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ComponentHearingDebug>();

            if (!SystemAPI.HasSingleton<ComponentHearingDebug>())
            {
                state.EntityManager.CreateSingleton(ComponentHearingDebug.Default);
            }
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var commands = new EntityCommandBuffer(Allocator.Temp);

            foreach (var source in SystemAPI
                         .QueryBuilder()
                         .WithAny<TagHearingReceiver, TagHearingSource>()
                         .WithNone<TagHearingDebug>()
                         .Build()
                         .ToEntityArray(Allocator.Temp))
            {
                commands.AddComponent<TagHearingDebug>(source);
                commands.SetComponentEnabled<TagHearingDebug>(source, false);
            }

            commands.Playback(state.EntityManager);

            var buffersPerceive = SystemAPI.GetBufferLookup<BufferHearingPerceive>();
            var buffersMemory = SystemAPI.GetBufferLookup<BufferHearingMemory>();
            var lookupPosition = SystemAPI.GetComponentLookup<ComponentHearingPosition>();

            ref readonly var debug = ref SystemAPI.GetSingletonRW<ComponentHearingDebug>().ValueRO;

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentHearingPosition>>()
                         .WithAll<TagHearingReceiver, TagHearingDebug>()
                         .WithAll<ComponentHearingMemory>()
                         .WithEntityAccess())
            {
                var position = positionRO.ValueRO.Current;
                var bufferMemory = buffersMemory[receiver];

                foreach (var memory in bufferMemory)
                {
                    var sourcePosition = lookupPosition[memory.Source].Current;
                    Debug.DrawLine(position, memory.Position, debug.ColorSourceMemorized);
                    Debug.DrawLine(memory.Position, sourcePosition, debug.ColorSourceMemorized);
                    DebugAdvanced.DrawOctahedron(memory.Position, debug.SizeOctahedron * debug.ScaleOctahedronSmall, debug.ColorSourceMemorized);
                    DebugAdvanced.DrawOctahedron(sourcePosition, debug.SizeOctahedron * debug.ScaleOctahedronBig, debug.ColorSourceMemorized);
                }
            }

            foreach (var (positionRO, receiver) in SystemAPI
                         .Query<RefRO<ComponentHearingPosition>>()
                         .WithAll<TagHearingReceiver, TagHearingDebug>()
                         .WithEntityAccess())
            {
                var position = positionRO.ValueRO.Current;
                var bufferPerceive = buffersPerceive[receiver];

                foreach (var perceive in bufferPerceive)
                {
                    Debug.DrawLine(position, perceive.Position, debug.ColorSourcePerceived);
                    DebugAdvanced.DrawOctahedron(perceive.Position, debug.SizeOctahedron * debug.ScaleOctahedronBig, debug.ColorSourcePerceived);
                }
            }

            foreach (var (transformRO, positionRO, radiusRO) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentHearingPosition>, RefRO<ComponentHearingRadius>>()
                         .WithAll<TagHearingSource, TagHearingDebug>()
                         .WithAll<ComponentHearingDuration>())
            {
                var position = positionRO.ValueRO.Current;
                var radius = math.sqrt(radiusRO.ValueRO.InternalCurrentSquared);

                DebugAdvanced.DrawSphere(position, transformRO.ValueRO.Rotation, radius, debug.ColorSourceInternal);
            }

            foreach (var (transformRO, positionRO, radiusRO) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ComponentHearingPosition>, RefRO<ComponentHearingRadius>>()
                         .WithAll<TagHearingSource, TagHearingDebug>())
            {
                var position = positionRO.ValueRO.Current;
                var radius = math.sqrt(radiusRO.ValueRO.CurrentSquared);

                DebugAdvanced.DrawSphere(position, transformRO.ValueRO.Rotation, radius, debug.ColorSourceSphere);
            }

            foreach (var (positionRO, radiusRO) in SystemAPI
                         .Query<RefRO<ComponentHearingPosition>, RefRO<ComponentHearingRadius>>()
                         .WithAll<TagHearingSource, TagHearingDebug>()
                         .WithAll<ComponentHearingDuration>()
                         .WithNone<LocalToWorld>())
            {
                var position = positionRO.ValueRO.Current;
                var radius = math.sqrt(radiusRO.ValueRO.InternalCurrentSquared);

                DebugAdvanced.DrawSphere(position, quaternion.identity, radius, debug.ColorSourceInternal);
            }

            foreach (var (positionRO, radiusRO) in SystemAPI
                         .Query<RefRO<ComponentHearingPosition>, RefRO<ComponentHearingRadius>>()
                         .WithAll<TagHearingSource, TagHearingDebug>()
                         .WithNone<LocalToWorld>())
            {
                var position = positionRO.ValueRO.Current;
                var radius = math.sqrt(radiusRO.ValueRO.CurrentSquared);

                DebugAdvanced.DrawSphere(position, quaternion.identity, radius, debug.ColorSourceSphere);
            }
        }
    }
}