using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class PlayerNameTagSystem : SystemBase
{
    private struct NameTagReference : ICleanupComponentData
    {
        public Entity TargetEntity;
    }

    private PlayerNameTag _cachedNameTagPrefab;

    protected override void OnUpdate()
    {
        // 1. 이름 태그 프리팹 캐싱 (Resources 폴더로부터 로드)
        if (_cachedNameTagPrefab == null)
        {
            _cachedNameTagPrefab = Resources.Load<PlayerNameTag>("PlayerNameTag");
            if (_cachedNameTagPrefab == null)
            {
                if (UnityEngine.Time.frameCount % 120 == 0)
                {
                    Debug.LogWarning("[PlayerNameTagSystem] Assets/Resources/PlayerNameTag 프리팹을 로드하지 못했습니다! Resources 폴더와 파일명을 확인해 주세요.");
                }
                return;
            }
        }

        // ECB 선언
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 2. 이름 태그를 새로 할당받아야 하는 고양이 엔티티를 찾아 태그를 생성합니다.
        foreach (var (playerName, entity) in SystemAPI.Query<RefRO<PlayerName>>().WithNone<NameTagReference>().WithEntityAccess())
        {
            // Resources에서 가져온 프리팹 복제
            var nameTagInstance = Object.Instantiate(_cachedNameTagPrefab);
            
            // 이름 갱신 (만약 GhostOwner 컴포넌트가 있다면 Network ID 값을 받아 Player X 로 표시)
            string displayName = "Player";
            if (SystemAPI.HasComponent<GhostOwner>(entity))
            {
                var owner = SystemAPI.GetComponent<GhostOwner>(entity);
                displayName = $"Player {owner.NetworkId}";
            }
            nameTagInstance.SetName(displayName);

            Debug.Log($"[PlayerNameTagSystem] 이름표({displayName})를 고양이({entity}) 머리 위에 생성 완료!");

            // 청소용 엔티티 생성 및 데이터 매핑
            var cleanEntity = ecb.CreateEntity();
            ecb.AddComponent(cleanEntity, new NameTagReference { TargetEntity = entity });
            ecb.AddComponent(cleanEntity, nameTagInstance);
            
            // 엔티티에 셋업 완료 링크 (ICleanupComponentData 상속 대상이므로 일반 AddComponent 호출)
            ecb.AddComponent(entity, new NameTagReference { TargetEntity = entity });
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();

        // 3. 고양이 위치 실시간 추적 (Z축은 스프라이트보다 카메라 앞쪽인 -0.5f로 지정)
        // 클래스 컴포넌트인 PlayerNameTag 조회를 위해 EntityManager.GetComponentObject 사용
        var tagRefQuery = GetEntityQuery(ComponentType.ReadOnly<NameTagReference>(), ComponentType.ReadOnly<PlayerNameTag>());
        var tagRefEntities = tagRefQuery.ToEntityArray(Allocator.Temp);
        
        foreach (var entity in tagRefEntities)
        {
            var tagRef = EntityManager.GetComponentData<NameTagReference>(entity);
            var nameTag = EntityManager.GetComponentObject<PlayerNameTag>(entity);

            if (nameTag == null) continue;
            if (!EntityManager.Exists(tagRef.TargetEntity)) continue;

            var transform = EntityManager.GetComponentData<LocalTransform>(tagRef.TargetEntity);
            // 약간 왼쪽으로 한 번 더 이동 (X + 0.6f -> +0.4f)
            nameTag.transform.position = new Vector3(transform.Position.x + 0.2f, transform.Position.y + 0.25f, -0.5f);
        }
        tagRefEntities.Dispose();

        // 4. 캐릭터 소멸 시 이름표 파괴
        var cleanupEcb = new EntityCommandBuffer(Allocator.Temp);
        var cleanupQuery = GetEntityQuery(ComponentType.ReadOnly<NameTagReference>(), ComponentType.ReadOnly<PlayerNameTag>());
        var cleanupEntities = cleanupQuery.ToEntityArray(Allocator.Temp);

        foreach (var entity in cleanupEntities)
        {
            var tagRef = EntityManager.GetComponentData<NameTagReference>(entity);
            var nameTag = EntityManager.GetComponentObject<PlayerNameTag>(entity);

            if (!EntityManager.Exists(tagRef.TargetEntity))
            {
                if (nameTag != null)
                {
                    Object.Destroy(nameTag.gameObject);
                }
                cleanupEcb.DestroyEntity(entity);
            }
        }
        cleanupEntities.Dispose();

        cleanupEcb.Playback(EntityManager);
        cleanupEcb.Dispose();
    }
}
