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
        private readonly WaitForSeconds _awaitPhysics = new(Time.fixedDeltaTime * 1.2f);
        private readonly BlobAssetReference<Unity.Physics.Collider> _collider = Unity.Physics.BoxCollider.Create(
            new BoxGeometry { Orientation = quaternion.identity, Size = new float3(1, 1, 1) }, CollisionFilter.Default);

        [UnityTest]
        public IEnumerator Position()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var receiver = new EntityBuilder(entityManager).Receiver().Offset(new float3(1, 1, 1)).Build();
            var source = new EntityBuilder(entityManager, new float3(2, 2, 2)).Source().Build();

            try
            {
                yield return _awaitPhysics;
                Assert.True(entityManager.HasComponent<ComponentSightPosition>(receiver));
                Assert.False(entityManager.HasComponent<ComponentSightOffset>(source));
                Assert.AreEqual(new float3(1, 1, 1), entityManager.GetComponentData<ComponentSightPosition>(receiver).Receiver);
                Assert.AreEqual(new float3(2, 2, 2), entityManager.GetComponentData<ComponentSightPosition>(source).Source);

                entityManager.SetComponentData(receiver, new ComponentSightOffset { Receiver = new float3(3, 3, 3) });
                entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(4, 4, 4)) });
                yield return _awaitPhysics;
                Assert.AreEqual(new float3(3, 3, 3), entityManager.GetComponentData<ComponentSightPosition>(receiver).Receiver);
                Assert.AreEqual(new float3(4, 4, 4), entityManager.GetComponentData<ComponentSightPosition>(source).Source);
            }
            finally
            {
                entityManager.DestroyEntity(receiver);
                entityManager.DestroyEntity(source);
            }
        }

        [UnityTest]
        public IEnumerator Cone()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var receiver = new EntityBuilder(entityManager).Receiver().RaySingle().Cone(new float2(1, 0), 4).Collider(_collider).Build();
            var source = new EntityBuilder(entityManager, new float3(0, 0, 2)).Source().Collider(_collider).Build();

            try
            {
                yield return _awaitPhysics;
                Assert.True(entityManager.HasBuffer<BufferSightCone>(receiver));
                Assert.AreEqual(1, entityManager.GetBuffer<BufferSightCone>(receiver).Length);
                Assert.AreEqual(source, entityManager.GetBuffer<BufferSightCone>(receiver)[0].Source);
                Assert.AreEqual(new float3(0, 0, 2), entityManager.GetBuffer<BufferSightCone>(receiver)[0].Position);

                entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(1, 1, 1)) });
                yield return _awaitPhysics;
                Assert.AreEqual(0, entityManager.GetBuffer<BufferSightCone>(receiver).Length);

                entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 3)) });
                yield return _awaitPhysics;
                Assert.AreEqual(0, entityManager.GetBuffer<BufferSightCone>(receiver).Length);

                var cone = new ComponentSightCone { Filter = CollisionFilter.Default, AnglesCos = new float2(0.5f, 0.5f), RadiusSquared = 16 };
                entityManager.SetComponentData(receiver, cone);
                entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(2, 2, 2)) });
                yield return _awaitPhysics;
                Assert.AreEqual(1, entityManager.GetBuffer<BufferSightCone>(receiver).Length);

                cone.ClipSquared = 13;
                entityManager.AddComponentData(receiver, cone);
                yield return _awaitPhysics;
                Assert.AreEqual(0, entityManager.GetBuffer<BufferSightCone>(receiver).Length);

                cone.ClipSquared = 4;
                entityManager.AddComponentData(receiver, new ComponentSightOffset { Receiver = new float3(2, 2, -2) });
                entityManager.SetComponentData(receiver, cone);
                yield return _awaitPhysics;
                Assert.AreEqual(1, entityManager.GetBuffer<BufferSightCone>(receiver).Length);

                entityManager.AddComponentData(receiver, new ComponentSightExtend { AnglesCos = new float2(0.17f, 0.17f), RadiusSquared = 25 });
                entityManager.SetComponentData(receiver, new LocalToWorld { Value = float4x4.Translate(new float3(-3, -3, 3)) });
                yield return _awaitPhysics;
                Assert.AreEqual(1, entityManager.GetBuffer<BufferSightCone>(receiver).Length);
            }
            finally
            {
                entityManager.DestroyEntity(receiver);
                entityManager.DestroyEntity(source);
            }
        }

        [UnityTest]
        public IEnumerator PerceiveSingle()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var receiver = new EntityBuilder(entityManager).Receiver().RaySingle().Cone(new float2(-1, 0), 100).Collider(_collider).Build();
            var source = new EntityBuilder(entityManager, new float3(0, 0, 5)).Source().Collider(_collider).Build();
            var obstacle = new EntityBuilder(entityManager, new float3(0, 0, 3)).Collider(_collider).Build();

            try
            {
                yield return _awaitPhysics;
                Assert.True(entityManager.HasBuffer<BufferSightPerceive>(receiver));
                Assert.AreEqual(0, entityManager.GetBuffer<BufferSightPerceive>(receiver).Length);

                entityManager.SetComponentData(obstacle, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 7)) });
                yield return _awaitPhysics;
                Assert.AreEqual(1, entityManager.GetBuffer<BufferSightPerceive>(receiver).Length);
                Assert.AreEqual(source, entityManager.GetBuffer<BufferSightPerceive>(receiver)[0].Source);
                Assert.AreEqual(new float3(0, 0, 5), entityManager.GetBuffer<BufferSightPerceive>(receiver)[0].Position);

                var cone = new ComponentSightCone { Filter = CollisionFilter.Default, AnglesCos = new float2(0.5f, 0.5f), RadiusSquared = 49, ClipSquared = 26 };
                entityManager.SetComponentData(receiver, cone);
                yield return _awaitPhysics;
                Assert.AreEqual(0, entityManager.GetBuffer<BufferSightPerceive>(receiver).Length);

                cone.ClipSquared = 4;
                entityManager.SetComponentData(receiver, cone);
                yield return _awaitPhysics;
                Assert.AreEqual(1, entityManager.GetBuffer<BufferSightPerceive>(receiver).Length);

                entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(0, 5, 5)) });
                yield return _awaitPhysics;
                Assert.AreEqual(0, entityManager.GetBuffer<BufferSightPerceive>(receiver).Length);

                entityManager.AddBuffer<BufferSightChild>(source);
                entityManager.GetBuffer<BufferSightChild>(source).Add(new BufferSightChild { Value = obstacle });
                entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 5)) });
                entityManager.SetComponentData(obstacle, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 3)) });
                yield return _awaitPhysics;
                Assert.AreEqual(1, entityManager.GetBuffer<BufferSightPerceive>(receiver).Length);
            }
            finally
            {
                entityManager.DestroyEntity(receiver);
                entityManager.DestroyEntity(source);
                entityManager.DestroyEntity(obstacle);
            }
        }

        [UnityTest]
        public IEnumerator PerceiveMultiple()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var receiver = new EntityBuilder(entityManager).Receiver().RayMultiple()
                .Cone(new float2(-1, 0), 100).RayOffset(new float3(0, 0.5f, 0)).Collider(_collider).Build();
            var source = new EntityBuilder(entityManager, new float3(0, 0, 5.5f)).Source().Collider(_collider).Build();
            var obstacle = new EntityBuilder(entityManager, new float3(0, 0, 3)).Collider(_collider).Build();

            try
            {
                yield return _awaitPhysics;
                Assert.True(entityManager.HasBuffer<BufferSightPerceive>(receiver));
                Assert.AreEqual(0, entityManager.GetBuffer<BufferSightPerceive>(receiver).Length);

                entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0.6f, 5.5f)) });
                yield return _awaitPhysics;
                Assert.AreEqual(1, entityManager.GetBuffer<BufferSightPerceive>(receiver).Length);
                Assert.AreEqual(source, entityManager.GetBuffer<BufferSightPerceive>(receiver)[0].Source);
                Assert.AreEqual(new float3(0, 0.6f, 5.5f), entityManager.GetBuffer<BufferSightPerceive>(receiver)[0].Position);

                entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(0, -0.6f, 5.5f)) });
                yield return _awaitPhysics;
                Assert.AreEqual(0, entityManager.GetBuffer<BufferSightPerceive>(receiver).Length);

                entityManager.GetBuffer<BufferSightRayOffset>(receiver).Add(new BufferSightRayOffset { Value = new float3(0, -0.5f, 0) });
                yield return _awaitPhysics;
                Assert.AreEqual(1, entityManager.GetBuffer<BufferSightPerceive>(receiver).Length);

                var cone = new ComponentSightCone { Filter = CollisionFilter.Default, AnglesCos = new float2(-1, 0), RadiusSquared = 100, ClipSquared = 32 };
                entityManager.SetComponentData(receiver, cone);
                yield return _awaitPhysics;
                Assert.AreEqual(0, entityManager.GetBuffer<BufferSightPerceive>(receiver).Length);
            }
            finally
            {
                entityManager.DestroyEntity(receiver);
                entityManager.DestroyEntity(source);
                entityManager.DestroyEntity(obstacle);
            }
        }

        [UnityTest]
        public IEnumerator Memory()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var receiver = new EntityBuilder(entityManager).Receiver().RaySingle().Cone(new float2(1, 1), 4).Collider(_collider).Memory(0.05f).Build();
            var source = new EntityBuilder(entityManager, new float3(0, 0, 2)).Source().Collider(_collider).Build();
            var obstacle = new EntityBuilder(entityManager, new float3(0, 0, 3)).Collider(_collider).Build();

            try
            {
                yield return _awaitPhysics;
                Assert.True(entityManager.HasBuffer<BufferSightMemory>(receiver));
                Assert.AreEqual(0, entityManager.GetBuffer<BufferSightMemory>(receiver).Length);

                entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 4)) });
                yield return null;
                Assert.AreEqual(1, entityManager.GetBuffer<BufferSightMemory>(receiver).Length);
                Assert.Greater(0.05f, entityManager.GetBuffer<BufferSightMemory>(receiver)[0].Time);
                Assert.AreEqual(source, entityManager.GetBuffer<BufferSightMemory>(receiver)[0].Source);
                Assert.AreEqual(new float3(0, 0, 2), entityManager.GetBuffer<BufferSightMemory>(receiver)[0].Position);

                yield return new WaitForSeconds(0.05f);
                Assert.AreEqual(0, entityManager.GetBuffer<BufferSightMemory>(receiver).Length);

                entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 2)) });
                yield return _awaitPhysics;

                entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 4)) });
                yield return _awaitPhysics;
                Assert.AreEqual(1, entityManager.GetBuffer<BufferSightMemory>(receiver).Length);

                entityManager.SetComponentData(source, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 2)) });
                yield return _awaitPhysics;
                Assert.AreEqual(0, entityManager.GetBuffer<BufferSightMemory>(receiver).Length);

                entityManager.SetComponentData(obstacle, new LocalToWorld { Value = float4x4.Translate(new float3(0, 0, 1)) });
                yield return _awaitPhysics;
                Assert.AreEqual(1, entityManager.GetBuffer<BufferSightMemory>(receiver).Length);
            }
            finally
            {
                entityManager.DestroyEntity(receiver);
                entityManager.DestroyEntity(source);
                entityManager.DestroyEntity(obstacle);
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
                _entityManager.AddComponentData(_entity, new TagSightReceiver());
                return this;
            }

            public EntityBuilder Source()
            {
                _entityManager.AddComponentData(_entity, new TagSightSource());
                return this;
            }

            public EntityBuilder RaySingle()
            {
                _entityManager.AddComponentData(_entity, new TagSightRaySingle());
                return this;
            }

            public EntityBuilder RayMultiple()
            {
                _entityManager.AddComponentData(_entity, new TagSightRayMultiple());
                return this;
            }

            public EntityBuilder RayOffset(float3 offset = default)
            {
                if (!_entityManager.HasBuffer<BufferSightRayOffset>(_entity))
                {
                    _entityManager.AddBuffer<BufferSightRayOffset>(_entity);
                }

                _entityManager.GetBuffer<BufferSightRayOffset>(_entity).Add(new BufferSightRayOffset { Value = offset });
                return this;
            }

            public EntityBuilder Offset(float3 offset = default)
            {
                _entityManager.AddComponentData(_entity, new ComponentSightOffset { Receiver = offset, Source = offset });
                return this;
            }

            public EntityBuilder Memory(float time = 0)
            {
                _entityManager.AddComponentData(_entity, new ComponentSightMemory { Time = time });
                _entityManager.AddBuffer<BufferSightMemory>(_entity);
                return this;
            }

            public EntityBuilder Cone(float2 anglesCos = default, float radiusSquared = 0, float clipSquared = 0)
            {
                _entityManager.AddComponentData(_entity, new ComponentSightCone
                {
                    Filter = CollisionFilter.Default, AnglesCos = anglesCos, RadiusSquared = radiusSquared, ClipSquared = clipSquared
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