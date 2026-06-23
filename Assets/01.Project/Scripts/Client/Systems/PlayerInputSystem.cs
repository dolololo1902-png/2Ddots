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
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveInput.y += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveInput.y -= 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveInput.x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveInput.x += 1f;

        if (math.lengthsq(moveInput) > 0)
        {
            moveInput = math.normalize(moveInput);
        }

        foreach (var inputData in SystemAPI.Query<RefRW<PlayerInputComponent>>())
        {
            inputData.ValueRW.Movement = moveInput;
        }
    }
}
