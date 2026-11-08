using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 공개 방 목록의 한 줄: 제목, 맵, 인원, 입장 버튼
public class RoomListRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI mapText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button joinButton;

    [Tooltip("{0}: 현재 인원, {1}: 최대 인원")]
    [SerializeField] private string countFormat = "{0} / {1}명";

    private string _code;
    private Action<string> _onJoin;

    private void Awake()
    {
        if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
    }

    public void Bind(PublicRoomInfo room, Action<string> onJoin)
    {
        _code = room.code;
        _onJoin = onJoin;

        // 제목은 방장이 보낸 그대로 서버에 저장되므로 여기서 서식 태그를 막는다
        if (titleText != null) titleText.text = TextSanitizer.BlockRichText(CleanTitle(room.title));
        if (mapText != null) mapText.text = MapCatalog.Instance.DisplayName(room.mapId);
        if (countText != null) countText.text = string.Format(countFormat, room.current, room.max);
        if (joinButton != null) joinButton.interactable = room.current < room.max;
    }

    public void SetInteractable(bool value)
    {
        if (joinButton != null) joinButton.interactable = value;
    }

    private void OnJoinClicked()
    {
        if (!string.IsNullOrEmpty(_code)) _onJoin?.Invoke(_code);
    }

    private static string CleanTitle(string title)
    {
        if (string.IsNullOrEmpty(title)) return "-";
        var sb = new System.Text.StringBuilder(title.Length);
        foreach (char c in title)
        {
            if (!char.IsControl(c)) sb.Append(c);
        }
        return sb.ToString();
    }
}
