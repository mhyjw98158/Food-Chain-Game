using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 타이틀의 공개 방 목록 창. Node 서버(GET /rooms)에서 공개·대기 중·자리 있는 방만 받아 보여 주고,
// 입장은 코드 입장과 같은 흐름(TitleUI.JoinRoom → 닉네임 확인 → 코드 조회 → 접속)을 쓴다.
// 열려 있는 동안 일정 간격으로 다시 불러온다. 비공개 방은 목록에 없다(코드로만 입장)
public class RoomListUI : MonoBehaviour
{
    [SerializeField] private TitleUI titleUI;
    [SerializeField] private GameObject window;
    [SerializeField] private Transform listRoot;
    [Tooltip("한 줄 원본(꺼 둔 상태). 복제해서 쓴다")]
    [SerializeField] private RoomListRow rowTemplate;
    [SerializeField] private TextMeshProUGUI statusText;

    [Tooltip("열려 있는 동안 목록을 다시 불러오는 간격(초). 0이면 새로고침 버튼으로만")]
    [SerializeField] private float autoRefreshSeconds = 5f;

    [Header("문구")]
    [SerializeField] private string loadingText = "불러오는 중";
    [SerializeField] private string emptyText = "공개된 방이 없습니다.";
    [SerializeField] private string joiningText = "입장하는 중";

    private readonly List<RoomListRow> _rows = new List<RoomListRow>();
    private bool _loading;
    private bool _joining;
    private float _nextRefreshTime;

    private void Awake()
    {
        if (window != null) window.SetActive(false);
        if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
    }

    public void Open()
    {
        if (window == null || titleUI == null) return;

        window.SetActive(true);
        // 타이틀의 ESC 처리가 이 창을 닫도록 맡긴다
        titleUI.activeUi = window;
        _joining = false;
        Refresh();
    }

    public void Close()
    {
        if (window == null) return;

        window.SetActive(false);
        if (titleUI != null && titleUI.activeUi == window) titleUI.activeUi = null;
    }

    public void Refresh()
    {
        if (_loading || titleUI == null || titleUI.RoomJoinService == null) return;

        _loading = true;
        _nextRefreshTime = Time.unscaledTime + Mathf.Max(1f, autoRefreshSeconds);
        if (_rows.Count == 0) SetStatus(loadingText);

        titleUI.RoomJoinService.FetchPublicRooms((success, rooms, error) =>
        {
            _loading = false;
            // 응답을 기다리는 사이 씬이 바뀌었거나 창이 닫혔으면 무시한다
            if (this == null || window == null || !window.activeSelf || _joining) return;

            if (!success)
            {
                ClearRows();
                SetStatus(error);
                return;
            }

            ShowRooms(rooms);
        });
    }

    private void Update()
    {
        if (window == null || !window.activeSelf || _joining || autoRefreshSeconds <= 0f) return;
        if (Time.unscaledTime >= _nextRefreshTime) Refresh();
    }

    private void ShowRooms(PublicRoomInfo[] rooms)
    {
        ClearRows();

        if (rooms == null || rooms.Length == 0)
        {
            SetStatus(emptyText);
            return;
        }

        SetStatus("");
        foreach (var room in rooms)
        {
            if (room == null || string.IsNullOrEmpty(room.code)) continue;

            var row = Instantiate(rowTemplate, listRoot);
            row.gameObject.SetActive(true);
            row.Bind(room, OnJoinRequested);
            _rows.Add(row);
        }
    }

    private void OnJoinRequested(string code)
    {
        if (_joining || titleUI == null) return;

        // 닉네임이 없거나 규칙에 어긋나면 TitleUI가 닉네임 창을 띄우고 false를 돌려준다. 그때는 목록을 닫아 창이 겹치지 않게 한다
        if (!titleUI.JoinRoom(code))
        {
            if (titleUI.activeUi != window) window.SetActive(false);
            return;
        }

        _joining = true;
        SetStatus(joiningText);
        foreach (var row in _rows) row.SetInteractable(false);
        // 입장에 실패하면 TitleUI가 오류 창을 띄운다. 목록은 닫아서 오류 창만 보이게 한다
        Close();
    }

    private void ClearRows()
    {
        foreach (var row in _rows)
        {
            if (row != null) Destroy(row.gameObject);
        }
        _rows.Clear();
    }

    private void SetStatus(string text)
    {
        if (statusText == null) return;
        statusText.text = text ?? "";
        statusText.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }
}
