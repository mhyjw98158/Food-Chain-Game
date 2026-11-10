using TMPro;
using UnityEngine;

// 화면 아래의 상호작용 키 안내("[Space] 미션 시작"). 씬마다 하나 두고, 대상을 정하는 쪽(Scanner, CustomizeObject)이 띄운다
public class InteractPromptUI : MonoBehaviour
{
    private static InteractPromptUI _instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI promptText;
    [Tooltip("{0}: 상호작용 키, {1}: 행동 이름")]
    [SerializeField] private string format = "[{0}] {1} 시작";

    private void Awake()
    {
        _instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public static void Show(string actionName)
    {
        if (_instance == null || _instance.panel == null) return;

        // 키 설정을 바꾸면 바로 반영되도록 띄울 때마다 키를 다시 읽는다
        string key = KeySetting.GetKey(KeyAction.INTERACT).ToUserFriendlyString();
        string text = string.Format(_instance.format, key, actionName);

        if (_instance.promptText != null && _instance.promptText.text != text)
            _instance.promptText.text = text;
        if (!_instance.panel.activeSelf)
            _instance.panel.SetActive(true);
    }

    public static void Hide()
    {
        if (_instance != null && _instance.panel != null && _instance.panel.activeSelf)
            _instance.panel.SetActive(false);
    }
}
