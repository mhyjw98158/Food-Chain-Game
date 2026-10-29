using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CharacterData;

// 결과 화면의 플레이어 한 칸: 캐릭터 그림(색 + 의상) 위에 직업과 닉네임을 보여 준다.
// 승리자는 스포트라이트와 바닥 조명을, 패배자는 원형 배경을 켠다
public class ResultSlotUI : MonoBehaviour
{
    [Header("Parts")]
    [SerializeField] private GameObject spotlight;
    [SerializeField] private GameObject floorLight;
    [SerializeField] private GameObject loserBackdrop;
    [SerializeField] private Image bodyImage;
    [SerializeField] private Image overlayImage;
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private TextMeshProUGUI nicknameText;

    [Header("Look")]
    [Tooltip("의상에 몸 그림이 없을 때 쓰는 기본 몸 그림(GamePlayer 프리팹의 몸 그림과 같게)")]
    [SerializeField] private Sprite defaultBodySprite;
    [Tooltip("패배자 캐릭터를 어둡게 할 때 곱하는 밝기(1이면 그대로)")]
    [Range(0f, 1f)]
    [SerializeField] private float loserBrightness = 0.7f;
    [Tooltip("승리 쪽(밝은 배경) 글자 색")]
    [SerializeField] private Color winnerTextColor = new Color(0.1f, 0.1f, 0.1f);
    [Tooltip("패배 쪽(어두운 배경) 글자 색")]
    [SerializeField] private Color loserTextColor = Color.white;
    [Tooltip("포식자 직업 글자 색")]
    [SerializeField] private Color predatorRoleColor = new Color(0.95f, 0.25f, 0.25f);
    [Tooltip("내 닉네임 색")]
    [SerializeField] private Color localNicknameColor = new Color(1f, 0.6f, 0.1f);

    public void Setup(GameResultEntry entry, bool isLocal)
    {
        if (spotlight != null) spotlight.SetActive(entry.isWinner);
        if (floorLight != null) floorLight.SetActive(entry.isWinner);
        if (loserBackdrop != null) loserBackdrop.SetActive(!entry.isWinner);

        Color textColor = entry.isWinner ? winnerTextColor : loserTextColor;
        CostumeData costume = CostumeCatalog.Get(entry.costumeIndex);
        float brightness = entry.isWinner ? 1f : loserBrightness;
        Color bodyColor = new Color(entry.color.r * brightness, entry.color.g * brightness, entry.color.b * brightness, 1f);

        if (bodyImage != null)
        {
            Sprite body = costume != null && costume.bodySprite != null ? costume.bodySprite : defaultBodySprite;
            bodyImage.sprite = body;
            bodyImage.enabled = body != null;
            bodyImage.color = bodyColor;
        }

        if (overlayImage != null)
        {
            Sprite overlay = costume != null ? costume.overlaySprite : null;
            overlayImage.sprite = overlay;
            overlayImage.enabled = overlay != null;
            if (overlay != null)
                overlayImage.color = costume.tintOverlay ? bodyColor : new Color(brightness, brightness, brightness, 1f);
        }

        if (roleText != null)
        {
            roleText.text = CharacterConfig.Characters.TryGetValue(entry.animalType, out var info) ? info.DisplayName : "?";
            roleText.color = entry.isPredator ? predatorRoleColor : textColor;
        }

        if (nicknameText != null)
        {
            nicknameText.text = entry.nickname;
            nicknameText.color = isLocal ? localNicknameColor : textColor;
        }
    }
}
