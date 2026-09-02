using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Component_OnOffButton : MonoBehaviour, IHighlightable, IInteractable
{

    // Serialize a MonoBehaviour so it appears in the Inspector
    [SerializeField, Required] private MonoBehaviour m_controlledObjectSource;
    [SerializeField, Required] private HighlightableRenderer m_highlight_renderer;
    [SerializeField, Required] private List<DOTweenAnimation> m_animations;
    
    // The actual interface property used by your logic
    private IToggleable m_controlled_object;
    private bool toggled = false;

    private void Awake()
    {
        m_controlled_object = m_controlledObjectSource.GetComponent<IToggleable>();

        if (m_controlled_object == null)
        {
            Debug.LogError($"Assigned object {m_controlledObjectSource.name} does not have a component implementing IToggleable!", this);
        }
    }
    public bool CanInteract(Controller_Equipment controller)
    {
        return m_controlled_object.CanToggle(out string reason);
    }

    public void OnHoverEnter(Controller_Equipment controller)
    {
        SetHighlight(CanInteract(controller) ? InteractionHighlightState.VALID : InteractionHighlightState.INVALID);
    }

    public void OnHoverExit(Controller_Equipment controller)
    {
        SetHighlight(InteractionHighlightState.NONE);
    }

    public void OnInteract(Controller_Equipment controller)
    {
        if (m_controlled_object == null)
        {
            return;
        }
        if (m_controlled_object.CanToggle(out string reason))
        {
            
            foreach (var animation in m_animations)
            {
                if (toggled)
                {
                    animation.DOPlayBackwards();
                }
                else
                {
                    animation.DOPlayForward();
                }

                toggled = !toggled;
            }
            AudioEvents.Fire(SoundID.Toggle, this.transform.position);
            m_controlled_object.ToggleWantsToBeOn();
        }
    }

    public void OnHoverUpdate(Controller_Equipment equipmentController, RaycastHit hitInfo)
    {
        return;
    }

    public string GetInteractionLabel(Controller_Equipment controller)
    {
         bool can_turn_on = m_controlled_object.OnRequirementsMet(out string reason);
         if (can_turn_on)
         {
             if (this.m_controlled_object.IsOn())
             {
                 return $"//TURN OFF {this.m_controlledObjectSource.name}";
             }
             else
             {
                 return $"//TURN ON {this.m_controlledObjectSource.name}";
             }
             
         }
         else
         {
             return $"// {this.m_controlledObjectSource.name} CANNOT_TOGGLE -- {reason}";
         }
         
    }

    public Transform InteractionPoint => this.transform;
    
    public void SetHighlight(InteractionHighlightState state, Controller_Equipment controller = null)
    {
        if (m_highlight_renderer)
        {
            m_highlight_renderer.SetHighlight(state);
        }
        else
        {
            MeshRenderer graphics = this.GetComponent<MeshRenderer>();
            if (state == InteractionHighlightState.VALID || state == InteractionHighlightState.INVALID)
            {
                graphics.enabled = true;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                graphics.GetPropertyBlock(block);

                Color current = block.GetColor("_BaseColor");
                Color tint = state == InteractionHighlightState.VALID ? Color.green : Color.red;
                tint.a = 0.5f;

                block.SetColor("_BaseColor", tint);
                graphics.SetPropertyBlock(block);
            }
            else
            {
                graphics.enabled = false;
            }  
        }
        
    }
}
