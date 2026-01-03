using UnityEngine;

public class Dragon : ARInteractableObject
{
    private Animator _animator;

    [SerializeField] private float rotationSpeed = 5f;

    private static readonly int IsInteractingHash = Animator.StringToHash("IsInteracting");
    private float _targetLocalYRotation = 0f;

    private void OnEnable()
    {
        _animator = GetComponent<Animator>();
        _targetLocalYRotation = 0f;

        if (_animator != null)
            _animator.SetBool(IsInteractingHash, false);
    }

    private void Update()
    {
        if (ARObjectState == State.Active)
        {
            ARInteractableObject closestDragon = GetClosestInteractable();
            if (closestDragon != null)
            {
                CalculateTargetYRotation(closestDragon.transform);
            }
        }
        else if (ARObjectState == State.Idle)
        {
            _targetLocalYRotation = 0f;
        }

        ApplySmoothYRotation();
    }

    protected override void SetState(State state)
    {
        base.SetState(state);

        if (_animator != null)
        {
            bool isInteracting = (state == State.Active);
            _animator.SetBool(IsInteractingHash, isInteracting);
            Debug.Log($"{gameObject.name} - State changed to: {state}, IsInteracting: {isInteracting}");
        }
    }

    private ARInteractableObject GetClosestInteractable()
    {
        ARInteractableObject closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (var interactable in GetInteractables())
        {
            float distance = Vector3.Distance(transform.position, interactable.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = interactable;
            }
        }

        return closest;
    }

    private void CalculateTargetYRotation(Transform target)
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion worldLookRotation = Quaternion.LookRotation(direction);

            Quaternion parentRotation = transform.parent != null
                ? transform.parent.rotation
                : Quaternion.identity;

            Quaternion localLookRotation = Quaternion.Inverse(parentRotation) * worldLookRotation;

            _targetLocalYRotation = localLookRotation.eulerAngles.y;
        }
    }

    private void ApplySmoothYRotation()
    {
        Vector3 currentLocalEuler = transform.localEulerAngles;
        
        float smoothY = Mathf.LerpAngle(
            currentLocalEuler.y,
            _targetLocalYRotation,
            Time.deltaTime * rotationSpeed
        );

        transform.localRotation = Quaternion.Euler(
            currentLocalEuler.x,
            smoothY,
            currentLocalEuler.z
        );
    }
}