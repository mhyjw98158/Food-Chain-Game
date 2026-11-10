using System.Collections.Generic;
using Mirror;
using UnityEngine;

// GamePlayer 이동을 서버가 정한다(서버 권위). 조작감을 지키려고 클라이언트 예측 + 서버 재조정 방식으로 동작한다.
// - 본인 클라이언트: 매 물리 틱마다 입력(8방향)을 서버로 보내고, 같은 이동 계산으로 먼저 움직인다(예측)
// - 서버: 받은 입력을 같은 계산으로 적용해 진짜 위치를 정하고, 몇 번째 입력까지 반영했는지와 위치를 본인에게 돌려준다
// - 본인 클라이언트: 서버 위치가 예측과 다르면 서버 위치에서 아직 반영 안 된 입력을 다시 계산하고, 차이는 화면에서만 천천히 메운다
// - 다른 클라이언트: 서버 위치를 OwnerPredictedNetworkTransform이 보간해서 보여준다
// 이동 계산(Simulate)은 물리 엔진을 돌리지 않고 콜라이더 캐스트로 직접 하므로 서버와 클라이언트가 같은 결과를 낸다.
// 방장 본인 캐릭터는 서버에서 바로 움직인다
[RequireComponent(typeof(Rigidbody2D))]
public class ServerAuthMovement : NetworkBehaviour
{
    [Header("충돌")]
    [Tooltip("벽에서 띄워 둘 거리(월드 단위). 너무 작으면 벽에 끼고, 너무 크면 벽 앞에서 멈춘 게 보인다")]
    [SerializeField] private float skinWidth = 0.02f;
    [Tooltip("벽을 따라 미끄러질 때 한 틱에 다시 계산하는 최대 횟수")]
    [SerializeField] private int maxSlideIterations = 3;

    [Header("입력 전송")]
    [Tooltip("한 번에 함께 보내는 최근 입력 수(1~8). 패킷이 몇 개 사라져도 서버가 입력을 놓치지 않게 한다")]
    [SerializeField] private int inputRedundancy = 4;

    [Header("서버 검증")]
    [Tooltip("렉 뒤에 몰려 도착한 입력을 한 번에 처리할 수 있는 최대 틱 수. 이보다 빨리 입력을 보내는 클라이언트(스피드핵)는 따라가지 못한다")]
    [SerializeField] private int maxBurstInputs = 25;
    [Tooltip("서버에 쌓아 둘 수 있는 입력 수. 넘으면 오래된 입력부터 버린다(그만큼 클라이언트가 서버 위치로 보정됨)")]
    [SerializeField] private int maxQueuedInputs = 6;

    [Header("보정")]
    [Tooltip("예측과 서버 위치 차이를 이 거리 이하면 무시한다")]
    [SerializeField] private float reconcileTolerance = 0.001f;
    [Tooltip("보정 차이를 화면에서 메우는 시간(초). 클수록 부드럽지만 서버 위치와 오래 어긋나 보인다")]
    [SerializeField] private float correctionSmoothTime = 0.1f;
    [Tooltip("보정 차이가 이보다 크면 메우지 않고 바로 옮긴다")]
    [SerializeField] private float correctionSnapDistance = 2f;

    [Header("방장 화면의 다른 플레이어")]
    [Tooltip("처리한 입력보다 몇 틱 뒤를 보여줄지. 입력이 불규칙하게 도착해도 일정한 속도로 보이게 하는 여유다(1틱 = 물리 틱 1번)")]
    [SerializeField] private float serverDisplayDelayTicks = 3f;
    [Tooltip("표시가 목표보다 늦거나 빠를 때 재생 속도를 바꾸는 최대 비율(0.25면 0.75~1.25배)")]
    [SerializeField] private float serverDisplayMaxRateChange = 0.25f;
    [Tooltip("표시가 목표에서 이 틱 수보다 멀어지면 따라가지 않고 바로 맞춘다")]
    [SerializeField] private float serverDisplaySnapTicks = 25f;

