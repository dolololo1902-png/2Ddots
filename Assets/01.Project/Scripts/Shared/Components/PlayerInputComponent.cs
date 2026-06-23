using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

// [GhostComponent]를 붙여주면, 이 입력 데이터를 네트워크를 통해 서버와 클라이언트가 서로 주고받을 수 있게 돼요.
// [InputSubroutine]은 이 컴포넌트가 '키보드 입력 정보'라는 것을 유니티 넷코드 엔진에게 친절하게 알려주는 표시예요.
[GhostComponent]
public struct PlayerInputComponent : IInputComponentData
{
    // 플레이어가 움직이고 싶어 하는 방향을 X와 Y 좌표(예: X=1이면 오른쪽, Y=-1이면 아래쪽)로 저장해요.
    public float2 Movement;
}