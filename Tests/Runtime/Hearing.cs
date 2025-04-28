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
        private readonly float _awaitPhysicsTimeSquared = Time.fixedDeltaTime * Time.fixedDeltaTime;
        private readonly WaitForSeconds _awaitPhysics = new(Time.fixedDeltaTime * 1.2f);

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
            var source = new EntityBuilder(entityManager).Source().Sphere(1, _awaitPhysicsTimeSquared).Build();

            try
            {
                yield return null;
                Assert.True(entityManager.HasComponent<ComponentHearingRadius>(source));
                Assert.Less(0, entityManager.GetComponentData<ComponentHearingRadius>(source).CurrentSquared);
                Assert.AreEqual(0, entityManager.GetComponentData<ComponentHearingRadius>(source).PreviousSquared);
                Assert.AreEqual(0, entityManager.GetComponentData<ComponentHearingRadius>(source).InternalCurrentSquared);
                Assert.AreEqual(0, entityManager.GetComponentData<ComponentHearingRadius>(source).InternalPreviousSquared);

                yield return _awaitPhysics;
                Assert.AreEqual(_awaitPhysicsTimeSquared, entityManager.GetComponentData<ComponentHearingRadius>(source).CurrentSquared);
                Assert.AreEqual(_awaitPhysicsTimeSquared, entityManager.GetComponentData<ComponentHearingRadius>(source).PreviousSquared);

                entityManager.AddComponentData(source, new ComponentHearingDuration { Time = 0 });
                yield return null;
                Assert.Less(0, entityManager.GetComponentData<ComponentHearingRadius>(source).InternalCurrentSquared);
                Assert.AreEqual(0, entityManager.GetComponentData<ComponentHearingRadius>(source).InternalPreviousSquared);

                yield return _awaitPhysics;
                Assert.False(entityManager.Exists(source));

                source = new EntityBuilder(entityManager).Source().Sphere(1, _awaitPhysicsTimeSquared).Build();
            }
            finally
            {
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

            public EntityBuilder Source()
            {
                _entityManager.AddComponentData(_entity, new TagHearingSource());
                return this;
            }

            public EntityBuilder Offset(float3 offset = default)
            {
                _entityManager.AddComponentData(_entity, new ComponentHearingOffset { Value = offset });
                return this;
            }

            public EntityBuilder Sphere(float speed, float rangeSquared)
            {
                _entityManager.AddComponentData(_entity, new ComponentHearingSphere { Speed = speed, RangeSquared = rangeSquared });
                return this;
            }

            public Entity Build()
            {
                return _entity;
            }
        }
    }
}