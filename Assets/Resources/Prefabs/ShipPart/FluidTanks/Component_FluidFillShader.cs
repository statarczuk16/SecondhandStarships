using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class Component_FluidFillShader : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float _fillAmount = 0.5f;

    private static readonly int FillWorldYID = Shader.PropertyToID("_FillWorldY");
    private static readonly int DarkTintColorID = Shader.PropertyToID("_DarkTint");

    private Renderer _renderer;
    private MeshFilter _meshFilter;
    private MaterialPropertyBlock _mpb;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _meshFilter = GetComponent<MeshFilter>();
        _mpb = new MaterialPropertyBlock();
    }

    private void OnEnable() => ApplyFill();

    

    // Call from Component_FluidSender whenever tank volume changes
    public void SetFillPercent(float normalizedFill)
    {
        _fillAmount = Mathf.Clamp01(normalizedFill);
        ApplyFill();
    }

    private void ApplyFill()
    {
        if (_renderer == null) return;

        // Renderer.bounds is already axis-aligned in WORLD space,
        // and automatically accounts for the object's current rotation/scale
        Bounds worldBounds = _renderer.bounds;

        float worldFillY = Mathf.Lerp(worldBounds.min.y, worldBounds.max.y, _fillAmount);

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(FillWorldYID, worldFillY);
        _mpb.SetColor(DarkTintColorID, Color.blue);
        _renderer.SetPropertyBlock(_mpb);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_renderer == null) _renderer = GetComponent<Renderer>();
        if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        if (!Application.isPlaying) ApplyFill();
    }
#endif
}