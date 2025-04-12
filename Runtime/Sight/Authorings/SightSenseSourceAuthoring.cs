using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Perception
{
    public class SightSenseSourceAuthoring : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] protected float3 _offset;
        [SerializeField] protected GameObject[] _children;

        public class Baker : Baker<SightSenseSourceAuthoring>
        {
            public override void Bake(SightSenseSourceAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new TagSightSource());

                if (math.any(authoring._offset != float3.zero))
                {
                    AddComponent(entity, new ComponentSightOffset { Source = authoring._offset });
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
            }
        }
    }
}