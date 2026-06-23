using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class CatAnimationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // 1. [최신 규격 대응] 값 타입인 CatAnimationState와 PlayerInputComponent를 쿼리하면서 Entity 정보를 함께 가져옵니다.
        foreach (var (animState, input, entity) in SystemAPI.Query<RefRW<CatAnimationState>, RefRO<PlayerInputComponent>>().WithEntityAccess())
        {
            // 2. SystemAPI.ManagedAPI를 통해 엔티티에 부착된 관리형 클래스 컴포넌트를 가져옵니다.
            if (!SystemAPI.ManagedAPI.HasComponent<SpriteRenderer>(entity) || !SystemAPI.ManagedAPI.HasComponent<CatVisuals>(entity))
                continue;

            var renderer = SystemAPI.ManagedAPI.GetComponent<SpriteRenderer>(entity);
            var visuals = SystemAPI.ManagedAPI.GetComponent<CatVisuals>(entity);

            if (renderer == null || visuals == null)
                continue;

            // 이동 방향 입력이 있는지 감지
            bool isMovingNow = math.lengthsq(input.ValueRO.Movement) > 0.01f;

            // 정지 <-> 걷기 상태가 전환되면 타이머 초기화
            if (isMovingNow != animState.ValueRO.IsMoving)
            {
                animState.ValueRW.IsMoving = isMovingNow;
                animState.ValueRW.CurrentFrame = 0;
                animState.ValueRW.Timer = 0f;
            }

            // 프레임 애니메이션 타이머 진행
            animState.ValueRW.Timer += deltaTime;
            if (visuals.FrameRate > 0f && animState.ValueRO.Timer >= visuals.FrameRate)
            {
                animState.ValueRW.Timer -= visuals.FrameRate;
                animState.ValueRW.CurrentFrame++;
            }

            // 상태에 맞춰 스프라이트 배열 결정
            Sprite[] currentSheet = isMovingNow ? visuals.WalkSprites : visuals.IdleSprites;
            if (currentSheet != null && currentSheet.Length > 0)
            {
                int index = animState.ValueRO.CurrentFrame % currentSheet.Length;
                renderer.sprite = currentSheet[index];
            }

            // 이동 방향(X축 값)에 따라 고양이의 시선(Sprite Flip) 설정
            if (input.ValueRO.Movement.x < -0.01f)
            {
                renderer.flipX = true; // 왼쪽 이동
            }
            else if (input.ValueRO.Movement.x > 0.01f)
            {
                renderer.flipX = false; // 오른쪽 이동
            }
        }
    }
}