    private const int HistorySize = 256;
    private const int DisplayBufferSize = 64;
    private const int MaxCastResults = 8;
    private const byte NoInput = 4;
    private const string GhostLayerName = "Ghost";

    private readonly RaycastHit2D[] _castResults = new RaycastHit2D[MaxCastResults];

    private Rigidbody2D _rigid;
    private PlayerMove _input;
    private GamePlayer _player;
    private Collider2D _bodyCollider;
    private int _aliveLayer;
    private int _ghostLayer;

    // 이동 계산 기준 위치. 서버에서는 진짜 위치, 본인 클라이언트에서는 예측 위치
    private Vector2 _simPosition;
    private bool _simulating;

    // 본인 클라이언트(예측)
    private uint _inputSeq;
    private uint _lastAckSeq;
    private readonly uint[] _historySeq = new uint[HistorySize];
    private readonly byte[] _historyInput = new byte[HistorySize];
    private readonly Vector2[] _historyPosition = new Vector2[HistorySize];
    private Vector2 _visualOffset;

    // 서버(다른 사람의 캐릭터)
    private struct QueuedInput
    {
        public uint Seq;
        public byte Code;
    }
    private readonly Queue<QueuedInput> _queue = new Queue<QueuedInput>();
    private uint _lastReceivedSeq;
    private uint _lastProcessedSeq;
    private float _inputTokens;
    private double _lastRefillTime;

    // 방장 화면 표시용: 입력 번호별 처리 결과. 입력 번호를 시간축으로 삼아 일정한 속도로 재생한다
    private readonly uint[] _displaySeq = new uint[DisplayBufferSize];
    private readonly Vector2[] _displayPosition = new Vector2[DisplayBufferSize];
    private double _renderSeq;
    private bool _renderStarted;

    // 이동 차단막(하늘 차단막, 밤 차단막). 클라이언트마다 켜지는 조건이 달라서 실제 콜라이더 대신 규칙으로 판정한다
    private readonly HashSet<Collider2D> _barrierColliders = new HashSet<Collider2D>();
    private readonly List<Rect> _skyBarrierRects = new List<Rect>();
    private readonly List<Rect> _nightBarrierRects = new List<Rect>();
    private GamePlayUI _barrierSource;

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody2D>();
        _input = GetComponent<PlayerMove>();
        _player = GetComponent<GamePlayer>();
        _bodyCollider = FindBodyCollider();
        _aliveLayer = gameObject.layer;
        _ghostLayer = LayerMask.NameToLayer(GhostLayerName);

