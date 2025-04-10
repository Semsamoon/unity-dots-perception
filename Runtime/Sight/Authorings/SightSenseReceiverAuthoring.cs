using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Perception
{
    public class SightSenseReceiverAuthoring : MonoBehaviour
    {
        [SerializeField] protected float _coneRadius;
        [SerializeField] protected float2 _coneAnglesDegrees;

        [SerializeField] protected float _clipRadius;
        [SerializeField] protected float _extendRadius;
        [SerializeField] protected float2 _extendAnglesDegrees;
        [SerializeField] protected float3 _offset;

        [SerializeField] protected float _memoryTime;
        [SerializeField] protected GameObject[] _children;
        [SerializeField] protected float3[] _rayOffsets;

        public class Baker : Baker<SightSenseReceiverAuthoring>
        {
            public override void Bake(SightSenseReceiverAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new TagSightReceiver());
                AddComponent(entity, new ComponentSightCone
                {
                    RadiusSquared = authoring._coneRadius * authoring._coneRadius,
                    AnglesTan = math.tan(math.radians(authoring._coneAnglesDegrees)),
                });

                if (authoring._clipRadius > 0)
                {
                    AddComponent(entity, new ComponentSightClip
                    {
                        RadiusSquared = authoring._clipRadius * authoring._clipRadius
                    });
                }

                if (authoring._extendRadius > 0 || math.any(authoring._extendAnglesDegrees > float2.zero))
                {
                    var extendRadius = authoring._coneRadius + authoring._extendRadius;
                    var extendAnglesDegrees = authoring._coneAnglesDegrees + authoring._extendAnglesDegrees;

                    AddComponent(entity, new ComponentSightExtend
                    {
                        RadiusSquared = extendRadius * extendRadius,
                        AnglesTan = math.tan(math.radians(extendAnglesDegrees)),
                    });
                }

                if (math.any(authoring._offset != float3.zero))
                {
                    AddComponent(entity, new ComponentSightOffset { Receiver = authoring._offset });
                }

                if (authoring._memoryTime > 0)
                {
                    AddComponent(entity, new ComponentSightMemory { Time = authoring._memoryTime });
                }

                if (authoring._children is { Length: > 0 })
                {
                    AddBuffer<BufferSightChild>(entity);

                    foreach (var child in authoring._children)
                    {
                        if (child)
                        {
                            AppendToBuffer(entity, new BufferSightChild { Value = GetEntity(child, TransformUsageFlags.Dynamic) });
                        }
                    }
                }

                if (authoring._rayOffsets is { Length: > 0 })
                {
                    AddComponent(entity, new TagSightRayMultiple());
                    AddBuffer<BufferSightRayOffset>(entity);

                    foreach (var rayOffset in authoring._rayOffsets)
                    {
                        AppendToBuffer(entity, new BufferSightRayOffset { Value = rayOffset });
                    }
                }
                else
                {
                    AddComponent(entity, new TagSightRaySingle());
                }
            }
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            _coneRadius = math.max(0, _coneRadius);
            _coneAnglesDegrees = math.max(new float2(0, 0), _coneAnglesDegrees);
            _coneAnglesDegrees = math.min(new float2(360, 180), _coneAnglesDegrees);

            _clipRadius = math.max(0, _clipRadius);
            _clipRadius = math.min(_coneRadius, _clipRadius);

            _extendRadius = math.max(0, _extendRadius);
            _extendAnglesDegrees = math.max(new float2(0, 0), _extendAnglesDegrees);
            _extendAnglesDegrees = math.min(new float2(360, 180) - _coneAnglesDegrees, _extendAnglesDegrees);

            _memoryTime = math.max(0, _memoryTime);
        }
#endif
    }
}