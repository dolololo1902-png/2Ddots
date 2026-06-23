using Unity.Entities;
using UnityEngine;

// 유니티 인스펙터에서 고양이 스프라이트 목록을 설정할 수 있게 해주는 컴포넌트입니다.
public class CatVisuals : MonoBehaviour
{
    [Header("Animations")]
    public Sprite[] IdleSprites;
    public Sprite[] WalkSprites;
    
    [Header("Settings")]
    public float FrameRate = 0.15f; // 프레임당 전환 속도

    public class CatVisualsBaker : Baker<CatVisuals>
    {
        public override void Bake(CatVisuals authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // 1. 애니메이션 재생 상태를 기록할 구조체 컴포넌트를 추가합니다.
            AddComponent(entity, new CatAnimationState());
            
            // 2. 관리형(Managed) 객체인 CatVisuals 자기 자신을 컴포넌트로 등록하여 
            // ECS 시스템에서 스프라이트 리스트에 직접 접근할 수 있도록 만듭니다.
            AddComponentObject(entity, authoring);

            // 3. 프리팹에 붙어있는 SpriteRenderer도 ECS 컴포넌트로 등록해 줍니다.
            var spriteRenderer = authoring.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                AddComponentObject(entity, spriteRenderer);
            }
        }
    }
}

// 고양이 애니메이션 상태 정보 구조체
public struct CatAnimationState : IComponentData
{
    public bool IsMoving;
    public int CurrentFrame;
    public float Timer;
    public Unity.Mathematics.float3 LastPosition;
}
