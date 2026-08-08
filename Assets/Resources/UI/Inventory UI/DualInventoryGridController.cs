using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages a Subnautica-style dual inventory interface.
/// Handles drag-and-drop between Player and Container inventories.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class DualInventoryController : MonoBehaviour
{
    private InventoryGridController m_playerGrid;
    private InventoryGridController m_containerGrid;

    private VisualElement m_playerWrap;
    private VisualElement m_containerWrap;
    private VisualElement m_spacer;

    // State for cross-inventory dragging
    private InventoryGridController m_heldGridSource = null;
    private int m_heldSlotIndex = -1;
    private Controller_Equipment m_equipment_controller;

    private void Awake()
    {
        m_playerGrid = new InventoryGridController("PlayerGrid");
        m_containerGrid = new InventoryGridController("ContainerGrid");

        // Listen for left clicks (pick up / place)
        m_playerGrid.OnSlotClicked += (index) => HandleSlotClicked(m_playerGrid, index);
        m_containerGrid.OnSlotClicked += (index) => HandleSlotClicked(m_containerGrid, index);

        // Listen for right clicks (quick transfer 1)
        m_playerGrid.OnSlotRightClicked += (index) => HandleSlotRightClicked(m_playerGrid, index);
        m_containerGrid.OnSlotRightClicked += (index) => HandleSlotRightClicked(m_containerGrid, index);
    }

    private void OnEnable()
    {
        var panelRenderer = GetComponent<PanelRenderer>();
        if (panelRenderer != null)
        {
            panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }
    }

    private void OnDisable()
    {
        var panelRenderer = GetComponent<PanelRenderer>();
        if (panelRenderer != null)
        {
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }
    }

    private void OnUIReload(PanelRenderer pr, VisualElement root)
    {
        m_playerWrap = root.Q<VisualElement>("InventoryGridWrapPlayer");
        m_containerWrap = root.Q<VisualElement>("InventoryGridWrapContainer");
        m_spacer = root.Q<VisualElement>("InventoryGridWrap"); // The spacer element

        m_playerGrid.Initialize(m_playerWrap);
        m_containerGrid.Initialize(m_containerWrap);

        RefreshLayoutVisibility();
    }

    private void Update()
    {
        m_playerGrid.Tick();
        m_containerGrid.Tick();
    }

    public void OpenUI(Controller_Equipment equipment_controller, IInventoryOwner playerOwner, IInventoryOwner containerOwner = null)
    {
        gameObject.SetActive(true);
        ClearHeldState();
        m_equipment_controller = equipment_controller;
        m_playerGrid.SetInventoryOwner(playerOwner);
        m_containerGrid.SetInventoryOwner(containerOwner);

        RefreshLayoutVisibility();
    }

    public void CloseUI()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
            ClearHeldState();
            m_playerGrid.SetInventoryOwner(null);
            m_containerGrid.SetInventoryOwner(null);
        }
        
    }

    private void RefreshLayoutVisibility()
    {
        if (m_playerWrap == null || m_containerWrap == null) return;

        bool hasPlayer = m_playerGrid.HasOwner;
        bool hasContainer = m_containerGrid.HasOwner;

        m_playerWrap.style.display = hasPlayer ? DisplayStyle.Flex : DisplayStyle.None;
        m_containerWrap.style.display = hasContainer ? DisplayStyle.Flex : DisplayStyle.None;
        m_spacer.style.display = (hasPlayer && hasContainer) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void HandleSlotClicked(InventoryGridController clickedGrid, int clickedIndex)
    {
        var clickedDataInv = clickedGrid.InventoryOwner.GetInventory();

        // SCENARIO 1: We aren't holding anything yet. Pick up if not empty.
        if (m_heldGridSource == null)
        {
            var slot = clickedDataInv.GetSlot(clickedIndex);
            if (slot != null && !slot.IsEmpty)
            {
                m_heldGridSource = clickedGrid;
                m_heldSlotIndex = clickedIndex;
                clickedGrid.SetHeldIndex(clickedIndex);
            }
            return;
        }

        // SCENARIO 2: We clicked the exact same slot we are holding. Cancel drop.
        if (m_heldGridSource == clickedGrid && m_heldSlotIndex == clickedIndex)
        {
            ClearHeldState();
            return;
        }

        // SCENARIO 3: Moving within the SAME inventory
        if (m_heldGridSource == clickedGrid)
        {
            if (!clickedDataInv.TryMoveSlot(m_heldSlotIndex, clickedIndex, out string error))
            {
                TopicLogger.Log(LogTopic.Inventory, LogLevel.WARN, $"Same-inventory move failed: {error}");
            }
            ClearHeldState();
            return;
        }

        // SCENARIO 4: Moving across DIFFERENT inventories (Player <-> Container)
        var sourceDataInv = m_heldGridSource.InventoryOwner.GetInventory();
        var sourceSlot = sourceDataInv.GetSlot(m_heldSlotIndex);

        if (sourceSlot.slot_type == InventorySlotType.Item)
        {
            clickedDataInv.TryAddItem(
                sourceSlot.item_type, 
                sourceSlot.tier, 
                sourceSlot.count, 
                clickedIndex, 
                out int leftover, 
                out string error
            );

            if (leftover <= 0)
            {
                sourceSlot.Clear();
            }
            else
            {
                sourceSlot.count = leftover;
            }
        }
        else if (sourceSlot.slot_type == InventorySlotType.Module)
        {
            if (clickedDataInv.TryAddModule(sourceSlot.module, clickedIndex, out string error))
            {
                sourceSlot.Clear();
            }
            else
            {
                TopicLogger.Log(LogTopic.Inventory, LogLevel.WARN, $"Cross-inventory module move failed: {error}");
            }
        }

        ClearHeldState();
    }
    
    // NEW: Handle right clicks for quick transferring exactly 1 item
    private void HandleSlotRightClicked(InventoryGridController clickedGrid, int clickedIndex)
    {
        // Don't allow quick-transfer if the user is currently holding an item on the cursor
        if (m_heldGridSource != null) return;

        // Identify the opposite grid
        InventoryGridController targetGrid = (clickedGrid == m_playerGrid) ? m_containerGrid : m_playerGrid;
        
        // If the other inventory isn't open, we can't transfer anything
        if (!targetGrid.HasOwner) return;

        if (targetGrid.InventoryOwner.IsInstallTarget() || clickedGrid.InventoryOwner.IsInstallTarget())
        {
            //open install mini game
            var connector = new Connector_InventoryItemRemoval(
                InstallationState.UNINSTALLED,
                EquipmentType.SCREW_DRIVER /* or whatever tool pulls parts */
            );

            // Define the win action using a lambda expression
            Action win_action = () => 
            {
                QuickMove(clickedGrid, clickedIndex);
            };
            Action always_action = () => 
            {
                this.gameObject.SetActive(true);
            };
            var minigame_goal = new HashSet<InstallationState>
            {
                InstallationState.INSTALLED
            };
            IToolMinigame minigame = new MiniGame_Wrench(connector, connector.RequiredTool(), minigame_goal);
            minigame.SetOutcomes(InputMode.MenuMode, always_action, win_action, null);
            this.gameObject.SetActive(false);
            m_equipment_controller.startMiniGame(minigame);
        }
        else
        {
            QuickMove(clickedGrid, clickedIndex);
        }
    }

    private void QuickMove(InventoryGridController clickedGrid, int clickedIndex)
    {
        // Don't allow quick-transfer if the user is currently holding an item on the cursor
        if (m_heldGridSource != null) return;

        // Identify the opposite grid
        InventoryGridController targetGrid = (clickedGrid == m_playerGrid) ? m_containerGrid : m_playerGrid;
        
        // If the other inventory isn't open, we can't transfer anything
        if (!targetGrid.HasOwner) return;

        var sourceDataInv = clickedGrid.InventoryOwner.GetInventory();
        var targetDataInv = targetGrid.InventoryOwner.GetInventory();

        var sourceSlot = sourceDataInv.GetSlot(clickedIndex);
        if (sourceSlot == null || sourceSlot.IsEmpty) return;

        if (sourceSlot.slot_type == InventorySlotType.Item)
        {
            // Attempt to place exactly 1 count into the target inventory with no preferred index (-1)
            targetDataInv.TryAddItem(
                sourceSlot.item_type,
                sourceSlot.tier,
                1,
                -1,
                out int leftover,
                out string error
            );

            // If successfully transferred (no leftover)
            if (leftover <= 0)
            {
                sourceSlot.count -= 1;
                
                // Clean up the slot if it's now empty
                if (sourceSlot.count <= 0)
                {
                    sourceSlot.Clear();
                }
            }
        }
        else if (sourceSlot.slot_type == InventorySlotType.Module)
        {
            // Modules don't have counts, so right-clicking just transfers the whole module
            if (targetDataInv.TryAddModule(sourceSlot.module, -1, out string error))
            {
                sourceSlot.Clear();
            }
        }
    }

    private void ClearHeldState()
    {
        m_heldGridSource = null;
        m_heldSlotIndex = -1;
        m_playerGrid.SetHeldIndex(-1);
        m_containerGrid.SetHeldIndex(-1);
    }
}