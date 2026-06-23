using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct PlayerSpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // 1. [유니티 6 대응] GhostCollection 싱글톤이 생성될 때까지 대기합니다.
        state.RequireForUpdate<GhostCollection>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // 2. [유니티 6 대응] GhostCollection을 가져옵니다.
        var ghostCollection = SystemAPI.GetSingleton<GhostCollection>();

        // 3. [유니티 6 대응] GhostCollection 내부의 프리팹 목록(GhostCollectionPrefab Buffer)에 접근할 엔티티를 찾습니다.
        var collectionEntity = SystemAPI.GetSingletonEntity<GhostCollection>();
        var ghostPrefabs = state.EntityManager.GetBuffer<GhostCollectionPrefab>(collectionEntity);

        // 4. 게임 스트림이 시작된(NetworkStreamInGame) 연결 중, 아직 스폰되지 않은 대상(NetworkId)을 찾습니다.
        foreach (var (networkId, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithAll<NetworkStreamInGame>().WithNone<NetworkIdSpawning>().WithEntityAccess())
        {
            // 5. [유니티 6 대응] GhostCollectionPrefab 하위에서 우리의 고양이 "PlayerPrefab"의 Entity ID를 찾습니다.
            Entity playerPrefab = Entity.Null;
            for (int i = 0; i < ghostPrefabs.Length; ++i)
            {
                var prefabEntity = ghostPrefabs[i].GhostPrefab;
                var prefabName = state.EntityManager.GetName(prefabEntity);
                UnityEngine.Debug.Log($"[PlayerSpawnSystem] Ghost Prefab Index: {i}, Entity: {prefabEntity}, Name: '{prefabName}'");
                
                // 이름에 "PlayerPrefab"이 포함되어 있는지 확인합니다.
                if (prefabName.Contains("PlayerPrefab"))
                {
                    playerPrefab = prefabEntity;
                    break;
                }
            }

            // 못 찾았다면 아직 프리팹이 로드/베이킹되지 않았으므로 다음 프레임에 재시도합니다.
            if (playerPrefab == Entity.Null)
            {
                UnityEngine.Debug.LogWarning("[PlayerSpawnSystem] PlayerPrefab을 아직 GhostCollection에서 찾지 못했습니다. 다음 프레임에 재시도합니다.");
                continue;
            }

            // 플레이어 캐릭터 생성
            var playerInstance = ecb.Instantiate(playerPrefab);

            ecb.SetComponent(playerInstance, new LocalTransform
            {
                Position = new float3(7f, 7f, 0f),
                Rotation = quaternion.identity,
                Scale = 1f
            });

            ecb.AddComponent(playerInstance, new GhostOwner { NetworkId = networkId.ValueRO.Value });

            // 6. [추가] 고스트 플레이어 캐릭터 생성 시, 각 클라이언트의 Network ID 번호를 이름 뒤에 붙여 동기화합니다.
            //    예: Network ID가 1이면 "Player 1", 2이면 "Player 2"
            FixedString32Bytes customName = $"Player {networkId.ValueRO.Value}";
            ecb.SetComponent(playerInstance, new PlayerName { Value = customName });
            
            // 정상적으로 생성이 완료된 경우에만 스폰 완료 표시를 해줍니다.
            ecb.AddComponent<NetworkIdSpawning>(entity);
            UnityEngine.Debug.Log($"[PlayerSpawnSystem] NetworkId {networkId.ValueRO.Value}에 대해 성공적으로 PlayerPrefab을 스폰했습니다! (닉네임: {customName})");
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

public struct NetworkIdSpawning : IComponentData { }