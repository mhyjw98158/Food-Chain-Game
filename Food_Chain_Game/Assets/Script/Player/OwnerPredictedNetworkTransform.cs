using Mirror;
using UnityEngine;

// 서버 위치를 모든 클라이언트에 보간해 보여주되(Server→Client), 본인 캐릭터에는 적용하지 않는다.
// 본인 캐릭터는 ServerAuthMovement가 예측으로 움직이고 서버 결과로 직접 보정하므로, 여기서 덮어쓰면 늦게 따라가는 조작감이 된다
public class OwnerPredictedNetworkTransform : NetworkTransformReliable
{
    protected override void Configure()
    {
        base.Configure();
        // 위치는 항상 서버가 정한다
        syncDirection = SyncDirection.ServerToClient;
    }

    protected override void OnServerToClientSync(Vector3? position, Quaternion? rotation, Vector3? scale)
    {
        if (isOwned && !isServer) return;
        base.OnServerToClientSync(position, rotation, scale);
    }
}
