using Unity.Entities;   // 1. 유니티 ECS 핵심 기능을 씁니다.
using Unity.NetCode;    // 2. 유니티 최신 네트워킹 기능을 씁니다.
using UnityEngine;

// [클래스 설정] 유니티가 이 클래스를 네트워킹 시작 지점으로 인식하도록 만듭니다.
[UnityEngine.Scripting.Preserve]
public class NetcodeBootstrap : ClientServerBootstrap // ConnectionBootstrap에서 수정되었습니다.
{
    // 4. 게임이 시작될 때 전반적인 초기화 및 월드 생성을 담당하는 함수입니다.
    public override bool Initialize(string defaultWorldName)
    {
        // 5. 플레이어가 서버에 연결할 인터넷 포트 번호(7979)를 설정해요.
        AutoConnectPort = 7979;

        // 6. 부모 클래스(ClientServerBootstrap)의 기본 초기화 기능을 그대로 실행하고 반환해요.
        // 이 과정에서 클라이언트/서버 월드가 생성되고 포트가 적용됩니다.
        return base.Initialize(defaultWorldName);
    }
}