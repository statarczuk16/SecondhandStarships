using UnityEngine;

public class Component_ShipPartSlot : MonoBehaviour, IInteractable, IHighlightable
{
    [SerializeField] private GameObject defaultShipPartPrefab;
    private bool installed = false;
    private GameObject ghostRoot;
    private GameObject currentGhost;

    public Transform InteractionPoint => transform;

    public bool CanInteract(Controller_Equipment controller)
    {
        return installed == false;
    }

    public void OnHoverEnter(Controller_Equipment controller)
    {
        if (CanInteract(controller))
        {
            SetHighlight(InteractionHighlightState.VALID);
        }
    }

    public void OnHoverExit()
    {
        SetHighlight(InteractionHighlightState.NONE);
    }

    public void OnInteract(Controller_Equipment controller)
    {
        if (CanInteract(controller))
        {
            ClearGhost();

            GameObject spawnedPart = Instantiate(defaultShipPartPrefab, this.transform.parent, false);
            spawnedPart.transform.localPosition = Vector3.zero;
            installed = true;
        }
    }

    public void SetHighlight(InteractionHighlightState state)
    {
        if (state == InteractionHighlightState.VALID)
        {
            if (currentGhost == null)
            {
                currentGhost = GhostPreviewFactory.CreateGhost(defaultShipPartPrefab, this.transform.parent, Color.green);
            }
        }
        else
        {
            ClearGhost();
        }
    }

    private void ClearGhost()
    {
        if (currentGhost != null)
        {
            GhostPreviewFactory.Destroy(currentGhost);
            currentGhost = null;
        }
    }
}