using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;


public class Component_Inventory : MonoBehaviour, IHighlightable, IInventoryOwner, IInteractable
{
    [SerializeField] private Data_Inventory m_data_inventory;
    [SerializeField] private IInventoryOwner m_owner;
    [SerializeField] private HighlightableRenderer m_highlightable;
    [SerializeField] private InventoryStartupBehavior startup_behavior = InventoryStartupBehavior.UseInspector;

    private void Awake()
    {
        switch(startup_behavior)
        {
            case InventoryStartupBehavior.UseInspector:
            {
                break;
            }
            case InventoryStartupBehavior.Empty:
            {
                this.m_data_inventory.ClearAll();
                break;
            }
            case InventoryStartupBehavior.AllPartsInstalled:
            {
                string err;
                bool success = this.m_data_inventory.TryFillWithRecipeItems(out err);
                if (!success)
                {
                    TopicLogger.Log(LogTopic.Inventory, LogLevel.ERROR, $"Couldnt fill inventory with recipe {err}");
                }
                break;
            }
            case InventoryStartupBehavior.SomePartsMissing:
            {
                var recipeList = this.m_data_inventory.Recipe;
                if (recipeList == null || recipeList.Count <= 1)
                {
                    string err;
                    this.m_data_inventory.TryFillWithRecipeItems(out err);
                    break;
                }

                int num_parts = recipeList.Count;
                // Random.Range max is exclusive for integers, picking between 1 and num_parts - 1
                int partsToKeep = Random.Range(1, num_parts);

                // Create a randomized subset of the recipe ingredients using a Fisher-Yates shuffle
                var subset = new List<RecipeIngredient>(recipeList);
                for (int i = 0; i < subset.Count; i++)
                {
                    int rnd = Random.Range(i, subset.Count);
                    RecipeIngredient temp = subset[i];
                    subset[i] = subset[rnd];
                    subset[rnd] = temp;
                }
                subset.RemoveRange(partsToKeep, subset.Count - partsToKeep);

                this.m_data_inventory.ClearAll();

                string error;
                if (!this.m_data_inventory.TrySetMaxSlots(recipeList.Count, out error))
                {
                    TopicLogger.Log(LogTopic.Inventory, LogLevel.ERROR, $"Couldnt set max slots for missing parts: {error}");
                    break;
                }

                foreach (var ingredient in subset)
                {
                    int leftover;
                    bool success = this.m_data_inventory.TryAddItem(ingredient.item_type, ingredient.tier, 1, out leftover, out error);
                    if (!success || leftover > 0)
                    {
                        TopicLogger.Log(LogTopic.Inventory, LogLevel.ERROR, $"Couldnt add recipe item during partial fill: {error}");
                    }
                }
                break;
            }
        }
    }

    public void ClaimInventory(IInventoryOwner new_owner)
    {
        this.m_owner = new_owner;
    }

    public Data_Inventory GetInventory()
    {
        return this.m_data_inventory;
    }
    public bool IsInstallTarget()
    {
        return m_owner?.IsInstallTarget() ?? false;
    }
    

    public bool CanInteract(Controller_Equipment controller)
    {
        return true;
    }

    public void OnHoverEnter(Controller_Equipment controller)
    {
        SetHighlight(CanInteract(controller) ? InteractionHighlightState.VALID : InteractionHighlightState.NONE);
    }

    public void OnHoverExit(Controller_Equipment controller)
    {
        SetHighlight(InteractionHighlightState.NONE);
    }

    public void OnInteract(Controller_Equipment controller)
    {
        controller.DisplayInventory(controller, this);
    }

    public void OnHoverUpdate(Controller_Equipment equipmentController, RaycastHit hitInfo)
    {
        return;
    }

    public string GetInteractionLabel(Controller_Equipment controller)
    {
        return $"//SERVICE HATCH -> OPEN TO INSPECT PARTS";
    }

    public Transform InteractionPoint => this.transform;
    
    public void SetHighlight(InteractionHighlightState state, Controller_Equipment controller = null)
    {
        bool visible = state == InteractionHighlightState.VALID;
        if (m_highlightable)
        {
            m_highlightable.SetHighlight(state);
        }
        else
        {
            MeshRenderer[] graphics = GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].enabled = visible;
            }
        }
        
    }

    public void ClearAll()
    {
        this.m_data_inventory.ClearAll();
    }
}
