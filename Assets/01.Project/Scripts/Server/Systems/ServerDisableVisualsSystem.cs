using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class ServerDisableVisualsSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 1. [최신 규격 대응] LocalTransform을 가진 엔티티 중 아직 그래픽이 비활성화되지 않은 대상을 찾습니다.
        foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>().WithNone<ServerVisualsDisabledTag>().WithEntityAccess())
        {
            // 2. SystemAPI.ManagedAPI를 통해 엔티티에 SpriteRenderer가 포함되어 있는지 확인합니다.
            if (SystemAPI.ManagedAPI.HasComponent<SpriteRenderer>(entity))
            {
                var spriteRenderer = SystemAPI.ManagedAPI.GetComponent<SpriteRenderer>(entity);
                if (spriteRenderer != null)
                {
                    // 서버에서는 렌더러를 꺼서 화면에 나타나지 않게 만듭니다.
                    spriteRenderer.enabled = false;
                }
            }

            // 매 프레임 재시도하지 않도록 마킹 컴포넌트를 붙여줍니다.
            ecb.AddComponent<ServerVisualsDisabledTag>(entity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

// 중복 처리 방지용 태그 컴포넌트
public struct ServerVisualsDisabledTag : IComponentData { }
