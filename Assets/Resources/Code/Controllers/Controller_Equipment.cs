using System;
using System.Collections.Generic;
using UnityEngine;

public enum EquipmentType
{
    None,
    Wrench,
    PlasmaCutter,
    PryBar,
}

[RequireComponent(typeof(Mediator_PlayerMiniGames))]

public class Controller_Equipment : MonoBehaviour
{
    [SerializeField] List<EquipmentType> availableTools; // assign in inspector, or populate at runtime from inventory
    [SerializeField] private EquipmentType m_equipped_tool;
    [SerializeField] private Mediator_PlayerMiniGames miniGameMediator;

    public void EquipTool(EquipmentType type)
    {
        if(availableTools.Contains(type))
        {
            m_equipped_tool = type;
        }
        else
        {
            //we dont have this tool 
        }
    }

    public EquipmentType GetEquippedTool()
    {
        return m_equipped_tool;
    }

    public void Unequip()
    {
        m_equipped_tool = EquipmentType.None;
    }

    public void startMiniGame(IToolMinigame game)
    {
        miniGameMediator.StartMiniGame(game);
    }

    public Controller_PlayerInput GetUIHub()
    {
        return miniGameMediator.GetUIHub();
    }
}