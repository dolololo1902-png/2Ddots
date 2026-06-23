using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

// 1. 클라이언트(플레이어) 월드에서 작동하는 시스템입니다.
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct GoToGameSystem : ISystem
{
    // RPC 요청을 이미 보냈는지 체크하는 로컬 컴포넌트 구조체
    public struct SentGoToGameRequest : IComponentData { }

    public void OnCreate(ref SystemState state)
    {
        // 2. 디바이스 접속 정보가 식별될 때까지 대기합니다.
        state.RequireForUpdate<NetworkId>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 3. NetworkId가 생성되었지만, 아직 서버에 GoToGameRequest를 보내지 않은 엔티티를 쿼리합니다.
        foreach (var (networkId, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<SentGoToGameRequest>().WithEntityAccess())
        {
            // 중복 요청 방지를 위해 태그 컴포넌트 추가
            ecb.AddComponent<SentGoToGameRequest>(entity);

            // 가상 클라이언트 대응을 위해 아직 NetworkStreamInGame이 없다면 추가해 줍니다.
            if (!state.EntityManager.HasComponent<NetworkStreamInGame>(entity))
            {
                ecb.AddComponent<NetworkStreamInGame>(entity);
            }

            // 서버로 게임 진입 RPC 전송 요청 생성
            var request = ecb.CreateEntity();
            ecb.AddComponent(request, new GoToGameRequest());
            ecb.AddComponent(request, new SendRpcCommandRequest { TargetConnection = entity });
            
            UnityEngine.Debug.Log($"[GoToGameSystem] NetworkId {networkId.ValueRO.Value} 클라이언트가 서버로 스폰(GoToGameRequest)을 요청했습니다!");
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
