using System.Collections.Generic;
using UnityEngine;

public abstract class ARInteractableObject : MonoBehaviour
{
    //List of interactables I am interacting with
    private List<ARInteractableObject> _interactables = new List<ARInteractableObject>();

    //State of this ARObject
    protected enum State
    {
        Idle,
        Active
    }
    protected State ARObjectState = State.Idle;

    //Get ARInteractableObject when trigger enters and exits
    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<ARInteractableObject>(out var interactable))
        {
            AddInteractable(interactable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.TryGetComponent<ARInteractableObject>(out var interactable))
        {
            RemoveInteractable(interactable);
        }
    }

    protected void AddInteractable(ARInteractableObject interactable)
    {
        _interactables.Add(interactable);
        SetState(State.Active);
    }

    protected void RemoveInteractable(ARInteractableObject interactable)
    {
        _interactables.Remove(interactable);
        if(_interactables.Count == 0)
        {
            SetState(State.Idle);
        }
    }

    protected List<ARInteractableObject> GetInteractables()
    {
        return _interactables;
    }

    //What happens when this object gets disable?
    private void OnDisable()
    {
        foreach(var interactable in _interactables)
        {
            interactable.RemoveInteractable(this);
        }
        _interactables.Clear();
        SetState(State.Idle);
    }

    //Update the State of this AR Object
    protected virtual void SetState(State state)
    {
        ARObjectState = state;
    }

}
