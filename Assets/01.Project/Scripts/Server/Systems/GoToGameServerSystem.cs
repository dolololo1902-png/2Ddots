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

        // 클라이언트로부터 수신된 GoToGameRequest RPC 엔티티를 쿼리합니다.
        foreach (var (request, rpcSource, entity) in SystemAPI.Query<RefRO<GoToGameRequest>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            // RPC를 수신한 대상 연결 엔티티
            var connectionEntity = rpcSource.ValueRO.SourceConnection;

            if (state.EntityManager.Exists(connectionEntity) && !state.EntityManager.HasComponent<SpawnProcessed>(connectionEntity))
            {
                // 중복 처리 방지 마크
                ecb.AddComponent<SpawnProcessed>(connectionEntity);

                // 해당 클라이언트를 인게임 활성화 상태로 전환 (이로 인해 PlayerSpawnSystem이 동작하여 고양이를 스폰합니다.)
                ecb.AddComponent<NetworkStreamInGame>(connectionEntity);

                int networkIdVal = -1;
                if (state.EntityManager.HasComponent<NetworkId>(connectionEntity))
                {
                    networkIdVal = state.EntityManager.GetComponentData<NetworkId>(connectionEntity).Value;
                }

                UnityEngine.Debug.Log($"[GoToGameServerSystem] 서버가 클라이언트(NetworkId: {networkIdVal})의 GoToGameRequest RPC를 정상 수신하여 스폰을 처리했습니다!");
            }

            // [중요] 수신된 RPC 엔티티는 프레임 지연 및 메모리 누수(Leak) 방지를 위해 반드시 파괴(Destroy)해야 합니다.
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}