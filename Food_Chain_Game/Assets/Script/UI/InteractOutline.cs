using UnityEngine;

// 상호작용 대상의 빛나는 테두리. 스프라이트를 사방으로 조금씩 밀어 뒤에 깔아서 어떤 모양의 그림이든 윤곽을 따라간다.
// 본인 화면에서만 쓰고, 처음 표시할 때 필요한 오브젝트에 붙어 복사본을 만든다
public class InteractOutline : MonoBehaviour
{
    private static readonly Vector2[] Directions =
    {
        new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
        new Vector2(0.7071f, 0.7071f), new Vector2(-0.7071f, 0.7071f), new Vector2(0.7071f, -0.7071f), new Vector2(-0.7071f, -0.7071f)
    };

    private static readonly int SourceOffsetId = Shader.PropertyToID("_SourceOffset");
    private static readonly int SourceRectId = Shader.PropertyToID("_SourceRect");

    private SpriteRenderer _source;
    private InteractOutlineSettings _settings;
    private GameObject _root;
    private SpriteRenderer[] _copies;
    private float[] _alphas;
    private MaterialPropertyBlock _block;
    // 원본 자리를 뚫는 값을 마지막으로 계산한 그림과 뒤집힘
    private Sprite _cutoutSprite;
    private bool _cutoutFlipX;
    private bool _cutoutFlipY;

    public static InteractOutline Of(Component target)
    {
        if (!target.TryGetComponent(out InteractOutline outline))
            outline = target.gameObject.AddComponent<InteractOutline>();
        return outline;
    }

    public void SetVisible(bool visible)
    {
        if (visible && _root == null) Build();
        if (_root != null) _root.SetActive(visible);
    }

    private void Build()
    {
        _source = GetComponent<SpriteRenderer>();
        _settings = InteractOutlineSettings.Instance;
        if (_source == null || _settings == null || _settings.material == null) return;

        _root = new GameObject("Interact Outline");
        _root.layer = gameObject.layer;
        _root.transform.SetParent(transform, false);

        _copies = new SpriteRenderer[Directions.Length * 2];
        _alphas = new float[_copies.Length];
        _block = new MaterialPropertyBlock();

        for (int i = 0; i < Directions.Length; i++)
        {
            AddCopy(i, Directions[i] * _settings.innerThickness, _settings.innerAlpha);
            AddCopy(Directions.Length + i, Directions[i] * _settings.outerThickness, _settings.outerAlpha);
        }
    }

    private void AddCopy(int index, Vector2 worldOffset, float alpha)
    {
        var go = new GameObject("Copy");
        go.layer = gameObject.layer;
        go.transform.SetParent(_root.transform, false);

        // 오브젝트 크기와 상관없이 두께가 같도록 부모 배율을 나눈다
        Vector3 scale = transform.lossyScale;
        go.transform.localPosition = new Vector3(worldOffset.x / NonZero(scale.x), worldOffset.y / NonZero(scale.y), 0f);

        var copy = go.AddComponent<SpriteRenderer>();
        copy.sharedMaterial = _settings.material;
        // 고정된 오브젝트의 테두리는 시야와 상관없이 전부 보인다. 플레이어와 시체는 벽 너머 위치가 드러나지 않게 시야를 따른다
        bool followsVision = TryGetComponent(out GamePlayer _) || TryGetComponent(out Corpse _);
        copy.maskInteraction = followsVision ? _source.maskInteraction : SpriteMaskInteraction.None;

        _copies[index] = copy;
        _alphas[index] = alpha;
    }

    private static float NonZero(float value)
    {
        return Mathf.Approximately(value, 0f) ? 1f : value;
    }

    // 그림·정렬 순서는 바뀔 수 있어(의상, 위치에 따른 정렬) 보이는 동안 원본을 따라간다
    private void LateUpdate()
    {
        if (_root == null || !_root.activeSelf) return;

        float wave = (Mathf.Sin(Time.time * _settings.pulseSpeed) + 1f) * 0.5f;
        float brightness = Mathf.Lerp(_settings.pulseMinBrightness, 1f, wave);

        UpdateCutout();

        for (int i = 0; i < _copies.Length; i++)
        {
            SpriteRenderer copy = _copies[i];
            copy.sprite = _source.sprite;
            copy.flipX = _source.flipX;
            copy.flipY = _source.flipY;
            copy.enabled = _source.enabled;
            copy.sortingLayerID = _source.sortingLayerID;
            copy.sortingOrder = _source.sortingOrder - 1;

            Color color = _settings.color;
            color.a *= _alphas[i] * brightness;
            copy.color = color;
        }
    }

    // 복사본마다 자기 픽셀이 원본 그림의 어느 UV에 해당하는지 셰이더에 알려 준다. 그림이나 뒤집힘이 바뀔 때만 계산한다
    private void UpdateCutout()
    {
        Sprite sprite = _source.sprite;
        if (sprite == null) return;
        if (sprite == _cutoutSprite && _source.flipX == _cutoutFlipX && _source.flipY == _cutoutFlipY) return;

        _cutoutSprite = sprite;
        _cutoutFlipX = _source.flipX;
        _cutoutFlipY = _source.flipY;

        Vector2[] uv = sprite.uv;
        Vector2 min = uv[0];
        Vector2 max = uv[0];
        for (int i = 1; i < uv.Length; i++)
        {
            min = Vector2.Min(min, uv[i]);
            max = Vector2.Max(max, uv[i]);
        }

        Vector3 size = sprite.bounds.size;
        Vector2 uvPerUnit = new Vector2((max.x - min.x) / NonZero(size.x), (max.y - min.y) / NonZero(size.y));
        if (_cutoutFlipX) uvPerUnit.x = -uvPerUnit.x;
        if (_cutoutFlipY) uvPerUnit.y = -uvPerUnit.y;

        for (int i = 0; i < _copies.Length; i++)
        {
            Vector3 offset = _copies[i].transform.localPosition;

            _copies[i].GetPropertyBlock(_block);
            _block.SetVector(SourceOffsetId, new Vector4(offset.x * uvPerUnit.x, offset.y * uvPerUnit.y, 0f, 0f));
            _block.SetVector(SourceRectId, new Vector4(min.x, min.y, max.x, max.y));
            _copies[i].SetPropertyBlock(_block);
        }
    }
}
