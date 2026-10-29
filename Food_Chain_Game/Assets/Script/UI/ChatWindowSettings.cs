using UnityEngine;

// 채팅창 위치·크기·잠금 상태를 PlayerPrefs에 저장하고, 씬이 열릴 때(로비·게임) 다시 적용한다.
// 로비와 게임 씬의 채팅창이 같은 값을 쓰므로 씬이 바뀌어도, 게임을 껐다 켜도 그대로 유지된다.
// 바뀐 값은 매 프레임 비교해 메모리에 기록하고, 디스크에는 창이 사라질 때(씬 전환·종료) 쓴다
[RequireComponent(typeof(RectTransform))]
public class ChatWindowSettings : MonoBehaviour
{
    private const string KeyPosX = "ChatWindow.PosX";
    private const string KeyPosY = "ChatWindow.PosY";
    private const string KeyWidth = "ChatWindow.Width";
    private const string KeyHeight = "ChatWindow.Height";
    private const string KeyLocked = "ChatWindow.Locked";

    [SerializeField] private UIDraggable draggable;
    [Tooltip("크기 조절 손잡이(가로·세로). 잠금 상태를 함께 맞춘다")]
    [SerializeField] private UIResizable[] resizables = new UIResizable[0];

    private RectTransform _rect;
    private Canvas _canvas;
    private Vector2 _savedPos;
    private Vector2 _savedSize;
    private bool _savedLocked;
    private bool _dirty;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _canvas = GetComponentInParent<Canvas>();
        if (draggable == null) draggable = GetComponent<UIDraggable>();
    }

    private void Start()
    {
        Load();
    }

    private void Load()
    {
        Vector2 size = _rect.sizeDelta;
        if (PlayerPrefs.HasKey(KeyWidth))
            size = new Vector2(PlayerPrefs.GetFloat(KeyWidth), PlayerPrefs.GetFloat(KeyHeight));
        if (resizables.Length > 0 && resizables[0] != null)
        {
            size.x = Mathf.Clamp(size.x, resizables[0].minSize.x, resizables[0].maxSize.x);
            size.y = Mathf.Clamp(size.y, resizables[0].minSize.y, resizables[0].maxSize.y);
        }
        _rect.sizeDelta = size;

        if (PlayerPrefs.HasKey(KeyPosX))
            _rect.anchoredPosition = new Vector2(PlayerPrefs.GetFloat(KeyPosX), PlayerPrefs.GetFloat(KeyPosY));
        ClampToCanvas();

        SetLocked(PlayerPrefs.GetInt(KeyLocked, 0) == 1);

        _savedPos = _rect.anchoredPosition;
        _savedSize = _rect.sizeDelta;
        _savedLocked = IsLocked;
    }

    private bool IsLocked => draggable != null && draggable.isLock;

    private void SetLocked(bool locked)
    {
        if (draggable != null) draggable.SetLock(locked);
        foreach (var r in resizables)
        {
            if (r != null) r.SetLock(locked);
        }
    }

    // 해상도가 바뀌어 저장된 위치가 화면 밖이면 화면 안으로 들인다
    private void ClampToCanvas()
    {
        if (_canvas == null) return;

        var canvasRect = (RectTransform)_canvas.transform;
        Vector2 canvasSize = canvasRect.rect.size;
        Vector2 size = _rect.rect.size;
        Vector2 pos = _rect.anchoredPosition;
        Vector2 anchor = _rect.anchorMin;
        Vector2 pivot = _rect.pivot;

        // 앵커가 한 점(anchorMin == anchorMax)인 창 기준. 화면 왼쪽 아래 기준 좌표로 바꿔서 자른다
        Vector2 origin = Vector2.Scale(canvasSize, anchor);
        Vector2 min = Vector2.Scale(size, pivot) - origin;
        Vector2 max = canvasSize - Vector2.Scale(size, Vector2.one - pivot) - origin;
        pos.x = Mathf.Clamp(pos.x, min.x, Mathf.Max(min.x, max.x));
        pos.y = Mathf.Clamp(pos.y, min.y, Mathf.Max(min.y, max.y));
        _rect.anchoredPosition = pos;
    }

    private void LateUpdate()
    {
        Vector2 pos = _rect.anchoredPosition;
        Vector2 size = _rect.sizeDelta;
        bool locked = IsLocked;
        if (pos == _savedPos && size == _savedSize && locked == _savedLocked) return;

        _savedPos = pos;
        _savedSize = size;
        _savedLocked = locked;

        PlayerPrefs.SetFloat(KeyPosX, pos.x);
        PlayerPrefs.SetFloat(KeyPosY, pos.y);
        PlayerPrefs.SetFloat(KeyWidth, size.x);
        PlayerPrefs.SetFloat(KeyHeight, size.y);
        PlayerPrefs.SetInt(KeyLocked, locked ? 1 : 0);
        _dirty = true;
    }

    private void OnDestroy()
    {
        if (_dirty) PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        if (_dirty) PlayerPrefs.Save();
    }
}
