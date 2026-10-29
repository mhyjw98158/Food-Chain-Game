using Mirror;
using System.Collections.Generic;
using UnityEngine;

// 로비의 커스터마이징 오브젝트: 가까이 가면 초록색으로 빛나고, 상호작용 키를 누르면 커스터마이징 창을 연다.
// 로비에서는 Scanner가 없으므로 PlayerMove.Interaction이 TryInteract를 직접 부른다
public class CustomizeObject : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] renderers;

    [Tooltip("상호작용할 수 있는 거리(오브젝트 콜라이더 가장자리 기준)")]
    [SerializeField] private float interactRange = 1.5f;
    [SerializeField] private Color highlightColor = Color.green;

    private static readonly List<CustomizeObject> _instances = new();

    private Color[] _defaultColors;
    private Collider2D _bodyCollider;
    private bool _highlighted;

    // 범위 안의 커스터마이징 오브젝트가 있으면 창을 열고 true
    public static bool TryInteract(Vector2 position)
    {
        for (int i = 0; i < _instances.Count; i++)
        {
            if (_instances[i] == null || !_instances[i].IsInRange(position)) continue;

            if (GameRoomUI.Instance != null) GameRoomUI.Instance.OpenColorUI();
            return true;
        }
        return false;
    }

    public bool IsInRange(Vector2 position)
    {
        Vector2 closest = (_bodyCollider != null && _bodyCollider.enabled)
            ? _bodyCollider.ClosestPoint(position)
            : (Vector2)transform.position;

        return (closest - position).sqrMagnitude <= interactRange * interactRange;
    }

    private void Awake()
    {
        _bodyCollider = GetComponent<Collider2D>();
        _instances.Add(this);

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>();

        _defaultColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            _defaultColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
    }

    private void OnDestroy()
    {
        _instances.Remove(this);
    }

    private void Update()
    {
        var local = NetworkClient.localPlayer;
        SetHighlight(local != null && IsInRange(local.transform.position));
    }

    private void SetHighlight(bool on)
    {
        if (_highlighted == on) return;
        _highlighted = on;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].color = on ? highlightColor : _defaultColors[i];
        }
    }
}
