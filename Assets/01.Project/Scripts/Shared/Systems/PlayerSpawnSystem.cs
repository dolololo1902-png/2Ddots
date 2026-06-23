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
        // 2. [유니티 6 대응] GhostCollection을 가져옵니다.
        var ghostCollection = SystemAPI.GetSingleton<GhostCollection>();

        // 3. [유니티 6 대응] GhostCollection 내부의 프리팹 목록(GhostCollectionPrefab Buffer)에 접근할 엔티티를 찾습니다.
        var collectionEntity = SystemAPI.GetSingletonEntity<GhostCollection>();
        var ghostPrefabs = state.EntityManager.GetBuffer<GhostCollectionPrefab>(collectionEntity);

        // 스폰 대상들을 담을 임시 리스트 선언 (구조적 변경 오류 방지)
        var spawnTargets = new NativeList<SpawnTargetData>(Allocator.Temp);

        // 4. 게임 스트림이 시작된(NetworkStreamInGame) 연결 중, 아직 스폰되지 않은 대상(NetworkId)을 찾습니다.
        foreach (var (networkId, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithAll<NetworkStreamInGame>().WithNone<NetworkIdSpawning>().WithEntityAccess())
        {
            spawnTargets.Add(new SpawnTargetData 
            { 
                ConnectionEntity = entity, 
                NetworkIdValue = networkId.ValueRO.Value 
            });
        }

        if (spawnTargets.Length > 0)
        {
            // 5. PlayerPrefab 찾기
            Entity playerPrefab = Entity.Null;
            for (int i = 0; i < ghostPrefabs.Length; ++i)
            {
                var prefabEntity = ghostPrefabs[i].GhostPrefab;
                if (state.EntityManager.Exists(prefabEntity))
                {
                    var prefabName = state.EntityManager.GetName(prefabEntity);
                    if (prefabName.Contains("PlayerPrefab"))
                    {
                        playerPrefab = prefabEntity;
                        break;
                    }
                }
            }

            // 프리팹을 찾은 경우에만 실제 스폰 진행
            if (playerPrefab != Entity.Null)
            {
                for (int i = 0; i < spawnTargets.Length; ++i)
                {
                    var target = spawnTargets[i];
                    
                    // 즉시 스폰 및 컴포넌트 데이터 기입
                    var playerInstance = state.EntityManager.Instantiate(playerPrefab);

                    var initialTransform = new LocalTransform
                    {
                        Position = new float3(7f, 7f, 0f),
                        Rotation = quaternion.identity,
                        Scale = 1f
                    };

                    if (state.EntityManager.HasComponent<LocalTransform>(playerInstance))
                        state.EntityManager.SetComponentData(playerInstance, initialTransform);
                    else
                        state.EntityManager.AddComponentData(playerInstance, initialTransform);

                    var ghostOwnerData = new GhostOwner { NetworkId = target.NetworkIdValue };
                    if (state.EntityManager.HasComponent<GhostOwner>(playerInstance))
                        state.EntityManager.SetComponentData(playerInstance, ghostOwnerData);
                    else
                        state.EntityManager.AddComponentData(playerInstance, ghostOwnerData);

                    FixedString32Bytes customName = $"Player {target.NetworkIdValue}";
                    var playerNameData = new PlayerName { Value = customName };
                    if (state.EntityManager.HasComponent<PlayerName>(playerInstance))
                        state.EntityManager.SetComponentData(playerInstance, playerNameData);
                    else
                        state.EntityManager.AddComponentData(playerInstance, playerNameData);

                    // 스폰 처리 완료 플래그 안전하게 추가 (더 이상 스폰 쿼리에 걸리지 않음)
                    state.EntityManager.AddComponent<NetworkIdSpawning>(target.ConnectionEntity);
                    UnityEngine.Debug.Log($"[PlayerSpawnSystem] NetworkId {target.NetworkIdValue}에 대해 성공적으로 PlayerPrefab을 즉시 안전하게 스폰했습니다!");
                }
            }
        }

        spawnTargets.Dispose();
    }

    private struct SpawnTargetData
    {
        public Entity ConnectionEntity;
        public int NetworkIdValue;
    }
}

public struct NetworkIdSpawning : IComponentData { }