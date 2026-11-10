using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvestigationObject : MonoBehaviour
{
    [Tooltip("서버가 탐색을 허용하는 최대 거리. Scanner의 scanRadius보다 조금 크게 잡아 지연 오차를 허용")]
    [SerializeField] private float interactRange = 2f;

    private static readonly List<InvestigationObject> _instances = new();
    public static IReadOnlyList<InvestigationObject> All => _instances;

    private Collider2D _bodyCollider;

    public static bool IsAnyInRange(Vector2 position)
    {
        for (int i = 0; i < _instances.Count; i++)
        {
            if (_instances[i] != null && _instances[i].IsInRange(position))
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
    }

    private void OnDestroy()
    {
        _instances.Remove(this);
    }

    // 상호작용 키로만 시작한다(PlayerMove.Interaction → Scanner). 오브젝트 클릭으로는 시작하지 않는다
}
