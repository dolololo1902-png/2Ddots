using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

// 1. 오직 '서버 컴퓨터(ServerSimulation)'에서만 이 수신 및 소환 승인 시스템을 가동합니다.
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct GoToGameServerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 2. 클라이언트들이 보낸 'GoToGameRequest' 편지 상자(RPC)를 모두 뒤집니다.
        // ReceiveRpcCommandRequest는 넷코드가 "누가 보냈는지" 정보를 담아두는 우표 같은 컴포넌트예요.
        foreach (var (request, rpcSource, entity) in SystemAPI.Query<RefRO<GoToGameRequest>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            // 3. 편지를 보낸 주체(클라이언트의 연결 Entity)를 식별하여, 
            // 그 플레이어 상태에 "이 유저는 인게임 접속이 허가되었다(NetworkStreamInGame)" 표시를 붙여요.
            ecb.AddComponent<NetworkStreamInGame>(rpcSource.ValueRO.SourceConnection);

            // 4. [매우 중요] 이미 처리한 요청 편지(RPC Entity)는 즉시 폭파(Destroy)시켜 없애요.
            // 안 지우면 아까처럼 'RPC Entity가 소비되지 않았다'는 경고와 함께 서버에 과부하가 걸려요!
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}