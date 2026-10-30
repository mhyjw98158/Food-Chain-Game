using Mirror;
using UnityEngine;

// 클라이언트가 보낸 위치를 서버가 검사한다. 조작감을 유지하려고 이동은 클라이언트가 하고, 서버는 불가능한 이동만 되돌린다.
// 렉으로 위치가 몰려 도착해도 치트로 보지 않도록 시간에 따라 쌓이는 이동 예산으로 판정한다.
// 방장 본인의 캐릭터는 검사하지 않는다(서버가 곧 그 플레이어다)
public class ServerMoveValidator : NetworkBehaviour
{
    [Tooltip("정상 이동 속도에 곱하는 여유. 예산이 쌓이는 속도이자 지속 가능한 최고 속도다")]
    [SerializeField] private float speedTolerance = 1.3f;
    [Tooltip("쌓아 둘 수 있는 이동 예산(월드 단위). 렉 뒤에 위치가 몰려 도착해도 되돌리지 않는 한도다")]
    [SerializeField] private float maxBurstDistance = 5f;
    [Tooltip("되돌린 뒤 클라이언트에 지시가 도착할 때까지 검사를 쉬는 시간(초). 그동안 오는 위치는 되돌리기 전의 값이다")]
    [SerializeField] private float correctionGraceSeconds = 0.5f;
    [Tooltip("살아 있는 플레이어가 들어갈 수 없는 층(벽, 맵 경계)")]
    [SerializeField] private LayerMask wallMask;
    [Tooltip("유령이 들어갈 수 없는 층(맵 경계)")]
    [SerializeField] private LayerMask ghostWallMask;

    private const int MaxOverlapResults = 8;

    private readonly Collider2D[] _overlaps = new Collider2D[MaxOverlapResults];

    private GamePlayer _player;
    private PlayerMove _move;
    private NetworkTransformBase _networkTransform;
    private Rigidbody2D _rigid;

    private Vector2 _lastValidPosition;
    private float _budget;
    private float _graceUntil;

    public override void OnStartServer()
    {
        _player = GetComponent<GamePlayer>();
        _move = GetComponent<PlayerMove>();
        _networkTransform = GetComponent<NetworkTransformBase>();
        _rigid = GetComponent<Rigidbody2D>();

        _lastValidPosition = transform.position;
        _budget = maxBurstDistance;
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        if (isLocalPlayer || _player == null || _move == null) return;

        float refill = _move.moveSpeed * speedTolerance * Time.fixedDeltaTime;
        _budget = Mathf.Min(maxBurstDistance, _budget + refill);

        if (Time.time < _graceUntil) return;

        Vector2 position = transform.position;
        float moved = Vector2.Distance(position, _lastValidPosition);

        if (moved > _budget || IsInsideWall(position))
        {
            Correct();
            return;
        }

        _budget -= moved;
        _lastValidPosition = position;
    }

    // 강체가 없는 차단막(밤 차단막, 하늘 차단막)은 켜지는 시점이 클라이언트마다 달라 판정에서 뺀다.
    // 그 자리에 서 있던 정상 플레이어를 치트로 보지 않기 위해서다
    private bool IsInsideWall(Vector2 position)
    {
        var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true };
        filter.SetLayerMask(_player.isAlive ? wallMask : ghostWallMask);

        int count = Physics2D.OverlapPoint(position, filter, _overlaps);
        for (int i = 0; i < count; i++)
        {
            Rigidbody2D body = _overlaps[i].attachedRigidbody;
            if (body != null && body.bodyType == RigidbodyType2D.Static) return true;
        }
        return false;
    }

    private void Correct()
    {
        Debug.LogWarning($"[ServerMoveValidator] {_player.nickname}의 이동을 되돌립니다: {(Vector2)transform.position} → {_lastValidPosition}");

        ApplyPosition(_lastValidPosition);
        _graceUntil = Time.time + correctionGraceSeconds;

        TargetCorrectPosition(_lastValidPosition);
    }

    // 위치는 본인이 동기화하므로(Client→Server) 본인 쪽 위치도 함께 옮긴다
    [TargetRpc]
    private void TargetCorrectPosition(Vector2 position)
    {
        ApplyPosition(position);
    }

    private void ApplyPosition(Vector2 position)
    {
        transform.position = position;

        if (_rigid == null) _rigid = GetComponent<Rigidbody2D>();
        if (_rigid != null)
        {
            _rigid.position = position;
            _rigid.linearVelocity = Vector2.zero;
        }

        if (_networkTransform == null) _networkTransform = GetComponent<NetworkTransformBase>();
        if (_networkTransform != null) _networkTransform.ResetState();
    }
}
