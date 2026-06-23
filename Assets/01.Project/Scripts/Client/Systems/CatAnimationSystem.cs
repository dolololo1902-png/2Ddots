using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class CatAnimationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // [중요] 원격 플레이어의 경우 PlayerInputComponent가 복제되지 않아 걷기/방향 전환 애니메이션이 나오지 않는 문제가 있었습니다.
        // 이를 해결하기 위해 입력값 대신 실제 캐릭터의 위치 변화량(Displacement)을 기반으로 애니메이션과 스프라이트 좌우 반전을 처리합니다.
        foreach (var (animState, transform, entity) in SystemAPI.Query<RefRW<CatAnimationState>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            if (!SystemAPI.ManagedAPI.HasComponent<SpriteRenderer>(entity) || !SystemAPI.ManagedAPI.HasComponent<CatVisuals>(entity))
                continue;

            var renderer = SystemAPI.ManagedAPI.GetComponent<SpriteRenderer>(entity);
            var visuals = SystemAPI.ManagedAPI.GetComponent<CatVisuals>(entity);

            if (renderer == null || visuals == null)
                continue;

            float3 curPos = transform.ValueRO.Position;

            // 스폰 후 첫 프레임 위치 초기화 보정
            if (math.all(animState.ValueRO.LastPosition == float3.zero))
            {
                animState.ValueRW.LastPosition = curPos;
            }

            float3 displacement = curPos - animState.ValueRO.LastPosition;
            animState.ValueRW.LastPosition = curPos;

            // 2D 상의 실제 이동 속도가 일정 수준 이상인지 판별
            float movementSq = displacement.x * displacement.x + displacement.y * displacement.y;
            bool isMovingNow = movementSq > 0.0001f;

            // 대기 <-> 걷기 상태 전환 시 프레임 인덱스 및 타이머 리셋
            if (isMovingNow != animState.ValueRO.IsMoving)
            {
                animState.ValueRW.IsMoving = isMovingNow;
                animState.ValueRW.CurrentFrame = 0;
                animState.ValueRW.Timer = 0f;
            }

            // 애니메이션 타이머 업데이트
            animState.ValueRW.Timer += deltaTime;
            if (visuals.FrameRate > 0f && animState.ValueRO.Timer >= visuals.FrameRate)
            {
                animState.ValueRW.Timer -= visuals.FrameRate;
                animState.ValueRW.CurrentFrame++;
            }

            // 스프라이트 업데이트
            Sprite[] currentSheet = isMovingNow ? visuals.WalkSprites : visuals.IdleSprites;
            if (currentSheet != null && currentSheet.Length > 0)
            {
                int index = animState.ValueRO.CurrentFrame % currentSheet.Length;
                renderer.sprite = currentSheet[index];
            }

            // X축의 변위 방향에 따라 시선(Sprite Flip)을 물리적 이동에 직접 맞춰 변경
            if (displacement.x < -0.002f)
            {
                renderer.flipX = true; // 왼쪽 이동 중
            }
            else if (displacement.x > 0.002f)
            {
                renderer.flipX = false; // 오른쪽 이동 중
            }
        }
    }
}
