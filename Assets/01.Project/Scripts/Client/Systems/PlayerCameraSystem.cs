using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;

// 1. 클라이언트 월드(ClientSimulation)에서만 작동하는 카메라 시스템입니다.
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
// 2. 최종 렌더링(프레임 출력) 직전 단계인 PresentationSystemGroup에서 카메라 위치를 업데이트하여 뚝뚝 끊기는 현상을 억제합니다.
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class PlayerCameraSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // 디버그용으로 GhostOwnerIsLocal과 PlayerInputComponent를 가진 엔티티 개수를 확인해 봅니다.
        int localOwnersCount = 0;
        int inputComponentsCount = 0;
        
        foreach (var _ in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<GhostOwnerIsLocal>())
        {
            localOwnersCount++;
        }
        foreach (var _ in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerInputComponent>())
        {
            inputComponentsCount++;
        }

        if (UnityEngine.Time.frameCount % 60 == 0) // 매 60프레임마다 한 번씩만 출력
        {
            Debug.Log($"[PlayerCameraSystem] GhostOwnerIsLocal 개수: {localOwnersCount}, PlayerInputComponent 개수: {inputComponentsCount}");
        }

        // 3. 로컬 플레이어가 소유한 엔티티(GhostOwnerIsLocal을 보유한 대상)의 최종 렌더링 트랜스폼(LocalToWorld)을 가져옵니다.
        foreach (var localToWorld in SystemAPI.Query<RefRO<LocalToWorld>>().WithAll<GhostOwnerIsLocal>())
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                var playerPos = localToWorld.ValueRO.Position;
                
                // Z축은 2D 카메라 기준인 -10f를 유지하며 부드럽게 쫓아갑니다.
                Vector3 targetPosition = new Vector3(playerPos.x, playerPos.y, -10f);
                
                // 프레임 레이트(FPS) 변화에 영향받지 않는 부드러운 지수 보간(Exponential Decay) 사용
                // 10.0f는 카메라가 따라가는 속도 조절 값입니다.
                float t = 1.0f - Mathf.Exp(-10.0f * UnityEngine.Time.deltaTime);
                
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, t);
            }
        }
    }
}
