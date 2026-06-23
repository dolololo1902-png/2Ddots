using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct PlayerMovementSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // 물리 연산을 수행할 PhysicsWorldSingleton 싱글톤이 준비될 때까지 시스템 갱신을 지연시킵니다.
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    public unsafe void OnUpdate(ref SystemState state)
    {
        float speed = 5.0f;
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        // ECS 물리 월드를 안전하게 쿼리하기 위한 싱글톤 획득
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        // 캐릭터 엔티티의 트랜스폼 및 입력을 가져옵니다. (동적 캐스트를 위해 PhysicsCollider 정보도 조회할 수 있게 쿼리)
        foreach (var (transform, input, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<PlayerInputComponent>>().WithEntityAccess())
        {
            float3 moveInput = new float3(input.ValueRO.Movement.x, input.ValueRO.Movement.y, 0f);
            if (math.lengthsq(moveInput) < 0.001f)
            {
                continue;
            }

            float3 moveDirection = math.normalize(moveInput);
            float3 displacement = moveDirection * speed * deltaTime;

            // 만약 캐릭터 엔티티에 고유 Collider가 있다면 캐스트 연산으로 충돌 검사 실행
            if (SystemAPI.HasComponent<PhysicsCollider>(entity))
            {
                var collider = SystemAPI.GetComponent<PhysicsCollider>(entity);
                
                // 캐릭터의 현재 위치와 방향을 설정
                ColliderCastInput castInput = new ColliderCastInput
                {
                    Collider = collider.ColliderPtr,
                    Start = transform.ValueRO.Position,
                    End = transform.ValueRO.Position + displacement,
                    Orientation = transform.ValueRO.Rotation
                };

                // 벽 충돌을 미리 쓸어넘기며 검출(ColliderCast)
                if (physicsWorld.CastCollider(castInput, out ColliderCastHit hit))
                {
                    // 닿은 시점의 충돌 지점 법선 벡터(Normal)를 구해 미끄러지도록(Sliding) 가속도 축 보정
                    float3 hitNormal = new float3(hit.SurfaceNormal.x, hit.SurfaceNormal.y, 0f);
                    hitNormal = math.normalize(hitNormal);

                    // 남은 이동 벡터에서 벽면 성분을 수직 투영하여 제거 (슬라이딩 처리)
                    float dot = math.dot(displacement, hitNormal);
                    displacement -= hitNormal * dot;

                    // 2차 충돌 예방 및 약간의 끼임 방지를 위해 여유 간격을 둡니다.
                    displacement *= math.max(0f, hit.Fraction - 0.01f);
                }
            }

            // 최종 보정된 변위만큼 안전하게 이동 적용
            transform.ValueRW.Position += displacement;
        }
    }
}
