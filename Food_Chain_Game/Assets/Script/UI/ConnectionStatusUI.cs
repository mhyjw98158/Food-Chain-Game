using Mirror;
using TMPro;
using UnityEngine;

// 방장의 응답이 잠시 끊겼을 때 띄우는 경고. 전송 계층이 연결을 끊을 때까지 멈춘 화면만 보이지 않게 한다.
// 응답이 돌아오면 사라지고, 끝내 돌아오지 않으면 RoomManager가 끊김 사유를 표시한다
public class ConnectionStatusUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI statusText;
    [Tooltip("이 시간(초) 동안 방장에게서 아무 메시지도 받지 못하면 경고를 띄운다. 평소 메시지 간격(최대 2초)보다 커야 한다")]
    [SerializeField] private float warningSeconds = 4f;
    [Tooltip("{0}: 응답이 없었던 시간(초)")]
    [SerializeField] private string messageFormat = "연결이 불안정합니다. 방장의 응답을 기다리는 중... ({0}초)";

    private int _shownSeconds = -1;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (panel == null) return;

        float silentSeconds = GetSilentSeconds();
        bool show = silentSeconds >= warningSeconds;

        if (panel.activeSelf != show)
            panel.SetActive(show);
        if (!show) return;

        int seconds = Mathf.FloorToInt(silentSeconds);
        if (seconds == _shownSeconds) return;

        _shownSeconds = seconds;
        if (statusText != null)
            statusText.text = string.Format(messageFormat, seconds);
    }

    // 방장 본인은 서버를 겸하므로 해당 없다. 씬을 불러오는 동안에는 메시지 처리가 멈추므로 재지 않는다
    private static float GetSilentSeconds()
    {
        if (NetworkServer.active || !NetworkClient.isConnected || NetworkClient.isLoadingScene) return 0f;

        var connection = NetworkClient.connection;
        return connection != null ? Time.time - connection.lastMessageTime : 0f;
    }
}
