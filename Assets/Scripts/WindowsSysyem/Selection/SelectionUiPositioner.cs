using UnityEngine;

[DisallowMultipleComponent]
public class SelectionUiPositioner : MonoBehaviour
{
    [SerializeField] Camera _worldCamera;

    ActorInfoWindow _actorWindow;
    RoomSelectionWindow _roomWindow;
    RoomObjectSelectionWindow _roomObjectWindow;
    RectTransform _rectTransform;

    void Awake()
    {
        _actorWindow = GetComponent<ActorInfoWindow>();
        _roomWindow = GetComponent<RoomSelectionWindow>();
        _roomObjectWindow = GetComponent<RoomObjectSelectionWindow>();
        _rectTransform = transform as RectTransform;
    }

    void OnEnable()
    {
        UpdatePosition();
    }

    void LateUpdate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        UpdatePosition();
    }

    void UpdatePosition()
    {
        Transform target = ResolveTargetTransform();
        if (target == null)
        {
            return;
        }

        if (_rectTransform == null)
        {
            _rectTransform = transform as RectTransform;
        }

        if (_rectTransform == null)
        {
            return;
        }

        if (!TryGetTargetScreenPoint(target, ResolveWorldCamera(), out Vector2 screenPoint))
        {
            return;
        }

        ApplyScreenPosition(screenPoint);
    }

    Camera ResolveWorldCamera()
    {
        if (_worldCamera == null)
        {
            _worldCamera = Camera.main;
        }

        return _worldCamera;
    }

    Transform ResolveTargetTransform()
    {
        if (_actorWindow != null && _actorWindow.CurrentSelectionTransform != null)
        {
            return _actorWindow.CurrentSelectionTransform;
        }

        if (_roomWindow != null && _roomWindow.CurrentRoom != null)
        {
            return _roomWindow.CurrentRoom.transform;
        }

        if (_roomObjectWindow != null && _roomObjectWindow.CurrentObject != null)
        {
            return _roomObjectWindow.CurrentObject.transform;
        }

        return null;
    }

    void ApplyScreenPosition(Vector2 screenPoint)
    {
        if (_rectTransform == null)
        {
            return;
        }

        _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        if (TryScreenPointToAnchoredPosition(_rectTransform, screenPoint, out Vector2 anchoredPosition))
        {
            _rectTransform.anchoredPosition = anchoredPosition;
        }
    }

    static bool TryGetTargetScreenPoint(Transform target, Camera worldCamera, out Vector2 screenPoint)
    {
        screenPoint = default;
        if (target == null)
        {
            return false;
        }

        Vector3 worldPoint = TryGetWorldBoundsCenter(target, out Vector3 boundsCenter)
            ? boundsCenter
            : target.position;

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        Vector3 projected = worldCamera != null
            ? worldCamera.WorldToScreenPoint(worldPoint)
            : RectTransformUtility.WorldToScreenPoint(null, worldPoint);
        if (worldCamera != null && projected.z < 0f)
        {
            return false;
        }

        screenPoint = new Vector2(projected.x, projected.y);
        return true;
    }

    static bool TryScreenPointToAnchoredPosition(RectTransform rectTransform, Vector2 screenPoint, out Vector2 anchoredPosition)
    {
        anchoredPosition = default;
        RectTransform parent = rectTransform != null ? rectTransform.parent as RectTransform : null;
        if (parent == null)
        {
            return false;
        }

        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        Camera uiCamera = ResolveUiCamera(canvas);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, uiCamera, out Vector2 localPoint))
        {
            return false;
        }

        Vector2 anchor = new Vector2(
            Mathf.Lerp(parent.rect.xMin, parent.rect.xMax, rectTransform.anchorMin.x),
            Mathf.Lerp(parent.rect.yMin, parent.rect.yMax, rectTransform.anchorMin.y));
        anchoredPosition = localPoint - anchor;
        return true;
    }

    static Camera ResolveUiCamera(Canvas canvas)
    {
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
    }

    static bool TryGetWorldBoundsCenter(Transform root, out Vector3 center)
    {
        center = default;
        if (root == null)
        {
            return false;
        }

        bool hasBounds = false;
        Bounds bounds = default;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.bounds.size.sqrMagnitude <= 0.000001f)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.bounds.size.sqrMagnitude <= 0.000001f)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        if (!hasBounds)
        {
            return false;
        }

        center = bounds.center;
        return true;
    }
}

