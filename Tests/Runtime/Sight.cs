using System.Collections;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Perception.Tests
{
    public sealed class Sight
    {
        [UnityTest]
        public IEnumerator Position()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var receiver = new EntityBuilder(entityManager).Receiver().Offset(new float3(1, 1, 1)).Build();
            var source = new EntityBuilder(entityManager, new float3(2, 2, 2)).Source().Position().Build();

            yield return null;
            Assert.True(entityManager.HasComponent<ComponentSightPosition>(receiver));
            Assert.False(entityManager.HasComponent<ComponentSightOffset>(source));
            Assert.AreEqual(new float3(1, 1, 1), entityManager.GetComponentData<ComponentSightPosition>(receiver).Value);
            Assert.AreEqual(new float3(2, 2, 2), entityManager.GetComponentData<ComponentSightPosition>(source).Value);

            entityManager.SetComponentData(receiver, new ComponentSightOffset { Value = new float3(3, 3, 3) });
            entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(4, 4, 4)) });
            yield return null;
            Assert.AreEqual(new float3(3, 3, 3), entityManager.GetComponentData<ComponentSightPosition>(receiver).Value);
            Assert.AreEqual(new float3(4, 4, 4), entityManager.GetComponentData<ComponentSightPosition>(source).Value);
        }

        [UnityTest]
        public IEnumerator Cone()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var receiver = new EntityBuilder(entityManager).Receiver().Cone(radiusSquared: 4).Build();
            var source = new EntityBuilder(entityManager, new float3(0, 0, 2)).Source().Build();

            yield return null;
            Assert.True(entityManager.HasBuffer<BufferSightInsideCone>(receiver));
            Assert.AreEqual(1, entityManager.GetBuffer<BufferSightInsideCone>(receiver).Length);
            Assert.AreEqual(source, entityManager.GetBuffer<BufferSightInsideCone>(receiver)[0].Source);
            Assert.AreEqual(new float3(0, 0, 2), entityManager.GetBuffer<BufferSightInsideCone>(receiver)[0].Position);

            entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(1, 1, 1)) });
            yield return null;
            Assert.AreEqual(0, entityManager.GetBuffer<BufferSightInsideCone>(receiver).Length);

            entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 3)) });
            yield return null;
            Assert.AreEqual(0, entityManager.GetBuffer<BufferSightInsideCone>(receiver).Length);

            entityManager.AddComponentData(receiver, new ComponentSightConeClip { RadiusSquared = 4 });
            entityManager.SetComponentData(receiver, new ComponentSightCone { AnglesTan = new float2(1, 1), RadiusSquared = 16 });
            entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(2, 2, 2)) });
            yield return null;
            Assert.AreEqual(1, entityManager.GetBuffer<BufferSightInsideCone>(receiver).Length);

            entityManager.SetComponentData(receiver, new ComponentSightConeClip { RadiusSquared = 13 });
            yield return null;
            Assert.AreEqual(0, entityManager.GetBuffer<BufferSightInsideCone>(receiver).Length);
        }

        [UnityTest]
        public IEnumerator Perceive()
        {
            var awaitPhysics = new WaitForSeconds(0.1f);
            var collider = Unity.Physics.BoxCollider.Create(new BoxGeometry
            {
                Orientation = quaternion.identity, Size = new float3(1, 1, 1)
            }, CollisionFilter.Default);

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var receiver = new EntityBuilder(entityManager).Receiver().Collider(collider).Build();
            var source = new EntityBuilder(entityManager, new float3(0, 0, 5)).Source().Collider(collider).Build();
            var obstacle = new EntityBuilder(entityManager, new float3(0, 0, 3)).Collider(collider).Build();

            yield return awaitPhysics;
            Assert.True(entityManager.HasBuffer<BufferSightPerceive>(receiver));
            Assert.AreEqual(0, entityManager.GetBuffer<BufferSightPerceive>(receiver).Length);

            entityManager.SetComponentData(obstacle, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 7)) });
            yield return awaitPhysics;
            Assert.AreEqual(1, entityManager.GetBuffer<BufferSightPerceive>(receiver).Length);
            Assert.AreEqual(source, entityManager.GetBuffer<BufferSightPerceive>(receiver)[0].Source);
            Assert.AreEqual(new float3(0, 0, 5), entityManager.GetBuffer<BufferSightPerceive>(receiver)[0].Position);

            entityManager.AddComponentData(receiver, new ComponentSightCone { AnglesTan = new float2(1, 1), RadiusSquared = 25 });
            yield return awaitPhysics;
            Assert.AreEqual(1, entityManager.GetBuffer<BufferSightPerceive>(receiver).Length);

            entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(0, 5, 5)) });
            yield return awaitPhysics;
            Assert.AreEqual(0, entityManager.GetBuffer<BufferSightPerceive>(receiver).Length);
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
                _entityManager.AddComponentData(_entity, new TagSightReceiver());
                return this;
            }

            public EntityBuilder Source()
            {
                _entityManager.AddComponentData(_entity, new TagSightSource());
                return this;
            }

            public EntityBuilder Position(float3 position = default)
            {
                _entityManager.AddComponentData(_entity, new ComponentSightPosition { Value = position });
                return this;
            }

            public EntityBuilder Offset(float3 offset = default)
            {
                _entityManager.AddComponentData(_entity, new ComponentSightOffset { Value = offset });
                return this;
            }

            public EntityBuilder Cone(float2 anglesTan = default, float radiusSquared = 0)
            {
                _entityManager.AddComponentData(_entity, new ComponentSightCone
                {
                    AnglesTan = anglesTan, RadiusSquared = radiusSquared
                });
                return this;
            }

            public EntityBuilder Collider(BlobAssetReference<Unity.Physics.Collider> collider = default)
            {
                _entityManager.AddComponentData(_entity, new PhysicsCollider { Value = collider });
                _entityManager.AddSharedComponent(_entity, new PhysicsWorldIndex());
                return this;
            }

            public Entity Build()
            {
                return _entity;
            }
        }
    }
}