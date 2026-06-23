using Unity.Entities;
using UnityEngine;

public class NetcodeSpawnerAuthoring : MonoBehaviour
{
    // 스폰할 플레이어 고스트 프리팹을 에디터에서 연결합니다.
    public GameObject PlayerPrefab;

    public class NetcodeSpawnerBaker : Baker<NetcodeSpawnerAuthoring>
    {
        public override void Bake(NetcodeSpawnerAuthoring authoring)
        {
            Debug.Log($"[NetcodeSpawnerBaker] Bake starting. PlayerPrefab present: {authoring.PlayerPrefab != null}");
            if (authoring.PlayerPrefab != null)
            {
                Debug.Log($"[NetcodeSpawnerBaker] Baking PlayerPrefab: {authoring.PlayerPrefab.name}");
                // 프리팹을 GetEntity를 호출해 베이킹 시스템에 알려줍니다.
                GetEntity(authoring.PlayerPrefab, TransformUsageFlags.Dynamic);
            }
            
            var entity = GetEntity(TransformUsageFlags.None);
        }
    }
}
