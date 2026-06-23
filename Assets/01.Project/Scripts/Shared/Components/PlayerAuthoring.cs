using Unity.Entities;
using UnityEngine;

// 1. 플레이어 캐릭터 생성 시 입력용 컴포넌트만 깔끔하게 주입하도록 원복합니다.
public class PlayerAuthoring : MonoBehaviour
{
    public class PlayerBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerInputComponent());
        }
    }
}
