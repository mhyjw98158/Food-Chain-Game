using UnityEngine;

// 상호작용 테두리의 모양. 게임과 로비의 모든 오브젝트가 같은 모양을 쓰도록 Resources에 하나만 두고 불러온다
[CreateAssetMenu(menuName = "UI/Interact Outline Settings", fileName = "InteractOutlineSettings")]
public class InteractOutlineSettings : ScriptableObject
{
    public const string ResourcePath = "InteractOutlineSettings";

    [Tooltip("Food Chain/Interact Outline 셰이더를 쓰는 머티리얼")]
    public Material material;
    public Color color = new Color(1f, 0.92f, 0.3f, 1f);

    [Header("Thickness")]
    [Tooltip("또렷한 안쪽 테두리 두께(월드 단위)")]
    public float innerThickness = 0.07f;
    [Tooltip("번지는 바깥쪽 빛 두께(월드 단위)")]
    public float outerThickness = 0.16f;
    [Range(0f, 1f)] public float innerAlpha = 1f;
    [Range(0f, 1f)] public float outerAlpha = 0.3f;

    [Header("Pulse")]
    [Tooltip("빛이 밝아졌다 어두워지는 속도")]
    public float pulseSpeed = 4f;
    [Tooltip("가장 어두울 때의 밝기 비율")]
    [Range(0f, 1f)] public float pulseMinBrightness = 0.6f;

    private static InteractOutlineSettings _instance;
    private static bool _loadFailed;

    public static InteractOutlineSettings Instance
    {
        get
        {
            if (_instance == null && !_loadFailed)
            {
                _instance = Resources.Load<InteractOutlineSettings>(ResourcePath);
                if (_instance == null)
                {
                    _loadFailed = true;
                    Debug.LogWarning($"[InteractOutline] Resources/{ResourcePath} 에 설정이 없습니다. 테두리 없이 진행합니다.");
                }
            }
            return _instance;
        }
    }
}
