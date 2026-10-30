using System.Collections.Generic;
using Mirror;
using UnityEngine;

// 로비의 플레이어 목록 창. 우측 상단 버튼으로 열고, 닫기 버튼과 ESC로 닫는다.
// 입장·퇴장과 닉네임·의상 변경을 따라가도록 열려 있는 동안 주기적으로 다시 맞춘다
public class LobbyPlayerListUI : MonoBehaviour
{
    [SerializeField] private GameObject window;
    [SerializeField] private Transform root;
    [SerializeField] private LobbyPlayerRow rowPrefab;

    [Tooltip("목록의 플레이어 구성을 다시 맞추는 간격(초)")]
    [SerializeField] private float listRefreshInterval = 0.5f;
    [Tooltip("말하는 중 표시를 갱신하는 간격(초)")]
    [SerializeField] private float stateRefreshInterval = 0.1f;

    private readonly Dictionary<RoomPlayer, LobbyPlayerRow> _rows = new();
    private readonly List<RoomPlayer> _players = new();
    private readonly List<RoomPlayer> _removeBuffer = new();

    private float _nextListRefresh;
    private float _nextStateRefresh;

    public void Open()
    {
        if (window == null || window.activeSelf) return;

        window.SetActive(true);
        UIManager.Instance.Push(UIPriority.Modal);
        UIManager.RegisterEscape(window, Close);

        _nextListRefresh = 0f;
        _nextStateRefresh = 0f;
    }

    public void Close()
    {
        if (window == null || !window.activeSelf) return;

        window.SetActive(false);
        UIManager.Instance.Pop(UIPriority.Modal);
        UIManager.UnregisterEscape(window);
        ClearRows();
    }

    private void Update()
    {
        if (window == null || !window.activeSelf) return;

        if (Time.unscaledTime >= _nextListRefresh)
        {
            _nextListRefresh = Time.unscaledTime + listRefreshInterval;
            RefreshList();
        }

        if (Time.unscaledTime >= _nextStateRefresh)
        {
            _nextStateRefresh = Time.unscaledTime + stateRefreshInterval;
            RefreshStates();
        }
    }

    private void RefreshList()
    {
        if (root == null || rowPrefab == null) return;

        _players.Clear();
        _players.AddRange(FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None));
        _players.Sort((a, b) => a.index.CompareTo(b.index));

        RoomPlayer local = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.GetComponent<RoomPlayer>() : null;
        bool localIsHost = local != null && local.isHost;

        for (int i = 0; i < _players.Count; i++)
        {
            RoomPlayer player = _players[i];

            if (!_rows.TryGetValue(player, out var row) || row == null)
            {
                row = Instantiate(rowPrefab, root);
                _rows[player] = row;
            }

            row.Bind(player, player == local, localIsHost && player != local);
            row.transform.SetSiblingIndex(i);
        }

        _removeBuffer.Clear();
        foreach (var player in _rows.Keys)
        {
            if (player == null || !_players.Contains(player)) _removeBuffer.Add(player);
        }
        foreach (var player in _removeBuffer)
        {
            if (_rows[player] != null) Destroy(_rows[player].gameObject);
            _rows.Remove(player);
        }
    }

    private void RefreshStates()
    {
        foreach (var pair in _rows)
        {
            if (pair.Value != null) pair.Value.RefreshState();
        }
    }

    private void ClearRows()
    {
        foreach (var pair in _rows)
        {
            if (pair.Value != null) Destroy(pair.Value.gameObject);
        }
        _rows.Clear();
    }
}
