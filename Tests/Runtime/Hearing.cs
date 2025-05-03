using System.Collections;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Perception.Tests
{
    public sealed class Hearing
    {
        private const float Delay = 0.1f;
        private const float DelaySquared = Delay * Delay;
        private readonly WaitForSeconds _awaitDelay = new(Delay * 1.5f);

        [UnityTest]
        public IEnumerator Position()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var receiver = new EntityBuilder(entityManager).Receiver().Offset(new float3(1, 1, 1)).Build();
            var source = new EntityBuilder(entityManager, new float3(2, 2, 2)).Source().Build();

            try
            {
                yield return null;
                Assert.True(entityManager.HasComponent<ComponentHearingPosition>(receiver));
                Assert.False(entityManager.HasComponent<ComponentHearingOffset>(source));
                Assert.AreEqual(new float3(1, 1, 1), entityManager.GetComponentData<ComponentHearingPosition>(receiver).Current);
                Assert.AreEqual(new float3(), entityManager.GetComponentData<ComponentHearingPosition>(receiver).Previous);
                Assert.AreEqual(new float3(2, 2, 2), entityManager.GetComponentData<ComponentHearingPosition>(source).Current);
                Assert.AreEqual(new float3(), entityManager.GetComponentData<ComponentHearingPosition>(receiver).Previous);

                entityManager.SetComponentData(receiver, new ComponentHearingOffset { Value = new float3(3, 3, 3) });
                entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(4, 4, 4)) });
                yield return null;
                Assert.AreEqual(new float3(3, 3, 3), entityManager.GetComponentData<ComponentHearingPosition>(receiver).Current);
                Assert.AreEqual(new float3(1, 1, 1), entityManager.GetComponentData<ComponentHearingPosition>(receiver).Previous);
                Assert.AreEqual(new float3(4, 4, 4), entityManager.GetComponentData<ComponentHearingPosition>(source).Current);
                Assert.AreEqual(new float3(2, 2, 2), entityManager.GetComponentData<ComponentHearingPosition>(source).Previous);
            }
            finally
            {
                entityManager.DestroyEntity(receiver);
                entityManager.DestroyEntity(source);
            }
        }

        [UnityTest]
        public IEnumerator Sphere()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var source = new EntityBuilder(entityManager).Source(1, DelaySquared).Build();

            try
            {
                yield return null;
                Assert.True(entityManager.HasComponent<ComponentHearingRadius>(source));
                Assert.Less(0, entityManager.GetComponentData<ComponentHearingRadius>(source).CurrentSquared);
                Assert.AreEqual(0, entityManager.GetComponentData<ComponentHearingRadius>(source).PreviousSquared);
                Assert.AreEqual(0, entityManager.GetComponentData<ComponentHearingRadius>(source).InternalCurrentSquared);
                Assert.AreEqual(0, entityManager.GetComponentData<ComponentHearingRadius>(source).InternalPreviousSquared);

                yield return _awaitDelay;
                Assert.AreEqual(DelaySquared, entityManager.GetComponentData<ComponentHearingRadius>(source).CurrentSquared);
                Assert.AreEqual(DelaySquared, entityManager.GetComponentData<ComponentHearingRadius>(source).PreviousSquared);

                var radius = entityManager.GetComponentData<ComponentHearingRadius>(source);
                radius.CurrentDuration = 0;
                entityManager.SetComponentData(source, radius);
                yield return null;
                Assert.Less(0, entityManager.GetComponentData<ComponentHearingRadius>(source).InternalCurrentSquared);
                Assert.AreEqual(0, entityManager.GetComponentData<ComponentHearingRadius>(source).InternalPreviousSquared);

                yield return _awaitDelay;
                Assert.False(entityManager.Exists(source));
            }
            finally
            {
                if (entityManager.Exists(source))
                {
                    entityManager.DestroyEntity(source);
                }
            }
        }

        [UnityTest]
        public IEnumerator Perceive()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var receiver = new EntityBuilder(entityManager).Receiver().Build();
            var source = new EntityBuilder(entityManager, new float3(0, 0, Delay)).Source(1, DelaySquared).Build();

            try
            {
                yield return null;
                Assert.True(entityManager.HasBuffer<BufferHearingPerceive>(receiver));
                Assert.AreEqual(0, entityManager.GetBuffer<BufferHearingPerceive>(receiver).Length);

                yield return _awaitDelay;
                Assert.AreEqual(1, entityManager.GetBuffer<BufferHearingPerceive>(receiver).Length);
                Assert.AreEqual(source, entityManager.GetBuffer<BufferHearingPerceive>(receiver)[0].Source);
                Assert.AreEqual(new float3(0, 0, Delay), entityManager.GetBuffer<BufferHearingPerceive>(receiver)[0].Position);

                var radius = entityManager.GetComponentData<ComponentHearingRadius>(source);
                radius.CurrentDuration = 0;
                entityManager.SetComponentData(source, radius);
                yield return _awaitDelay;
                Assert.AreEqual(0, entityManager.GetBuffer<BufferHearingPerceive>(receiver).Length);
            }
            finally
            {
                entityManager.DestroyEntity(receiver);
                entityManager.DestroyEntity(source);
            }
        }

        [UnityTest]
        public IEnumerator Memory()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var receiver = new EntityBuilder(entityManager).Receiver().Memory(Delay).Build();
            var source = new EntityBuilder(entityManager).Source(0, 1).Build();

            try
            {
                yield return null;
                Assert.True(entityManager.HasBuffer<BufferHearingMemory>(receiver));
                Assert.AreEqual(0, entityManager.GetBuffer<BufferHearingMemory>(receiver).Length);

                entityManager.SetComponentData(receiver, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 1)) });
                yield return null;
                Assert.AreEqual(1, entityManager.GetBuffer<BufferHearingMemory>(receiver).Length);
                Assert.AreEqual(source, entityManager.GetBuffer<BufferHearingMemory>(receiver)[0].Source);
                Assert.AreEqual(float3.zero, entityManager.GetBuffer<BufferHearingMemory>(receiver)[0].Position);
                Assert.AreEqual(Delay, entityManager.GetBuffer<BufferHearingMemory>(receiver)[0].Time);

                yield return null;
                Assert.Greater(Delay, entityManager.GetBuffer<BufferHearingMemory>(receiver)[0].Time);

                entityManager.SetComponentData(receiver, new LocalToWorld { Value = float4x4.identity });
                yield return null;
                Assert.AreEqual(0, entityManager.GetBuffer<BufferHearingMemory>(receiver).Length);

                entityManager.SetComponentData(receiver, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 1)) });
                yield return _awaitDelay;
                Assert.AreEqual(0, entityManager.GetBuffer<BufferHearingMemory>(receiver).Length);
            }
            finally
            {
                entityManager.DestroyEntity(receiver);
                entityManager.DestroyEntity(source);
            }
        }

        private struct EntityBuilder
        {
            private EntityManager _entityManager;
            private readonly Entity _entity;

            public EntityBuilder(EntityManager entityManager, float3 position = default)
            {
                _entityManager = entityManager;
                _entity = entityManager.CreateEntity();
                _entityManager.AddComponentData(_entity, new LocalToWorld { Value = float4x4.Translate(position) });
            }

            public EntityBuilder Receiver()
            {
                _entityManager.AddComponentData(_entity, new TagHearingReceiver());
                return this;
            }

            public EntityBuilder Source(float speed = 0, float rangeSquared = 0, float duration = float.PositiveInfinity)
            {
                _entityManager.AddComponentData(_entity, new TagHearingSource());
                _entityManager.AddComponentData(_entity, new ComponentHearingSphere { Speed = speed, RangeSquared = rangeSquared, Duration = duration });
                return this;
            }

            public EntityBuilder Offset(float3 offset = default)
            {
                _entityManager.AddComponentData(_entity, new ComponentHearingOffset { Value = offset });
                return this;
            }

            public EntityBuilder Memory(float time = 0)
            {
                _entityManager.AddComponentData(_entity, new ComponentHearingMemory { Time = time });
                return this;
            }

            public Entity Build()
            {
                return _entity;
            }
        }
    }
}