using Unity.Entities;
using UnityEngine;

public class PlayerNameAuthoring : MonoBehaviour
{
    public class PlayerNameBaker : Baker<PlayerNameAuthoring>
    {
        public override void Bake(PlayerNameAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // 닉네임 문자열 컴포넌트만 심플하게 주입합니다.
            AddComponent(entity, new PlayerName { Value = "Player" });
        }
    }
}
