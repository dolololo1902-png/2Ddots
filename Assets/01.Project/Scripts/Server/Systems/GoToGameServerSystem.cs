using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct GoToGameServerSystem : ISystem
{
    // 이미 스폰 처리를 완료했는지 기록하는 서버용 태그 컴포넌트
    private struct SpawnProcessed : IComponentData { }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 서버 월드에 존재하는 모든 클라이언트 접속 엔티티(NetworkId) 중 아직 인게임 처리가 안 된 대상 조회
        foreach (var (networkId, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<SpawnProcessed>().WithEntityAccess())
        {
            // 중복 처리 방지 마크
            ecb.AddComponent<SpawnProcessed>(entity);

            // 해당 클라이언트를 인게임 활성화 상태로 전환 (이로 인해 PlayerSpawnSystem이 동작하여 고양이를 스폰합니다.)
            ecb.AddComponent<NetworkStreamInGame>(entity);

            UnityEngine.Debug.Log($"[GoToGameServerSystem] 서버가 클라이언트(NetworkId: {networkId.ValueRO.Value})의 스폰 처리를 직접 트리거했습니다!");
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}