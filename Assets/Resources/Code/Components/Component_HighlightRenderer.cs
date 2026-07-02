using TMPro;
using UnityEngine;

public class HighlightableRenderer : MonoBehaviour
{
    // Keeping this serialized in case you want to manually assign a specific root, 
    // but the script will automatically fallback to the current GameObject if left empty.
    [SerializeField] GameObject targetObject;

    [SerializeField] Color validColor = Color.green;
    [SerializeField] Color invalidColor = Color.red;
    [SerializeField] Color neutralColor = Color.black;
    [SerializeField] Color currentColor = Color.black;

    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    MaterialPropertyBlock propBlock;

    // Array to store the root renderer and all child renderers
    private Renderer[] targetRenderers;

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();

        // If no target object was assigned in the inspector, use this object
        if (targetObject == null)
        {
            targetObject = this.gameObject;
        }

        // Find all Renderers on the target object and all of its children
        targetRenderers = targetObject.GetComponentsInChildren<Renderer>();
    }

    public void SetHighlight(InteractionHighlightState state)
    {
        // 1. Determine the color using a cleaner modern switch expression
        Color c = state switch
        {
            InteractionHighlightState.VALID => validColor,
            InteractionHighlightState.INVALID => invalidColor,
            _ => neutralColor // Handles both NONE and default safety cases
        };

        // 2. Set the color inside the property block
        propBlock.SetColor(EmissionColor, c);
        currentColor = c;

        // 3. Loop through every renderer we found and apply the property block
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null) // Safety check
            {
                targetRenderers[i].SetPropertyBlock(propBlock);
            }
        }
    }
}