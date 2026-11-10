using UnityEngine;
using UnityEngine.UI;

// 게임 씬 채팅창의 + 버튼: 채팅 채널 목록을 숨기거나 보인다(처음에는 숨김).
// 목록이 숨겨진 동안 귓속말을 받으면 알림음을 내고 버튼을 노란색으로 바꾸며, 목록을 열면 원래 색으로 돌아온다
public class ChatChannelToggle : MonoBehaviour
{
    [SerializeField] private GameObject channelList;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color alertColor = new Color(1f, 0.85f, 0.1f);

    [Header("Alert Sound")]
    [Tooltip("귓속말 알림음. 비워 두면 짧은 알림음을 코드로 만들어 쓴다")]
    [SerializeField] private AudioClip alertClip;
    [SerializeField] private float generatedToneFrequency = 880f;
    [SerializeField] private float generatedToneSeconds = 0.15f;

    private AudioSource _audio;
    private bool _alerting;

    public bool IsListVisible => channelList != null && channelList.activeSelf;

    private void Awake()
    {
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        if (channelList != null) channelList.SetActive(false);
        ApplyColor();
    }

    // + 버튼(OnClick)에 연결
    public void Toggle()
    {
        if (channelList == null) return;

        channelList.SetActive(!channelList.activeSelf);
        if (channelList.activeSelf) _alerting = false;
        ApplyColor();
    }

    // 다른 사람에게서 귓속말을 받았을 때 ChatManager가 부른다
    public void NotifyWhisperReceived()
    {
        if (IsListVisible) return;

        _alerting = true;
        ApplyColor();
        PlayAlert();
    }

    private void ApplyColor()
    {
        if (buttonImage != null) buttonImage.color = _alerting ? alertColor : normalColor;
    }

    private void PlayAlert()
    {
        if (alertClip == null) alertClip = CreateTone();
        if (alertClip == null) return;

        if (_audio == null)
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.bypassListenerEffects = true;
        }

        // UI 효과음 음량(설정·음소거 반영)을 따른다
        float volume = AudioManager.Instance != null ? AudioManager.Instance.uiSfxVolume : 1f;
        if (volume <= 0f) return;
        _audio.PlayOneShot(alertClip, volume);
    }

    // 두 번 울리는 짧은 알림음(임시)
    private AudioClip CreateTone()
    {
        const int sampleRate = 44100;
        int beepSamples = Mathf.Max(1, Mathf.RoundToInt(generatedToneSeconds * sampleRate));
        int gapSamples = beepSamples / 2;
        int total = beepSamples * 2 + gapSamples;
        var data = new float[total];

        for (int beep = 0; beep < 2; beep++)
        {
            int start = beep * (beepSamples + gapSamples);
            float freq = generatedToneFrequency * (beep == 0 ? 1f : 1.25f);
            for (int i = 0; i < beepSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Min(1f, i / (sampleRate * 0.01f)) * (1f - (float)i / beepSamples);
                data[start + i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
            }
        }

        var clip = AudioClip.Create("WhisperAlert", total, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
