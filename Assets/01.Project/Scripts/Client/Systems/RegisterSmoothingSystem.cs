using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;

// 1. 클라이언트 월드(ClientSimulation)에서 기동 시 작동합니다.
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct RegisterSmoothingSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // 2. Netcode 보간용 싱글톤이 준비될 때까지 대기합니다.
        state.RequireForUpdate<GhostPredictionSmoothing>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // 3. 싱글톤을 가져와 LocalTransform 컴포넌트에 대한 기본 위치 보간 기능(DefaultTranslationSmoothingAction)을 등록합니다.
        if (SystemAPI.TryGetSingleton<GhostPredictionSmoothing>(out var smoothing))
        {
            smoothing.RegisterSmoothingAction<LocalTransform>(state.EntityManager, DefaultTranslationSmoothingAction.Action);
        }

        // 4. 최초 1회만 등록하면 되므로 이 시스템을 즉시 비활성화합니다.
        state.Enabled = false;
    }
}
