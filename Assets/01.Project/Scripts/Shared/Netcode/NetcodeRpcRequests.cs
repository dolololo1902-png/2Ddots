using Unity.NetCode;

// 1. 유니티 6 Netcode에서 RPC 전송이 가능하도록 public으로 선언하며 더미 데이터를 포함시킵니다.
public struct GoToGameRequest : IRpcCommand
{
    public byte Dummy;
}