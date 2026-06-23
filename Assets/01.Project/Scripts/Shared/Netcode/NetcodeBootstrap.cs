using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public class NetcodeBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        // 멀티플레이어 환경에서 백그라운드 포커스 유실 시 타임아웃 차단을 위해 항상 활성화합니다.
        Application.runInBackground = true;

        // 포트 번호 고정
        AutoConnectPort = 7979;

        return base.Initialize(defaultWorldName);
    }
}