using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

// 1. 클라이언트(플레이어) 단에서만 이 요청 시스템을 실행합니다.
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct GoToGameSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // 2. 이미 연결 요청을 보냈거나 연결이 완료되었는지 확인하기 위한 대기 설정입니다.
        state.RequireForUpdate<NetworkId>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 3. 아직 게임 서버에 본격적으로 진입(GoToGame)하지 않은 연결망(NetworkId)을 찾습니다.
        // (WithNone<NetworkStreamInGame>을 통해 이미 인게임 상태인 경우는 걸러냅니다.)
        foreach (var (networkId, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>().WithEntityAccess())
        {
            // 4. 이 연결 Entity에 "나 이제 진짜 인게임 들어갈게(NetworkStreamInGame)" 라는 컴포넌트를 붙여줍니다.
            ecb.AddComponent<NetworkStreamInGame>(entity);

            // 5. 서버에 "이 플레이어가 게임 씬에 정상 진입했다"고 통보해 주는 요청용 새 Entity(Request)를 생성해요.
            var request = ecb.CreateEntity();
            // 6. 이 요청이 '인게임 진입 요청(SpawnPlayerRpc)'임을 마킹하고, 보낸 대상을 지정해요.
            ecb.AddComponent(request, new GoToGameRequest());
            ecb.AddComponent(request, new SendRpcCommandRequest { TargetConnection = entity });
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

