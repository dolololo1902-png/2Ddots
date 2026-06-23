using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;

// 1. WallTilemap 및 FloorTilemap 오브젝트에 부착하여 타일맵의 2D 렌더러와
//    2D 콜라이더 형태를 ECS 물리 및 렌더링에 적합하도록 변환시키는 베이커 컴포넌트입니다.
public class TilemapPhysicsBaker : MonoBehaviour
{
    public class Baker : Baker<TilemapPhysicsBaker>
    {
        public override void Bake(TilemapPhysicsBaker authoring)
        {
            // TransformUsageFlags를 Dynamic으로 지정하여 렌더러가 올바른 월드 트랜스폼 위치를 추적할 수 있게 합니다.
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // 2. CompositeCollider2D 가 있는 경우에만 물리 콜라이더를 추출하여 빌드합니다.
            var composite = authoring.GetComponent<CompositeCollider2D>();
            if (composite != null)
            {
                List<float3> vertices = new List<float3>();
                
                // 복합 콜라이더 안의 모든 패스(경로)들을 순회합니다.
                for (int i = 0; i < composite.pathCount; i++)
                {
                    Vector2[] pathPoints = new Vector2[composite.GetPathPointCount(i)];
                    composite.GetPath(i, pathPoints);

                    foreach (var point in pathPoints)
                    {
                        // 2D 좌표를 3D float3 좌표로 변환하여 물리 데이터에 적합하게 삽입합니다. (Z 두께는 0.1f)
                        vertices.Add(new float3(point.x, point.y, 0f));
                        vertices.Add(new float3(point.x, point.y, 0.1f));
                    }
                }

                if (vertices.Count >= 3)
                {
                    // 4. [메모리 변환] C# List 데이터를 고속 ECS 연산용 NativeArray 형식으로 변환합니다.
                    using (var nativeVertices = new NativeArray<float3>(vertices.ToArray(), Allocator.Temp))
                    {
                        // 5. [정석적인 Convex 물리 콜라이더 생성]
                        var collisionFilter = CollisionFilter.Default;
                        var convexCollider = Unity.Physics.ConvexCollider.Create(
                            nativeVertices,
                            ConvexHullGenerationParameters.Default,
                            collisionFilter
                        );

                        // 6. 생성한 물리 콜라이더 자산을 엔티티에 직접 주입합니다.
                        AddComponent(entity, new PhysicsCollider
                        {
                            Value = convexCollider
                        });
                    }
                }
            }

            // 7. [중요] 타일맵 벽은 고정되어 움직이지 않으므로, 
            //    속도 및 질량 컴포넌트를 등록하지 않아 자동으로 완벽한 Static 물리 벽체로 작동시킵니다.
        }
    }
}
