using Mirror;
using UnityEngine;
using UnityEngine.UI;

// 로비 플레이어 목록 한 줄: 닉네임, 캐릭터 모습(색 + 의상), 음량·음소거, 추방 버튼.
// 음량·음소거와 닉네임은 설정 창 음성 목록과 같은 LobbyVoiceParticipantRow가 처리한다
public class LobbyPlayerRow : MonoBehaviour
{
    [SerializeField] private LobbyVoiceParticipantRow voiceRow;
    [Tooltip("음성 ID가 없거나 내 줄이면 숨기는 음량·음소거 오브젝트")]
    [SerializeField] private GameObject[] voiceControls = new GameObject[0];
    [SerializeField] private Image bodyImage;
    [SerializeField] private Image overlayImage;
    [SerializeField] private Button kickButton;
    [SerializeField] private string kickConfirmFormat = "{0}님을 추방하시겠습니까?";

    private RoomPlayer _player;

    private void Awake()
    {
        if (kickButton != null)
            kickButton.onClick.AddListener(OnKickClicked);
    }

    public void Bind(RoomPlayer player, bool isLocal, bool canKick)
    {
        _player = player;

        if (voiceRow != null) voiceRow.Bind(player.voiceId, player.nickname);

        bool showVoice = !isLocal && !string.IsNullOrEmpty(player.voiceId);
        foreach (var control in voiceControls)
        {
            if (control != null && control.activeSelf != showVoice)
                control.SetActive(showVoice);
        }

        if (kickButton != null && kickButton.gameObject.activeSelf != canKick)
            kickButton.gameObject.SetActive(canKick);

        RefreshCharacter();
    }

    // 말하는 중 표시. 로비에는 사망자가 없다
    public void RefreshState()
    {
        if (voiceRow == null || _player == null) return;

        var voice = VoiceManager.Instance;
        bool speaking = voice != null && !string.IsNullOrEmpty(_player.voiceId) && voice.IsSpeakingVisible(_player.voiceId);
        voiceRow.SetState(false, speaking);
    }

    // 몸 그림은 로비 캐릭터가 지금 쓰는 그림(PlayerCostumeView가 의상을 반영해 둔다)을 그대로 쓴다
    private void RefreshCharacter()
    {
        Color color = _player.rpColor;
        color.a = 1f;

        if (bodyImage != null)
        {
            var body = _player.GetComponent<SpriteRenderer>();
            bodyImage.sprite = body != null ? body.sprite : null;
            bodyImage.color = color;
            bodyImage.enabled = bodyImage.sprite != null;
        }

        if (overlayImage != null)
        {
            CostumeData costume = CostumeCatalog.Get(_player.costumeIndex);
            overlayImage.sprite = costume != null ? costume.overlaySprite : null;
            overlayImage.color = costume != null && costume.tintOverlay ? color : Color.white;
            overlayImage.enabled = overlayImage.sprite != null;
        }
    }

    // 추방은 되돌릴 수 없으므로 확인을 받은 뒤에만 보낸다
    private void OnKickClicked()
    {
        RoomPlayer target = _player;
        if (target == null) return;

        ReturnToTitleConfirmUI.TryShow(string.Format(kickConfirmFormat, target.nickname), () => Kick(target));
    }

    // 확인을 기다리는 사이에 대상이 나갔을 수 있다. 방장 여부와 대상은 서버(RoomPlayer.CmdKickPlayer)가 다시 검증한다
    private static void Kick(RoomPlayer target)
    {
        if (target == null || NetworkClient.localPlayer == null) return;

        var local = NetworkClient.localPlayer.GetComponent<RoomPlayer>();
        if (local != null)
            local.CmdKickPlayer(target.netId);
    }
}