        if (_input == null) Debug.LogError("[ServerAuthMovement] PlayerMove가 없습니다.", this);
        if (_bodyCollider == null) Debug.LogError("[ServerAuthMovement] 몸통 콜라이더(트리거가 아닌 Capsule/Circle/Box)가 없습니다.", this);
    }

    public override void OnStartServer()
    {
        _simPosition = transform.position;
        _inputTokens = maxBurstInputs;
        _lastRefillTime = Time.realtimeSinceStartupAsDouble;
        _simulating = true;
    }

    public override void OnStartLocalPlayer()
    {
        _simPosition = transform.position;
        _visualOffset = Vector2.zero;
        _simulating = true;
    }

    private void FixedUpdate()
    {
        if (!_simulating) return;

        if (isServer)
        {
            if (isLocalPlayer) HostLocalTick();
            else ServerRemoteTick();
        }
        else if (isLocalPlayer)
        {
            ClientPredictTick();
        }
    }

    private void Update()
    {
        // 방장 화면에서 다른 플레이어는 입력이 불규칙하게 도착해 계단식으로 움직이므로, 처리 결과를 입력 번호 순서대로
        // 조금 늦게 일정한 속도로 재생한다. 이 위치가 다른 클라이언트에도 전송된다(판정 기준은 _simPosition)
        if (!_simulating || !isServer || isLocalPlayer || _lastProcessedSeq == 0) return;

        double target = _lastProcessedSeq - serverDisplayDelayTicks;
        if (!_renderStarted || System.Math.Abs(target - _renderSeq) > serverDisplaySnapTicks)
        {
            _renderSeq = target;
            _renderStarted = true;
        }
        else
        {
            // 목표보다 늦으면 조금 빨리, 앞서면 조금 느리게 재생해 지연을 일정하게 유지한다
            double diff = target - _renderSeq;
            double rate = 1.0 + System.Math.Max(-serverDisplayMaxRateChange, System.Math.Min(serverDisplayMaxRateChange, diff * 0.1));
            _renderSeq += Time.deltaTime / Time.fixedDeltaTime * rate;
        }

        if (_renderSeq > _lastProcessedSeq) _renderSeq = _lastProcessedSeq;

        uint from = (uint)System.Math.Floor(_renderSeq);
        float t = (float)(_renderSeq - from);
        if (!TryGetDisplayPosition(from, out Vector2 a)) return;
        Vector2 next = TryGetDisplayPosition(from + 1, out Vector2 b) ? Vector2.Lerp(a, b, t) : a;

        transform.position = new Vector3(next.x, next.y, transform.position.z);
    }

    private void RecordDisplayPosition(uint seq, Vector2 position)
    {
        int index = (int)(seq % DisplayBufferSize);
        _displaySeq[index] = seq;
        _displayPosition[index] = position;
    }

    private bool TryGetDisplayPosition(uint seq, out Vector2 position)
    {
        int index = (int)(seq % DisplayBufferSize);
        position = _displayPosition[index];
        return _displaySeq[index] == seq && seq != 0;
    }

    private float MoveSpeed => _input != null ? _input.moveSpeed : 0f;

    // ───────────────────────── 방장 본인 ─────────────────────────

    private void HostLocalTick()
    {
        byte code = EncodeInput(_input != null ? _input.movement : Vector2.zero);
        _simPosition = Simulate(_simPosition, ServerFilterInput(code), Time.fixedDeltaTime);
        _rigid.MovePosition(_simPosition);
    }

    // ───────────────────────── 본인 클라이언트(예측) ─────────────────────────

    private void ClientPredictTick()
    {
        // 라운드가 진행 중이 아니면(시작 전, 결과 화면) 어차피 움직일 수 없으므로 입력을 보내지 않는다.
        // 결과 화면 뒤 씬 전환 때 서버가 먼저 준비 해제한 연결로 입력이 도착해 경고가 나는 것도 이것으로 막는다
        if (!NetworkClient.ready) return;
        if (GameMamager.Instance != null && !GameMamager.Instance.isRoundActive) return;

        _inputSeq++;
        byte code = EncodeInput(_input != null ? _input.movement : Vector2.zero);

        _simPosition = Simulate(_simPosition, DecodeInput(code), Time.fixedDeltaTime);

        int index = (int)(_inputSeq % HistorySize);
        _historySeq[index] = _inputSeq;
        _historyInput[index] = code;
        _historyPosition[index] = _simPosition;

        SendInputs();

        // 보정 차이는 화면에서만 천천히 메운다(이동 계산은 항상 _simPosition 기준)
        if (_visualOffset != Vector2.zero)
        {
            float decay = correctionSmoothTime > 0f ? Mathf.Exp(-Time.fixedDeltaTime / correctionSmoothTime) : 0f;
            _visualOffset *= decay;
            if (_visualOffset.sqrMagnitude < 0.00001f) _visualOffset = Vector2.zero;
        }

        _rigid.MovePosition(_simPosition + _visualOffset);
    }

    // 최근 입력 몇 개를 4비트씩 묶어 보낸다(0번이 가장 최근)
    private void SendInputs()
    {
        int count = (int)Mathf.Min(Mathf.Clamp(inputRedundancy, 1, 8), _inputSeq);
        uint packed = 0;
        for (int i = 0; i < count; i++)
        {
            uint seq = _inputSeq - (uint)i;
            packed |= (uint)(_historyInput[seq % HistorySize] & 0xF) << (i * 4);
        }

        CmdSendInputs(_inputSeq, packed, (byte)count);
    }

    // 서버가 seq번 입력까지 반영한 위치를 알려 준다
    [TargetRpc(channel = Channels.Unreliable)]
    private void TargetAckState(uint seq, Vector2 serverPosition)
    {
        if (isServer || !_simulating) return;
        // 순서가 뒤바뀌어 도착한 옛 결과는 버린다
        if (seq <= _lastAckSeq) return;
        _lastAckSeq = seq;

        int index = (int)(seq % HistorySize);
        if (_historySeq[index] != seq || seq > _inputSeq)
        {
            // 기록이 없을 만큼 오래된 결과: 서버 위치로 옮긴다
            ResyncTo(serverPosition, seq, true);
            return;
        }

        if ((_historyPosition[index] - serverPosition).sqrMagnitude <= reconcileTolerance * reconcileTolerance)
            return;

        ResyncTo(serverPosition, seq, false);
    }

    // 서버 위치에서 아직 반영되지 않은 입력을 다시 계산해 예측을 고친다
    private void ResyncTo(Vector2 serverPosition, uint ackSeq, bool snap)
    {
        Vector2 before = _simPosition;
        Vector2 position = serverPosition;

        for (uint seq = ackSeq + 1; seq <= _inputSeq; seq++)
        {
            int index = (int)(seq % HistorySize);
            if (_historySeq[index] != seq) continue;

            position = Simulate(position, DecodeInput(_historyInput[index]), Time.fixedDeltaTime);
            _historyPosition[index] = position;
        }

        _simPosition = position;

        Vector2 jump = before - _simPosition;
        _visualOffset = snap || (_visualOffset + jump).magnitude > correctionSnapDistance
            ? Vector2.zero
            : _visualOffset + jump;
    }

    // ───────────────────────── 서버(다른 사람의 캐릭터) ─────────────────────────

    [Command(channel = Channels.Unreliable)]
    private void CmdSendInputs(uint newestSeq, uint packed, byte count)
    {
        if (count == 0 || count > 8) return;
        if (newestSeq <= _lastReceivedSeq) return;

        uint oldestInPacket = newestSeq - (uint)(count - 1);
        if (newestSeq < count) oldestInPacket = 1;

        // 패킷이 연속으로 사라져 빠진 입력은 남아 있는 가장 오래된 입력으로 채운다(틀리면 클라이언트가 보정된다)
        byte fillCode = (byte)((packed >> ((int)(newestSeq - oldestInPacket) * 4)) & 0xF);
        uint start = _lastReceivedSeq + 1;
        if (oldestInPacket > start + (uint)maxQueuedInputs)
            start = oldestInPacket - (uint)maxQueuedInputs;

        for (uint seq = start; seq <= newestSeq; seq++)
        {
            byte code = seq >= oldestInPacket
                ? (byte)((packed >> ((int)(newestSeq - seq) * 4)) & 0xF)
                : fillCode;
            _queue.Enqueue(new QueuedInput { Seq = seq, Code = code });
        }

        _lastReceivedSeq = newestSeq;

        // 명령을 몰아 보내도 메모리가 늘지 않게 자른다(남은 것은 다음 틱에 정리)
        while (_queue.Count > maxBurstInputs + maxQueuedInputs)
        {
            _lastProcessedSeq = _queue.Dequeue().Seq;
            RecordDisplayPosition(_lastProcessedSeq, _simPosition);
        }
    }

    private void ServerRemoteTick()
    {
        // 처리 한도: 실제 시간 한 틱마다 1개씩 쌓이고 maxBurstInputs까지 모아 둔다. 실제 시간보다 빨리 움직일 수 없다.
        // 물리 틱 수가 아니라 실제 시간으로 채운다(방장 PC가 잠깐 멈춰 틱을 건너뛰어도 입력이 밀리지 않게)
        double now = Time.realtimeSinceStartupAsDouble;
        float refill = (float)((now - _lastRefillTime) / Time.fixedDeltaTime);
        _lastRefillTime = now;
        _inputTokens = Mathf.Min(maxBurstInputs, _inputTokens + refill);

        bool changed = false;
        while (_queue.Count > 0 && _inputTokens >= 1f)
        {
            QueuedInput queued = _queue.Dequeue();
            _inputTokens -= 1f;
            _simPosition = Simulate(_simPosition, ServerFilterInput(queued.Code), Time.fixedDeltaTime);
            _lastProcessedSeq = queued.Seq;
            RecordDisplayPosition(queued.Seq, _simPosition);
            changed = true;
        }

        // 한도를 넘게 쌓인 입력은 버린다. 클라이언트는 다음 결과를 받고 서버 위치로 보정된다
        while (_queue.Count > maxQueuedInputs)
        {
            _lastProcessedSeq = _queue.Dequeue().Seq;
            RecordDisplayPosition(_lastProcessedSeq, _simPosition);
            changed = true;
        }

        if (changed && connectionToClient != null)
            TargetAckState(_lastProcessedSeq, _simPosition);
    }

    // 서버가 막는 입력. 라운드가 진행 중이 아닐 때(시작 연출, 결과)는 움직이지 않는다
    private Vector2 ServerFilterInput(byte code)
    {
        if (code > 8) return Vector2.zero;
        if (GameMamager.Instance != null && !GameMamager.Instance.isRoundActive) return Vector2.zero;
        return DecodeInput(code);
    }

    /// <summary>서버에서 캐릭터를 순간이동시킨다. 게임 중 위치를 옮기는 기능은 transform 대신 이것을 쓴다</summary>
    [Server]
    public void ServerTeleport(Vector2 destination)
    {
        _simPosition = destination;
        _queue.Clear();
        _lastProcessedSeq = _lastReceivedSeq;
        // 표시도 미끄러지지 않고 바로 옮긴다
        RecordDisplayPosition(_lastProcessedSeq, destination);
        _renderStarted = false;

        transform.position = new Vector3(destination.x, destination.y, transform.position.z);
        _rigid.position = destination;

        if (connectionToClient != null && !isLocalPlayer)
            TargetTeleport(_lastProcessedSeq, destination);
    }

    [TargetRpc]
    private void TargetTeleport(uint ackSeq, Vector2 destination)
    {
        if (isServer) return;

        if (ackSeq > _lastAckSeq) _lastAckSeq = ackSeq;
        ResyncTo(destination, ackSeq, true);
        transform.position = new Vector3(_simPosition.x, _simPosition.y, transform.position.z);
        _rigid.position = _simPosition;
    }

    // ───────────────────────── 이동 계산(서버·클라이언트 공용) ─────────────────────────

    // 한 틱 이동. 벽에 닿으면 벽을 따라 미끄러진다. 같은 입력이면 서버와 클라이언트가 같은 결과를 낸다
    private Vector2 Simulate(Vector2 start, Vector2 direction, float deltaTime)
    {
        if (_bodyCollider == null) return start;

        bool alive = _player == null || _player.isAlive;
        // 멈춰 있어도 차단막은 판정한다(밤 차단막이 켜질 때 통로에 서 있던 플레이어를 밀어냄)
        if (direction == Vector2.zero) return alive ? ResolveBarriers(start, start) : start;

        int layer = alive || _ghostLayer < 0 ? _aliveLayer : _ghostLayer;
        int mask = Physics2D.GetLayerCollisionMask(layer);

        Vector2 position = start;
        Vector2 delta = direction * MoveSpeed * deltaTime;

        for (int i = 0; i < maxSlideIterations && delta.sqrMagnitude > 0.0000001f; i++)
        {
            float distance = delta.magnitude;
            Vector2 moveDir = delta / distance;

            if (!CastNearest(position, moveDir, distance + skinWidth, mask, out RaycastHit2D hit))
            {
                position += delta;
                break;
            }

            float travel = Mathf.Max(0f, hit.distance - skinWidth);
            position += moveDir * travel;

            Vector2 remaining = moveDir * (distance - travel);
            delta = remaining - Vector2.Dot(remaining, hit.normal) * hit.normal;
        }

        if (alive) position = ResolveBarriers(start, position);
        return position;
    }

    private bool CastNearest(Vector2 position, Vector2 direction, float distance, int mask, out RaycastHit2D nearest)
    {
        nearest = default;

        var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true };
        filter.SetLayerMask(mask);

        int count = CastBody(position, direction, distance, filter);
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = _castResults[i];
            Collider2D other = hit.collider;
            if (other == null) continue;
            if (other.attachedRigidbody == _rigid) continue;
            if (_barrierColliders.Contains(other)) continue;
            // 이미 맞닿은 벽에서 멀어지는 방향은 막지 않는다
            if (Vector2.Dot(hit.normal, direction) >= 0f) continue;

            if (!found || hit.distance < nearest.distance)
            {
                nearest = hit;
                found = true;
            }
        }
        return found;
    }

    private int CastBody(Vector2 position, Vector2 direction, float distance, ContactFilter2D filter)
    {
        Vector2 scale = transform.lossyScale;
        scale = new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        Vector2 origin = position + Vector2.Scale(_bodyCollider.offset, scale);

        switch (_bodyCollider)
        {
            case CapsuleCollider2D capsule:
                return Physics2D.CapsuleCast(origin, Vector2.Scale(capsule.size, scale), capsule.direction, 0f, direction, filter, _castResults, distance);
            case CircleCollider2D circle:
                return Physics2D.CircleCast(origin, circle.radius * Mathf.Max(scale.x, scale.y), direction, filter, _castResults, distance);
            case BoxCollider2D box:
                return Physics2D.BoxCast(origin, Vector2.Scale(box.size, scale), 0f, direction, filter, _castResults, distance);
            default:
                return 0;
        }
    }

    private Collider2D FindBodyCollider()
    {
        foreach (var col in GetComponents<Collider2D>())
        {
            if (col.isTrigger) continue;
            if (col is CapsuleCollider2D || col is CircleCollider2D || col is BoxCollider2D) return col;
        }
        return null;
    }

    private Vector2 BodyHalfExtents()
    {
        Vector2 scale = transform.lossyScale;
        scale = new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        switch (_bodyCollider)
        {
            case CapsuleCollider2D capsule: return Vector2.Scale(capsule.size, scale) * 0.5f;
            case CircleCollider2D circle: return Vector2.one * circle.radius * Mathf.Max(scale.x, scale.y);
            case BoxCollider2D box: return Vector2.Scale(box.size, scale) * 0.5f;
            default: return Vector2.zero;
        }
    }

    // ───────────────────────── 이동 차단막 ─────────────────────────

    // 하늘 차단막: 날지 못하는 동물. 밤 차단막: 밤 동안 모두. 둘 다 산 사람만(유령은 통과)
    private Vector2 ResolveBarriers(Vector2 from, Vector2 to)
    {
        CacheBarriers();
        if (_barrierSource == null) return to;

        Vector2 half = BodyHalfExtents();

        if (_player != null && !_player.isFly)
            to = ResolveRects(_skyBarrierRects, half, from, to);

        if (GameMamager.Instance != null && GameMamager.Instance.IsNightPhase)
            to = ResolveRects(_nightBarrierRects, half, from, to);

        return to;
    }

    private static Vector2 ResolveRects(List<Rect> rects, Vector2 half, Vector2 from, Vector2 to)
    {
        for (int i = 0; i < rects.Count; i++)
        {
            Rect r = rects[i];
            Rect area = Rect.MinMaxRect(r.xMin - half.x, r.yMin - half.y, r.xMax + half.x, r.yMax + half.y);
            if (!StrictlyInside(area, to)) continue;

            if (StrictlyInside(area, from))
            {
                // 차단막이 켜질 때 그 안에 있었다: 가장 가까운 바깥으로 밀어낸다
                to = PushOut(area, to);
                continue;
            }

            float edgeX = from.x <= area.center.x ? area.xMin : area.xMax;
            float edgeY = from.y <= area.center.y ? area.yMin : area.yMax;

            if (!StrictlyInside(area, new Vector2(from.x, to.y))) to.x = edgeX;      // 가로로 들어왔다: 세로로만 미끄러짐
            else if (!StrictlyInside(area, new Vector2(to.x, from.y))) to.y = edgeY; // 세로로 들어왔다: 가로로만 미끄러짐
            else to = new Vector2(edgeX, edgeY);
        }
        return to;
    }

    private static bool StrictlyInside(Rect area, Vector2 p)
    {
        return p.x > area.xMin && p.x < area.xMax && p.y > area.yMin && p.y < area.yMax;
    }

    private static Vector2 PushOut(Rect area, Vector2 p)
    {
        float left = p.x - area.xMin;
        float right = area.xMax - p.x;
        float down = p.y - area.yMin;
        float up = area.yMax - p.y;
        float min = Mathf.Min(Mathf.Min(left, right), Mathf.Min(down, up));

        if (min == left) return new Vector2(area.xMin, p.y);
        if (min == right) return new Vector2(area.xMax, p.y);
        if (min == down) return new Vector2(p.x, area.yMin);
        return new Vector2(p.x, area.yMax);
    }

    // 차단막은 GamePlayUI의 skyBlock, block 아래 BoxCollider2D다. 꺼져 있어도 위치를 읽을 수 있다
    private void CacheBarriers()
    {
        GamePlayUI ui = GamePlayUI.Instance;
        if (ui == null || ui == _barrierSource) return;

        _barrierSource = ui;
        _barrierColliders.Clear();
        _skyBarrierRects.Clear();
        _nightBarrierRects.Clear();

        AddBarrier(ui.skyBlock, _skyBarrierRects);
        AddBarrier(ui.block, _nightBarrierRects);
    }

    private void AddBarrier(GameObject root, List<Rect> rects)
    {
        if (root == null) return;

        foreach (var box in root.GetComponentsInChildren<BoxCollider2D>(true))
        {
            if (box.isTrigger) continue;
            _barrierColliders.Add(box);

            Vector2 scale = box.transform.lossyScale;
            Vector2 size = new Vector2(Mathf.Abs(box.size.x * scale.x), Mathf.Abs(box.size.y * scale.y));
            Vector2 center = box.transform.TransformPoint(box.offset);
            rects.Add(new Rect(center - size * 0.5f, size));
        }
    }

    // ───────────────────────── 입력 부호화 ─────────────────────────

    // 8방향 + 정지를 0~8로. 4가 정지
    private static byte EncodeInput(Vector2 movement)
    {
        int x = movement.x > 0.01f ? 1 : movement.x < -0.01f ? -1 : 0;
        int y = movement.y > 0.01f ? 1 : movement.y < -0.01f ? -1 : 0;
        return (byte)((x + 1) * 3 + (y + 1));
    }

    private static Vector2 DecodeInput(byte code)
    {
        if (code > 8 || code == NoInput) return Vector2.zero;
        int x = code / 3 - 1;
        int y = code % 3 - 1;
        return new Vector2(x, y).normalized;
    }
}
