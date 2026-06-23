using Unity.Entities;
using Unity.Collections;

// 1. 서버-클라이언트 간 동기화할 플레이어 닉네임 데이터 구조체
public struct PlayerName : IComponentData
{
    // 최대 32바이트(영문 기준 약 32자, 한글 10자 내외)의 고속 동기화 문자열 구조체 사용
    public FixedString32Bytes Value;
}
