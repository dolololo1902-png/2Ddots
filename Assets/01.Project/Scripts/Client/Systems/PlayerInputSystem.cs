using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
// 유니티 6 Netcode for Entities에서 입력을 안전하게 수집하는 올바른 시스템 그룹은 GhostInputSystemGroup입니다.
[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial struct PlayerInputSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkId>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float2 moveInput = float2.zero;

        // 1. New Input System 지원
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveInput.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveInput.y -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput.x += 1f;
        }

        // 2. 구형 Input Manager 폴백(Fallback) 지원
        if (math.lengthsq(moveInput) == 0)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveInput.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveInput.y -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveInput.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveInput.x += 1f;
        }

        if (math.lengthsq(moveInput) > 0)
        {
            moveInput = math.normalize(moveInput);
        }

        int entityCount = 0;
        int localOwnedCount = 0;

        // GhostOwnerIsLocal을 필터링하되, 동기화 유실에 대비하여 엔티티를 안전하게 처리합니다.
        foreach (var (inputData, entity) in SystemAPI.Query<RefRW<PlayerInputComponent>>().WithEntityAccess())
        {
            entityCount++;
            // 나에게 소유권(GhostOwnerIsLocal)이 있는 로컬 엔티티에만 입력을 입력합니다.
            if (SystemAPI.HasComponent<GhostOwnerIsLocal>(entity))
            {
                localOwnedCount++;
                inputData.ValueRW.Movement = moveInput;
            }
            else
            {
                // 다른 플레이어의 캐릭터 입력 데이터는 로컬에서 제어하지 않고 비워둡니다.
                inputData.ValueRW.Movement = float2.zero;
            }
        }

        // 입력이 들어왔을 때, 매칭되는 로컬 플레이어 엔티티가 없는 경우 로그 출력
        if (math.lengthsq(moveInput) > 0 && localOwnedCount == 0)
        {
            UnityEngine.Debug.LogWarning($"[{state.World.Name}] Input detected {moveInput}, but no entity with GhostOwnerIsLocal was found! Total PlayerInputComponent entities: {entityCount}");
        }
    }
}
