using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;


[ExecuteAlways]
[AddComponentMenu("Scripts/MRTK/SDK/ScrollingObjectCollection2")]
public class ScrollingObjectCollection2 : MonoBehaviour, IMixedRealityPointerHandler, IEventSystemHandler, IMixedRealitySourceStateHandler, IMixedRealityTouchHandler
{
    public enum VelocityType
    {
        FalloffPerFrame,
        FalloffPerItem,
        NoVelocitySnapToItem,
        None
    }

    public enum ScrollDirectionType
    {
        UpAndDown,
        LeftAndRight
    }

    public enum EditMode
    {
        Auto,
        Manual
    }

    [Serializable]
    public class ScrollEvent : UnityEvent<GameObject>
    {
    }

    private enum VelocityState
    {
        None,
        Resolving,
        Calculating,
        Bouncing,
        Dragging,
        Animating
    }

    public enum PaginationMode
    {
        ByTier,
        ByPage,
        ToCellIndex
    }

    [SerializeField]
    [Tooltip("Enables/disables scrolling with near/far interaction.")]
    private bool canScroll = true;

    [SerializeField]
    [Tooltip("Edit modes for defining the clipping box masking boundaries. Choose 'Auto' to automatically use pagination values. Choose 'Manual' for enabling direct manipulation of the clipping box object.")]
    private EditMode maskEditMode;

    [SerializeField]
    [Tooltip("Edit modes for defining the scroll interaction collider boundaries. Choose 'Auto' to automatically use pagination values. Choose 'Manual' for enabling direct manipulation of the collider.")]
    private EditMode colliderEditMode;

    [SerializeField]
    private bool maskEnabled = true;

    private bool wasMaskEnabled = true;

    [SerializeField]
    [Tooltip("The distance, in meters, the current pointer can travel along the scroll direction before triggering a scroll drag.")]
    [Range(0f, 0.2f)]
    private float handDeltaScrollThreshold = 0.02f;

    [SerializeField]
    [Tooltip("Withdraw amount, in meters, from the front of the scroll boundary needed to transition from touch engaged to released.")]
    private float releaseThresholdFront = 0.03f;

    [SerializeField]
    [Tooltip("Withdraw amount, in meters, from the back of the scroll boundary needed to transition from touch engaged to released.")]
    private float releaseThresholdBack = 0.2f;

    [SerializeField]
    [Tooltip("Withdraw amount, in meters, from the right or left of the scroll boundary needed to transition from touch engaged to released.")]
    private float releaseThresholdLeftRight = 0.2f;

    [SerializeField]
    [Tooltip("Withdraw amount, in meters, from the top or bottom of the scroll boundary needed to transition from touch engaged to released.")]
    private float releaseThresholdTopBottom = 0.2f;

    [SerializeField]
    [Tooltip("Distance, in meters, to position a local xy plane used to verify if a touch interaction started in the front of the scroll view.")]
    [Range(0f, 0.05f)]
    private float frontTouchDistance = 0.005f;

    [SerializeField]
    [Tooltip("The direction in which content should scroll.")]
    private ScrollDirectionType scrollDirection;

    [SerializeField]
    [Tooltip("Toggles whether the scrollingObjectCollection will use the Camera OnPreRender event to manage content visibility.")]
    private bool useOnPreRender;

    [SerializeField]
    [Tooltip("Amount of (extra) velocity to be applied to scroller")]
    [Range(0f, 0.02f)]
    private float velocityMultiplier = 0.008f;

    [SerializeField]
    [Tooltip("Amount of falloff applied to velocity")]
    [Range(0.0001f, 0.9999f)]
    private float velocityDampen = 0.9f;

    [SerializeField]
    [Tooltip("The desired type of velocity for the scroller.")]
    private VelocityType typeOfVelocity;

    [SerializeField]
    [Tooltip("Animation curve for pagination.")]
    private AnimationCurve paginationCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    [SerializeField]
    [Tooltip("The amount of time (in seconds) the PaginationCurve will take to evaluate.")]
    private float animationLength = 0.25f;

    [Tooltip("Number of cells in a row on up-down scroll view or number of cells in a column on left-right scroll view.")]
    [SerializeField]
    [FormerlySerializedAs("tiers")]
    [Min(1f)]
    private int cellsPerTier = 1;

    [SerializeField]
    [Tooltip("Number of visible tiers in the scrolling area.")]
    [FormerlySerializedAs("viewableArea")]
    [Min(1f)]
    private int tiersPerPage = 2;

    [Tooltip("Width of the pagination cell.")]
    [SerializeField]
    [Min(0.001f)]
    private float cellWidth = 0.25f;

    [Tooltip("Height of the pagination cell.")]
    [SerializeField]
    [Min(0.001f)]
    private float cellHeight = 0.25f;

    [Tooltip("Depth of cell used for masking out content renderers that are out of bounds.")]
    [SerializeField]
    [Min(0.001f)]
    private float cellDepth = 0.25f;

    [SerializeField]
    [Tooltip("Multiplier to add more bounce to the overscroll of a list when using VelocityType.FalloffPerFrame or VelocityType.FalloffPerItem.")]
    private float bounceMultiplier = 0.1f;

    private const float DragLerpInterval = 0.5f;

    private const float OverDampLerpInterval = 0.9f;

    private const float BounceLerpInterval = 0.2f;

    [Tooltip("Event that is fired on the target object when the ScrollingObjectCollection deems event as a Click.")]
    public ScrollEvent OnClick = new ScrollEvent();

    [Tooltip("Event that is fired on the target object when the ScrollingObjectCollection is touched.")]
    public ScrollEvent OnTouchStarted = new ScrollEvent();

    [Tooltip("Event that is fired on the target object when the ScrollingObjectCollection is no longer touched.")]
    public ScrollEvent OnTouchEnded = new ScrollEvent();

    [Tooltip("Event that is fired on the target object when the ScrollingObjectCollection is no longer in motion from velocity.")]
    public UnityEvent OnMomentumEnded = new UnityEvent();

    [Tooltip("Event that is fired on the target object when the ScrollingObjectCollection is starting motion with velocity.")]
    public UnityEvent OnMomentumStarted = new UnityEvent();

    [SerializeField]
    [HideInInspector]
    private CameraEventRouter cameraMethods;

    private readonly float minY = 0f;

    private readonly float maxX = 0f;

    private Bounds contentBounds;

    private BoxCollider scrollingCollider;

    private const float ScrollingColliderDepth = 0.001f;

    private NearInteractionTouchable scrollingTouchable;

    [SerializeField]
    [HideInInspector]
    private GameObject scrollContainer;

    [SerializeField]
    [HideInInspector]
    private GameObject clippingObject;

    [SerializeField]
    [HideInInspector]
    private ClippingBox clipBox;

    private Collider clippingBoundsCollider;

    private readonly float contentVisibilityThresholdRatio = 1.025f;

    private bool oldIsTargetPositionLockedOnFocusLock;

    private readonly HashSet<Renderer> clippedRenderers = new HashSet<Renderer>();

    private Vector3 initialScrollerPos;

    private Vector3 workingScrollerPos;

    private List<Renderer> renderersToClip = new List<Renderer>();

    private List<Renderer> renderersToUnclip = new List<Renderer>();

    private IMixedRealityPointer currentPointer;

    private GameObject initialFocusedObject;

    private Vector3 initialPointerPos;

    private Vector3 lastPointerPos;

    private float scrollVelocity = 0f;

    private float avgVelocity = 0f;

    private readonly float velocityFilterWeight = 0.97f;

    private VelocityState currentVelocityState;

    private VelocityState previousVelocityState;

    private Vector3 velocityDestinationPos;

    private float velocitySnapshot;

    private IEnumerator animateScroller;

    [SerializeField]
    [Tooltip("Disables Gameobjects with Renderer components which are clipped by the clipping box.")]
    private bool disableClippedGameObjects = true;

    [SerializeField]
    [Tooltip("Disables the Renderer components of Gameobjects which are clipped by the clipping box.")]
    private bool disableClippedRenderers = false;

    public bool CanScroll
    {
        get
        {
            return canScroll;
        }
        set
        {
            canScroll = value;
        }
    }

    public EditMode MaskEditMode
    {
        get
        {
            return maskEditMode;
        }
        set
        {
            maskEditMode = value;
        }
    }

    public EditMode ColliderEditMode
    {
        get
        {
            return colliderEditMode;
        }
        set
        {
            colliderEditMode = value;
        }
    }

    public bool MaskEnabled
    {
        get
        {
            return maskEnabled;
        }
        set
        {
            if (!value && value != wasMaskEnabled)
            {
                RestoreContentVisibility();
            }

            wasMaskEnabled = value;
            maskEnabled = value;
        }
    }

    public float HandDeltaScrollThreshold
    {
        get
        {
            return handDeltaScrollThreshold;
        }
        set
        {
            handDeltaScrollThreshold = value;
        }
    }

    public float ReleaseThresholdFront
    {
        get
        {
            return releaseThresholdFront;
        }
        set
        {
            releaseThresholdFront = value;
        }
    }

    public float ReleaseThresholdBack
    {
        get
        {
            return releaseThresholdBack;
        }
        set
        {
            releaseThresholdBack = value;
        }
    }

    public float ReleaseThresholdLeftRight
    {
        get
        {
            return releaseThresholdLeftRight;
        }
        set
        {
            releaseThresholdLeftRight = value;
        }
    }

    public float ReleaseThresholdTopBottom
    {
        get
        {
            return releaseThresholdTopBottom;
        }
        set
        {
            releaseThresholdTopBottom = value;
        }
    }

    public float FrontTouchDistance
    {
        get
        {
            return frontTouchDistance;
        }
        set
        {
            frontTouchDistance = value;
        }
    }

    public ScrollDirectionType ScrollDirection
    {
        get
        {
            return scrollDirection;
        }
        set
        {
            scrollDirection = value;
        }
    }

    public bool UseOnPreRender
    {
        get
        {
            return useOnPreRender;
        }
        set
        {
            if (useOnPreRender != value)
            {
                if (cameraMethods == null)
                {
                    cameraMethods = CameraCache.Main.gameObject.EnsureComponent<CameraEventRouter>();
                }

                ClipBox.UseOnPreRender = true;
                if (value)
                {
                    cameraMethods.OnCameraPreRender += OnCameraPreRender;
                }
                else
                {
                    cameraMethods.OnCameraPreRender -= OnCameraPreRender;
                }

                useOnPreRender = value;
                ClipBox.UseOnPreRender = useOnPreRender;
            }
        }
    }

    public float VelocityMultiplier
    {
        get
        {
            return velocityMultiplier;
        }
        set
        {
            velocityMultiplier = value;
        }
    }

    public float VelocityDampen
    {
        get
        {
            return velocityDampen;
        }
        set
        {
            velocityDampen = value;
        }
    }

    public VelocityType TypeOfVelocity
    {
        get
        {
            return typeOfVelocity;
        }
        set
        {
            typeOfVelocity = value;
        }
    }

    public AnimationCurve PaginationCurve
    {
        get
        {
            return paginationCurve;
        }
        set
        {
            paginationCurve = value;
        }
    }

    public float AnimationLength
    {
        get
        {
            return (animationLength < 0f) ? 0f : animationLength;
        }
        set
        {
            animationLength = value;
        }
    }

    public int CellsPerTier
    {
        get
        {
            return cellsPerTier;
        }
        set
        {
            UnityEngine.Debug.Assert(value > 0, "Cells per tier should have a positive non zero value");
            cellsPerTier = Mathf.Max(1, value);
        }
    }

    public int TiersPerPage
    {
        get
        {
            return tiersPerPage;
        }
        set
        {
            UnityEngine.Debug.Assert(value > 0, "Tiers per page should have a positive non zero value");
            tiersPerPage = Mathf.Max(1, value);
        }
    }

    public float CellWidth
    {
        get
        {
            return cellWidth;
        }
        set
        {
            UnityEngine.Debug.Assert(value > 0f, "Cell width should have a positive non zero value");
            cellWidth = Mathf.Max(0.001f, value);
        }
    }

    public float CellHeight
    {
        get
        {
            return cellHeight;
        }
        set
        {
            UnityEngine.Debug.Assert(cellHeight > 0f, "Cell height should have a positive non zero value");
            cellHeight = Mathf.Max(0.001f, value);
        }
    }

    public float CellDepth
    {
        get
        {
            return cellDepth;
        }
        set
        {
            UnityEngine.Debug.Assert(value > 0f, "Cell depth should have a positive non zero value");
            cellDepth = Mathf.Max(0.001f, value);
        }
    }

    public float BounceMultiplier
    {
        get
        {
            return bounceMultiplier;
        }
        set
        {
            bounceMultiplier = value;
        }
    }

    private float MaxY
    {
        get
        {
            _ = contentBounds;
            float num = ((contentBounds.size.y <= 0f) ? 0f : Mathf.Max(0f, contentBounds.size.y - (float)TiersPerPage * CellHeight));
            if (maskEditMode == EditMode.Auto)
            {
                num = Mathf.Round(SafeDivisionFloat(num, CellHeight)) * CellHeight;
            }

            return num;
        }
    }

    private float MinX
    {
        get
        {
            _ = contentBounds;
            float num = ((contentBounds.size.x <= 0f) ? 0f : Mathf.Max(0f, contentBounds.size.x - (float)TiersPerPage * CellWidth));
            if (maskEditMode == EditMode.Auto)
            {
                num = Mathf.Round(SafeDivisionFloat(num, CellWidth)) * CellWidth;
            }

            return num * -1f;
        }
    }

    public int FirstVisibleCellIndex
    {
        get
        {
            if (scrollDirection == ScrollDirectionType.UpAndDown)
            {
                return (int)Mathf.Ceil(ScrollContainer.transform.localPosition.y / CellHeight) * CellsPerTier;
            }

            return (int)Mathf.Ceil(Mathf.Abs(ScrollContainer.transform.localPosition.x / CellWidth)) * CellsPerTier;
        }
    }

    public int FirstHiddenCellIndex
    {
        get
        {
            if (scrollDirection == ScrollDirectionType.UpAndDown)
            {
                return (int)Mathf.Floor(ScrollContainer.transform.localPosition.y / CellHeight) * CellsPerTier + TiersPerPage * CellsPerTier;
            }

            return (int)Mathf.Floor((0f - ScrollContainer.transform.localPosition.x) / CellWidth) * CellsPerTier + TiersPerPage * CellsPerTier;
        }
    }

    public BoxCollider ScrollingCollider
    {
        get
        {
            if (scrollingCollider == null)
            {
                scrollingCollider = base.gameObject.EnsureComponent<BoxCollider>();
            }

            return scrollingCollider;
        }
    }

    public NearInteractionTouchable ScrollingTouchable
    {
        get
        {
            if (scrollingTouchable == null)
            {
                scrollingTouchable = base.gameObject.EnsureComponent<NearInteractionTouchable>();
            }

            return scrollingTouchable;
        }
    }

    public Vector3 ScrollContainerPosition => ScrollContainer.transform.localPosition;

    private GameObject ScrollContainer
    {
        get
        {
            if (scrollContainer == null)
            {
                Transform transform = base.transform.Find("Container");
                if (transform != null)
                {
                    scrollContainer = transform.gameObject;
                    UnityEngine.Debug.LogWarning(base.name + " ScrollingObjectCollection found an existing Container object, using it for the list");
                }
                else
                {
                    scrollContainer = new GameObject();
                    scrollContainer.name = "Container";
                    scrollContainer.transform.parent = base.transform;
                    scrollContainer.transform.localPosition = Vector3.zero;
                    scrollContainer.transform.localRotation = Quaternion.identity;
                }
            }

            return scrollContainer;
        }
    }

    public GameObject ClippingObject
    {
        get
        {
            if (clippingObject == null)
            {
                Transform transform = base.transform.Find("Clipping Bounds");
                if (transform != null)
                {
                    clippingObject = transform.gameObject;
                    UnityEngine.Debug.LogWarning(base.name + " ScrollingObjectCollection found an existing Clipping object, using it for the list");
                }
                else
                {
                    clippingObject = new GameObject();
                }

                clippingObject.name = "Clipping Bounds";
                clippingObject.transform.parent = base.transform;
                clippingObject.transform.localRotation = Quaternion.identity;
                clippingObject.transform.localPosition = Vector3.zero;
            }

            return clippingObject;
        }
    }

    public ClippingBox ClipBox
    {
        get
        {
            if (clipBox == null)
            {
                clipBox = ClippingObject.EnsureComponent<ClippingBox>();
                clipBox.ClippingSide = ClippingPrimitive.Side.Outside;
            }

            return clipBox;
        }
    }

    private Collider ClippingBoundsCollider
    {
        get
        {
            if (clippingBoundsCollider == null)
            {
                clippingBoundsCollider = ClippingObject.EnsureComponent<BoxCollider>();
                clippingBoundsCollider.enabled = false;
            }

            return clippingBoundsCollider;
        }
    }

    public bool IsEngaged { get; private set; } = false;


    public bool IsDragging { get; private set; } = false;


    public bool IsTouched { get; private set; } = false;


    public bool HasMomentum { get; private set; } = false;


    private VelocityState CurrentVelocityState
    {
        get
        {
            return currentVelocityState;
        }
        set
        {
            if (value != currentVelocityState)
            {
                if (value == VelocityState.None)
                {
                    OnMomentumEnded.Invoke();
                }
                else if (currentVelocityState == VelocityState.None)
                {
                    OnMomentumStarted.Invoke();
                }

                previousVelocityState = currentVelocityState;
                currentVelocityState = value;
            }
        }
    }

    public bool DisableClippedGameObjects
    {
        get
        {
            return disableClippedGameObjects;
        }
        set
        {
            disableClippedGameObjects = value;
        }
    }

    public bool DisableClippedRenderers
    {
        get
        {
            return disableClippedRenderers;
        }
        set
        {
            disableClippedRenderers = value;
        }
    }

    public void UpdateContent()
    {
        UpdateContentBounds();
        SetupScrollingInteractionCollider();
        SetupClippingObject();
        ManageVisibility();
    }

    public void UpdateContentBounds()
    {
        Quaternion rotation = base.transform.rotation;
        base.transform.rotation = Quaternion.identity;
        Renderer[] componentsInChildren = ScrollContainer.GetComponentsInChildren<Renderer>(includeInactive: true);
        if (componentsInChildren != null)
        {
            contentBounds = new Bounds
            {
                size = Vector3.zero,
                center = ClipBox.transform.position
            };
            Renderer[] array = componentsInChildren;
            foreach (Renderer renderer in array)
            {
                contentBounds.Encapsulate(renderer.bounds);
            }

            Vector3 size = default(Vector3);
            size.y = SafeDivisionFloat(contentBounds.size.y, base.transform.lossyScale.y);
            size.x = SafeDivisionFloat(contentBounds.size.x, base.transform.lossyScale.x);
            size.z = SafeDivisionFloat(contentBounds.size.z, base.transform.lossyScale.z);
            contentBounds.size = size;
        }

        base.transform.rotation = rotation;
    }

    private void SetupScrollingInteractionCollider()
    {
        if (colliderEditMode != EditMode.Manual)
        {
            if (scrollDirection == ScrollDirectionType.UpAndDown)
            {
                ScrollingCollider.size = new Vector3(CellWidth * (float)CellsPerTier, CellHeight * (float)TiersPerPage, 0.001f);
            }
            else
            {
                ScrollingCollider.size = new Vector3(CellWidth * (float)TiersPerPage, CellHeight * (float)CellsPerTier, 0.001f);
            }

            Vector3 vector = default(Vector3);
            vector.x = ScrollingCollider.size.x / 2f;
            vector.y = (0f - ScrollingCollider.size.y) / 2f;
            vector.z = cellDepth / 2f + 0.001f;
            ScrollingCollider.center = vector;
            Vector2 bounds = new Vector2(Math.Abs(Vector3.Dot(ScrollingCollider.size, ScrollingTouchable.LocalRight)), Math.Abs(Vector3.Dot(ScrollingCollider.size, ScrollingTouchable.LocalUp)));
            Vector3 localCenter = vector;
            localCenter.z = (0f - cellDepth) / 2f;
            ScrollingTouchable.SetBounds(bounds);
            ScrollingTouchable.SetLocalCenter(localCenter);
        }
    }

    private void SetupClippingObject()
    {
        if (maskEditMode != EditMode.Manual)
        {
            Bounds otherBounds = default(Bounds);
            otherBounds.size = Vector3.one;
            Vector3 localPosition = default(Vector3);
            ScrollDirectionType scrollDirectionType = scrollDirection;
            ScrollDirectionType scrollDirectionType2 = scrollDirectionType;
            if (scrollDirectionType2 == ScrollDirectionType.UpAndDown || scrollDirectionType2 != ScrollDirectionType.LeftAndRight)
            {
                otherBounds.size = new Vector3(CellWidth * (float)CellsPerTier, CellHeight * (float)TiersPerPage, CellDepth);
                ClipBox.transform.localScale = new Bounds(Vector3.zero, Vector3.one).GetScaleToMatchBounds(otherBounds);
            }
            else
            {
                otherBounds.size = new Vector3(CellWidth * (float)TiersPerPage, CellHeight * (float)CellsPerTier, CellDepth);
                ClipBox.transform.localScale = new Bounds(Vector3.zero, Vector3.one).GetScaleToMatchBounds(otherBounds);
            }

            localPosition.x = ClipBox.transform.localScale.x * 0.5f;
            localPosition.y = ClipBox.transform.localScale.y * -0.5f;
            localPosition.z = 0f;
            ClipBox.transform.localPosition = localPosition;
        }
    }

    private void OnEnable()
    {
        CoreServices.InputSystem?.RegisterHandler<IMixedRealitySourceStateHandler>(this);
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityTouchHandler>(this);
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityPointerHandler>(this);
        if (useOnPreRender)
        {
            ClipBox.UseOnPreRender = true;
            if (cameraMethods == null)
            {
                cameraMethods = CameraCache.Main.gameObject.EnsureComponent<CameraEventRouter>();
            }

            cameraMethods.OnCameraPreRender += OnCameraPreRender;
        }
    }

    private void Start()
    {
        UpdateContent();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (ScrollContainer.GetComponentInChildren<Renderer>(includeInactive: true) == null)
        {
            workingScrollerPos = Vector3.zero;
            ApplyPosition(workingScrollerPos);
            return;
        }

        if (IsEngaged && TryGetPointerPositionOnPlane(out var result))
        {
            Vector3 direction = initialPointerPos - result;
            direction = base.transform.InverseTransformDirection(direction);
            if (IsDragging && currentPointer != null)
            {
                currentPointer.IsFocusLocked = true;
            }

            if (!IsDragging)
            {
                float num = ((scrollDirection == ScrollDirectionType.UpAndDown) ? Mathf.Abs(direction.y) : Mathf.Abs(direction.x));
                if (num > handDeltaScrollThreshold)
                {
                    scrollVelocity = 0f;
                    avgVelocity = 0f;
                    IsDragging = true;
                    direction = Vector3.zero;
                    CurrentVelocityState = VelocityState.Dragging;
                    initialScrollerPos = (workingScrollerPos = ScrollContainer.transform.localPosition);
                    initialPointerPos = result;
                }
            }

            if (IsTouched && DetectScrollRelease(result))
            {
                if (IsDragging)
                {
                    initialScrollerPos = workingScrollerPos;
                    CurrentVelocityState = VelocityState.Calculating;
                }
                else
                {
                    OnClick?.Invoke(initialFocusedObject);
                }

                ResetInteraction();
            }
            else if (IsDragging && canScroll)
            {
                if (scrollDirection == ScrollDirectionType.UpAndDown)
                {
                    float num2 = SafeDivisionFloat(direction.y, base.transform.lossyScale.y);
                    if (workingScrollerPos.y > MaxY || workingScrollerPos.y < minY)
                    {
                        workingScrollerPos.y = MathUtilities.CLampLerp(initialScrollerPos.y - num2, minY, MaxY, 0.9f);
                    }
                    else
                    {
                        workingScrollerPos.y = MathUtilities.CLampLerp(initialScrollerPos.y - num2, minY, MaxY, 0.5f);
                    }

                    workingScrollerPos.x = 0f;
                }
                else
                {
                    float num3 = SafeDivisionFloat(direction.x, base.transform.lossyScale.x);
                    if (workingScrollerPos.x > maxX || workingScrollerPos.x < MinX)
                    {
                        workingScrollerPos.x = MathUtilities.CLampLerp(initialScrollerPos.x - num3, MinX, maxX, 0.9f);
                    }
                    else
                    {
                        workingScrollerPos.x = MathUtilities.CLampLerp(initialScrollerPos.x - num3, MinX, maxX, 0.5f);
                    }

                    workingScrollerPos.y = 0f;
                }

                ApplyPosition(workingScrollerPos);
                CalculateVelocity();
                lastPointerPos = result;
            }
        }
        else if ((CurrentVelocityState != 0 || previousVelocityState != 0) && CurrentVelocityState != VelocityState.Animating)
        {
            HandleVelocityFalloff();
            ApplyPosition(workingScrollerPos);
        }

        if (CurrentVelocityState != 0 || previousVelocityState != 0)
        {
            HasMomentum = true;
        }
        else
        {
            HasMomentum = false;
        }

        previousVelocityState = CurrentVelocityState;
    }

    private void LateUpdate()
    {
        if (!UseOnPreRender)
        {
            ManageVisibility();
        }
    }

    private void OnDisable()
    {
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealitySourceStateHandler>(this);
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityTouchHandler>(this);
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityPointerHandler>(this);
        if (!Application.isPlaying)
        {
            RestoreContentVisibility(!(new StackFrame(1)?.GetMethod()?.Name?.Contains("Paste")).GetValueOrDefault());
        }
        else
        {
            RestoreContentVisibility();
        }

        if (useOnPreRender && cameraMethods != null)
        {
            CameraEventRouter cameraEventRouter = CameraCache.Main.gameObject.EnsureComponent<CameraEventRouter>();
            cameraEventRouter.OnCameraPreRender -= OnCameraPreRender;
        }
    }

    private void OnCameraPreRender(CameraEventRouter router)
    {
        ManageVisibility();
    }

    private void ReconcileClippingContent()
    {
        if (renderersToClip.Count > 0)
        {
            AddRenderersToClippingObject(renderersToClip);
            renderersToClip.Clear();
        }

        if (renderersToUnclip.Count > 0)
        {
            RemoveRenderersFromClippingObject(renderersToUnclip);
            renderersToUnclip.Clear();
        }
    }

    private bool TryGetPointerPositionOnPlane(out Vector3 result)
    {
        result = Vector3.zero;
        if ((MonoBehaviour)currentPointer == null)
        {
            return false;
        }

        if (currentPointer.GetType() == typeof(PokePointer))
        {
            result = currentPointer.Position;
            return true;
        }

        Vector3 onNormal = ((scrollDirection == ScrollDirectionType.UpAndDown) ? base.transform.up : base.transform.right);
        result = base.transform.position + Vector3.Project(currentPointer.Position - base.transform.position, onNormal);
        return true;
    }

    private void HandleVelocityFalloff()
    {
        switch (typeOfVelocity)
        {
            case VelocityType.FalloffPerFrame:
                HandleFalloffPerFrame();
                break;
            default:
                HandleFalloffPerItem();
                break;
            case VelocityType.NoVelocitySnapToItem:
                CurrentVelocityState = VelocityState.None;
                avgVelocity = 0f;
                if (scrollDirection == ScrollDirectionType.UpAndDown)
                {
                    workingScrollerPos.y = Mathf.Round(ScrollContainer.transform.localPosition.y / CellHeight) * CellHeight;
                }
                else
                {
                    workingScrollerPos.x = Mathf.Round(ScrollContainer.transform.localPosition.x / CellWidth) * CellWidth;
                }

                initialScrollerPos = workingScrollerPos;
                break;
            case VelocityType.None:
                CurrentVelocityState = VelocityState.None;
                avgVelocity = 0f;
                break;
        }

        if (CurrentVelocityState == VelocityState.None)
        {
            workingScrollerPos.y = Mathf.Clamp(workingScrollerPos.y, minY, MaxY);
            workingScrollerPos.x = Mathf.Clamp(workingScrollerPos.x, MinX, maxX);
        }
    }

    private void HandleFalloffPerItem()
    {
        switch (CurrentVelocityState)
        {
            case VelocityState.Calculating:
                {
                    int steps;
                    if (scrollDirection == ScrollDirectionType.UpAndDown)
                    {
                        float num;
                        if (avgVelocity == 0f)
                        {
                            num = ScrollContainer.transform.localPosition.y;
                        }
                        else
                        {
                            velocitySnapshot = IterateFalloff(avgVelocity, out steps);
                            num = initialScrollerPos.y - velocitySnapshot;
                        }

                        velocityDestinationPos.y = Mathf.Round(num / CellHeight) * CellHeight;
                        CurrentVelocityState = VelocityState.Resolving;
                    }
                    else
                    {
                        float num;
                        if (avgVelocity == 0f)
                        {
                            num = ScrollContainer.transform.localPosition.x;
                        }
                        else
                        {
                            velocitySnapshot = IterateFalloff(avgVelocity, out steps);
                            num = initialScrollerPos.x + velocitySnapshot;
                        }

                        velocityDestinationPos.x = Mathf.Round(num / CellWidth) * CellWidth;
                        CurrentVelocityState = VelocityState.Resolving;
                    }

                    workingScrollerPos = Solver.SmoothTo(scrollContainer.transform.localPosition, velocityDestinationPos, Time.deltaTime, 0.2f);
                    avgVelocity = 0f;
                    break;
                }
            case VelocityState.Resolving:
                if (scrollDirection == ScrollDirectionType.UpAndDown)
                {
                    if (ScrollContainer.transform.localPosition.y > MaxY || ScrollContainer.transform.localPosition.y < minY)
                    {
                        CurrentVelocityState = VelocityState.Bouncing;
                        velocitySnapshot = 0f;
                    }
                    else
                    {
                        workingScrollerPos = Solver.SmoothTo(ScrollContainer.transform.localPosition, velocityDestinationPos, Time.deltaTime, 0.2f);
                        SnapVelocityFinish();
                    }
                }
                else if (ScrollContainer.transform.localPosition.x > maxX + FrontTouchDistance * bounceMultiplier || ScrollContainer.transform.localPosition.x < MinX - FrontTouchDistance * bounceMultiplier)
                {
                    CurrentVelocityState = VelocityState.Bouncing;
                    velocitySnapshot = 0f;
                }
                else
                {
                    workingScrollerPos = Solver.SmoothTo(ScrollContainer.transform.localPosition, velocityDestinationPos, Time.deltaTime, 0.2f);
                    SnapVelocityFinish();
                }

                break;
            case VelocityState.Bouncing:
                HandleBounceState();
                break;
            default:
                initialScrollerPos = workingScrollerPos;
                break;
        }
    }

    private void HandleFalloffPerFrame()
    {
        switch (CurrentVelocityState)
        {
            case VelocityState.Calculating:
                if (scrollDirection == ScrollDirectionType.UpAndDown)
                {
                    workingScrollerPos.y = initialScrollerPos.y + avgVelocity;
                }
                else
                {
                    workingScrollerPos.x = initialScrollerPos.x + avgVelocity;
                }

                CurrentVelocityState = VelocityState.Resolving;
                initialScrollerPos = workingScrollerPos;
                break;
            case VelocityState.Resolving:
                if (scrollDirection == ScrollDirectionType.UpAndDown)
                {
                    if (ScrollContainer.transform.localPosition.y > MaxY + FrontTouchDistance * bounceMultiplier || ScrollContainer.transform.localPosition.y < minY - FrontTouchDistance * bounceMultiplier)
                    {
                        CurrentVelocityState = VelocityState.Bouncing;
                        avgVelocity = 0f;
                        break;
                    }

                    avgVelocity *= velocityDampen;
                    workingScrollerPos.y = initialScrollerPos.y + avgVelocity;
                    SnapVelocityFinish();
                }
                else
                {
                    if (ScrollContainer.transform.localPosition.x > maxX + FrontTouchDistance * bounceMultiplier || ScrollContainer.transform.localPosition.x < MinX - FrontTouchDistance * bounceMultiplier)
                    {
                        CurrentVelocityState = VelocityState.Bouncing;
                        avgVelocity = 0f;
                        break;
                    }

                    avgVelocity *= velocityDampen;
                    workingScrollerPos.x = initialScrollerPos.x + avgVelocity;
                    SnapVelocityFinish();
                }

                initialScrollerPos = workingScrollerPos;
                break;
            case VelocityState.Bouncing:
                HandleBounceState();
                break;
        }
    }

    private void HandleBounceState()
    {
        Vector3 goal = new Vector3(Mathf.Clamp(ScrollContainer.transform.localPosition.x, MinX, maxX), Mathf.Clamp(ScrollContainer.transform.localPosition.y, minY, MaxY), 0f);
        if ((scrollDirection == ScrollDirectionType.UpAndDown && Mathf.Approximately(ScrollContainer.transform.localPosition.y, goal.y)) || (scrollDirection == ScrollDirectionType.LeftAndRight && Mathf.Approximately(ScrollContainer.transform.localPosition.x, goal.x)))
        {
            CurrentVelocityState = VelocityState.None;
            initialScrollerPos = (workingScrollerPos = goal);
        }
        else
        {
            workingScrollerPos.y = Solver.SmoothTo(ScrollContainer.transform.localPosition, goal, Time.deltaTime, 0.2f).y;
            workingScrollerPos.x = Solver.SmoothTo(ScrollContainer.transform.localPosition, goal, Time.deltaTime, 0.2f).x;
        }
    }

    private void SnapVelocityFinish()
    {
        if (Vector3.Distance(ScrollContainer.transform.localPosition, workingScrollerPos) > Mathf.Epsilon)
        {
            return;
        }

        if (typeOfVelocity == VelocityType.FalloffPerItem)
        {
            if (scrollDirection == ScrollDirectionType.UpAndDown)
            {
                workingScrollerPos.y = Mathf.Round(ScrollContainer.transform.localPosition.y / CellHeight) * CellHeight;
            }
            else
            {
                workingScrollerPos.x = Mathf.Round(ScrollContainer.transform.localPosition.x / CellWidth) * CellWidth;
            }
        }

        CurrentVelocityState = VelocityState.None;
        avgVelocity = 0f;
        initialScrollerPos = workingScrollerPos;
    }

    private void CalculateVelocity()
    {
        TryGetPointerPositionOnPlane(out var result);
        scrollVelocity = ((scrollDirection == ScrollDirectionType.UpAndDown) ? ((result.y - lastPointerPos.y) / Time.deltaTime * velocityMultiplier) : ((result.x - lastPointerPos.x) / Time.deltaTime * velocityMultiplier));
        avgVelocity = avgVelocity * (1f - velocityFilterWeight) + scrollVelocity * velocityFilterWeight;
    }

    private IEnumerator AnimateTo(Vector3 initialPos, Vector3 finalPos, AnimationCurve curve = null, float? time = null, Action callback = null)
    {
        if (curve == null)
        {
            curve = paginationCurve;
        }

        if (!time.HasValue)
        {
            time = animationLength;
        }

        float counter = 0f;
        while (counter <= time)
        {
            workingScrollerPos = Vector3.Lerp(initialPos, finalPos, curve.Evaluate(counter / time.Value));
            ScrollContainer.transform.localPosition = workingScrollerPos;
            counter += Time.deltaTime;
            yield return null;
        }

        if (scrollDirection == ScrollDirectionType.UpAndDown)
        {
            workingScrollerPos.y = (initialScrollerPos.y = finalPos.y);
        }
        else
        {
            workingScrollerPos.x = (initialScrollerPos.x = finalPos.x);
        }

        if (callback != null)
        {
            callback?.Invoke();
        }

        CurrentVelocityState = VelocityState.None;
        animateScroller = null;
    }

    private bool DetectScrollRelease(Vector3 pointerPos)
    {
        Vector3 vector = pointerPos - ClipBox.transform.position;
        return Vector3.Magnitude(Vector3.Project(vector, ClipBox.transform.up)) > ClipBox.transform.lossyScale.y / 2f + releaseThresholdTopBottom || Vector3.Magnitude(Vector3.Project(vector, ClipBox.transform.right)) > ClipBox.transform.lossyScale.x / 2f + releaseThresholdLeftRight || ((Vector3.Dot(vector, base.transform.forward) > 0f) ? (Vector3.Magnitude(Vector3.Project(vector, ClipBox.transform.forward)) > ClipBox.transform.lossyScale.z / 2f + releaseThresholdBack) : (Vector3.Magnitude(Vector3.Project(vector, ClipBox.transform.forward)) > ClipBox.transform.lossyScale.z / 2f + releaseThresholdFront));
    }

    private bool HasPassedThroughFrontPlane(PokePointer pokePointer)
    {
        return base.transform.InverseTransformPoint(pokePointer.PreviousPosition).z <= 0f - FrontTouchDistance;
    }

    private void AddRenderersToClippingObject(List<Renderer> renderers)
    {
        foreach (Renderer renderer in renderers)
        {
            ClipBox.AddRenderer(renderer);
        }
    }

    private void RemoveRenderersFromClippingObject(List<Renderer> renderers)
    {
        foreach (Renderer renderer in renderers)
        {
            ClipBox.RemoveRenderer(renderer);
        }
    }

    private void ClearClippingBox(bool autoDestroyMaterial = true)
    {
        ClipBox.ClearRenderers(autoDestroyMaterial);
    }

    private static int SafeDivisionInt(int numerator, int denominator)
    {
        return (denominator != 0) ? (numerator / denominator) : 0;
    }

    private float SafeDivisionFloat(float numerator, float denominator)
    {
        return (denominator != 0f) ? (numerator / denominator) : 0f;
    }

    public void ManageVisibility(bool isRestoringVisibility = false)
    {
        if (!MaskEnabled && !isRestoringVisibility)
        {
            return;
        }

        ClippingBoundsCollider.enabled = true;
        Bounds bounds = ClippingBoundsCollider.bounds;
        Renderer[] componentsInChildren = ScrollContainer.GetComponentsInChildren<Renderer>(includeInactive: true);
        clippedRenderers.Clear();
        clippedRenderers.UnionWith(ClipBox.GetRenderersCopy());
        foreach (Renderer clippedRenderer in clippedRenderers)
        {
            if (clippedRenderer != null && !clippedRenderer.transform.IsChildOf(ScrollContainer.transform))
            {
                if (disableClippedGameObjects && !clippedRenderer.gameObject.activeSelf)
                {
                    clippedRenderer.gameObject.SetActive(value: true);
                }

                if (disableClippedRenderers && !clippedRenderer.enabled)
                {
                    clippedRenderer.enabled = true;
                }

                renderersToUnclip.Add(clippedRenderer);
            }
        }

        Renderer[] array = componentsInChildren;
        foreach (Renderer renderer in array)
        {
            if (!isRestoringVisibility && MaskEnabled && !clippedRenderers.Contains(renderer))
            {
                renderersToClip.Add(renderer);
            }

            if (isRestoringVisibility || bounds.ContainsBounds(renderer.bounds) || bounds.Intersects(renderer.bounds))
            {
                if (disableClippedGameObjects && !renderer.gameObject.activeSelf)
                {
                    renderer.gameObject.SetActive(value: true);
                }

                if (disableClippedRenderers && !renderer.enabled)
                {
                    renderer.enabled = true;
                }
            }
            else
            {
                if (disableClippedGameObjects && renderer.gameObject.activeSelf)
                {
                    renderer.gameObject.SetActive(value: false);
                }

                if (disableClippedRenderers && renderer.enabled)
                {
                    renderer.enabled = false;
                }
            }
        }

        if (Application.isPlaying)
        {
            Bounds bounds2 = ClippingBoundsCollider.bounds;
            bounds2.size *= contentVisibilityThresholdRatio;
            Collider[] componentsInChildren2 = ScrollContainer.GetComponentsInChildren<Collider>(includeInactive: true);
            Collider[] array2 = componentsInChildren2;
            foreach (Collider collider in array2)
            {
                if (!isRestoringVisibility && IsDragging)
                {
                    if (collider.enabled)
                    {
                        collider.enabled = false;
                    }
                }
                else
                {
                    if (!isRestoringVisibility && !collider.gameObject.activeSelf)
                    {
                        continue;
                    }

                    bool flag = collider.enabled;
                    if (!flag)
                    {
                        collider.enabled = true;
                    }

                    if (isRestoringVisibility || bounds2.ContainsBounds(collider.bounds))
                    {
                        if (!flag)
                        {
                            flag = true;
                        }
                    }
                    else if (flag)
                    {
                        flag = false;
                    }

                    collider.enabled = flag;
                }
            }
        }

        ClippingBoundsCollider.enabled = false;
        if (!isRestoringVisibility)
        {
            ReconcileClippingContent();
        }
    }


    public void ManageVisibility1(bool isRestoringVisibility = false)
    {
        ClippingBoundsCollider.enabled = true;
        Bounds bounds = ClippingBoundsCollider.bounds;
        Renderer[] componentsInChildren = ScrollContainer.GetComponentsInChildren<Renderer>(includeInactive: true);
        clippedRenderers.Clear();
        clippedRenderers.UnionWith(ClipBox.GetRenderersCopy());
        foreach (Renderer clippedRenderer in clippedRenderers)
        {
            if (clippedRenderer != null && !clippedRenderer.transform.IsChildOf(ScrollContainer.transform))
            {
                if (disableClippedGameObjects && !clippedRenderer.gameObject.activeSelf)
                {
                    clippedRenderer.gameObject.SetActive(value: true);
                }

                if (disableClippedRenderers && !clippedRenderer.enabled)
                {
                    clippedRenderer.enabled = true;
                }

                renderersToUnclip.Add(clippedRenderer);
            }
        }

        Renderer[] array = componentsInChildren;
        foreach (Renderer renderer in array)
        {
            if (!isRestoringVisibility && MaskEnabled && !clippedRenderers.Contains(renderer))
            {
                renderersToClip.Add(renderer);
            }

            if (isRestoringVisibility || bounds.ContainsBounds(renderer.bounds) || bounds.Intersects(renderer.bounds))
            {
                if (disableClippedGameObjects && !renderer.gameObject.activeSelf)
                {
                    renderer.gameObject.SetActive(value: true);
                }

                if (disableClippedRenderers && !renderer.enabled)
                {
                    renderer.enabled = true;
                }
            }
            else
            {
                if (disableClippedGameObjects && renderer.gameObject.activeSelf)
                {
                    renderer.gameObject.SetActive(value: false);
                }

                if (disableClippedRenderers && renderer.enabled)
                {
                    renderer.enabled = false;
                }
            }
        }
    }
    public void ManageVisibility2(bool isRestoringVisibility = false) {

        if (Application.isPlaying)
        {
            Bounds bounds2 = ClippingBoundsCollider.bounds;
            bounds2.size *= contentVisibilityThresholdRatio;
            Collider[] componentsInChildren2 = ScrollContainer.GetComponentsInChildren<Collider>(includeInactive: true);
            Collider[] array2 = componentsInChildren2;
            foreach (Collider collider in array2)
            {
                if (!isRestoringVisibility && IsDragging)
                {
                    if (collider.enabled)
                    {
                        collider.enabled = false;
                    }
                }
                else
                {
                    if (!isRestoringVisibility && !collider.gameObject.activeSelf)
                    {
                        continue;
                    }

                    bool flag = collider.enabled;
                    if (!flag)
                    {
                        collider.enabled = true;
                    }

                    if (isRestoringVisibility || bounds2.ContainsBounds(collider.bounds))
                    {
                        if (!flag)
                        {
                            flag = true;
                        }
                    }
                    else if (flag)
                    {
                        flag = false;
                    }

                    collider.enabled = flag;
                }
            }
        }

        ClippingBoundsCollider.enabled = false;
        if (!isRestoringVisibility)
        {
            ReconcileClippingContent();
        }
    }

    private float IterateFalloff(float vel, out int steps)
    {
        float num = 0f;
        float num2 = vel;
        steps = 0;
        while ((double)Mathf.Abs(num2) > 1E-05)
        {
            num2 *= velocityDampen;
            num += num2;
            steps++;
        }

        return num;
    }

    private void ApplyPosition(Vector3 workingPos)
    {
        ScrollDirectionType scrollDirectionType = scrollDirection;
        ScrollDirectionType scrollDirectionType2 = scrollDirectionType;
        Vector3 localPosition = ((scrollDirectionType2 != 0 && scrollDirectionType2 == ScrollDirectionType.LeftAndRight) ? new Vector3(workingPos.x, ScrollContainer.transform.localPosition.y, 0f) : new Vector3(ScrollContainer.transform.localPosition.x, workingPos.y, 0f));
        ScrollContainer.transform.localPosition = localPosition;
    }

    private void ResetInteraction()
    {
        OnTouchEnded?.Invoke(initialFocusedObject);
        if (currentPointer != null)
        {
            currentPointer.IsFocusLocked = false;
        }

        currentPointer = null;
        initialFocusedObject = null;
        IsTouched = false;
        IsEngaged = false;
        IsDragging = false;
    }

    private void ResetScrollOffset()
    {
        MoveToIndex(0, animateToPosition: false);
        workingScrollerPos = Vector3.zero;
        ApplyPosition(workingScrollerPos);
    }

    private void RestoreContentVisibility(bool autoDestroyMaterial = true)
    {
        ClearClippingBox(autoDestroyMaterial);
        ManageVisibility(isRestoringVisibility: true);
    }

    private void MoveToTier(int tierIndex, bool animateToPosition = true, Action callback = null)
    {
        if (animateScroller != null)
        {
            CurrentVelocityState = VelocityState.None;
            StopAllCoroutines();
        }

        if (scrollDirection == ScrollDirectionType.UpAndDown)
        {
            workingScrollerPos.y = (float)tierIndex * CellHeight;
            workingScrollerPos.y = Mathf.Clamp(workingScrollerPos.y, minY, MaxY);
            workingScrollerPos = workingScrollerPos.Mul(Vector3.up);
        }
        else
        {
            workingScrollerPos.x = (float)tierIndex * CellWidth * -1f;
            workingScrollerPos.x = Mathf.Clamp(workingScrollerPos.x, MinX, maxX);
            workingScrollerPos = workingScrollerPos.Mul(Vector3.right);
        }

        if (initialScrollerPos != workingScrollerPos)
        {
            CurrentVelocityState = VelocityState.Animating;
            if (animateToPosition)
            {
                animateScroller = AnimateTo(ScrollContainer.transform.localPosition, workingScrollerPos, paginationCurve, animationLength, callback);
                StartCoroutine(animateScroller);
            }
            else
            {
                CurrentVelocityState = VelocityState.None;
                initialScrollerPos = workingScrollerPos;
            }

            if (callback != null)
            {
                callback?.Invoke();
            }
        }
    }

    public void Reset()
    {
        ResetInteraction();
        UpdateContent();
        ResetScrollOffset();
    }

    public void AddContent(GameObject content)
    {
        content.transform.parent = ScrollContainer.transform;
        Reset();
    }

    public void RemoveItem(GameObject item)
    {
        if (item == null)
        {
            return;
        }

        Renderer[] componentsInChildren = item.GetComponentsInChildren<Renderer>();
        if (componentsInChildren != null)
        {
            Renderer[] array = componentsInChildren;
            foreach (Renderer item2 in array)
            {
                renderersToUnclip.Add(item2);
            }
        }

        item.transform.parent = null;
        Reset();
    }

    public bool IsCellVisible(int cellIndex)
    {
        bool result = true;
        if (cellIndex < FirstVisibleCellIndex)
        {
            result = false;
        }
        else if (cellIndex >= FirstHiddenCellIndex)
        {
            result = false;
        }

        return result;
    }

    public void MoveByPages(int numberOfPages, bool animate = true, Action callback = null)
    {
        int tierIndex = SafeDivisionInt(FirstVisibleCellIndex, CellsPerTier) + numberOfPages * TiersPerPage;
        MoveToTier(tierIndex, animate, callback);
    }

    public void MoveByTiers(int numberOfTiers, bool animate = true, Action callback = null)
    {
        int tierIndex = SafeDivisionInt(FirstVisibleCellIndex, CellsPerTier) + numberOfTiers;
        MoveToTier(tierIndex, animate, callback);
    }

    public void MoveToIndex(int cellIndex, bool animateToPosition = true, Action callback = null)
    {
        cellIndex = ((cellIndex >= 0) ? cellIndex : 0);
        int tierIndex = SafeDivisionInt(cellIndex, CellsPerTier);
        MoveToTier(tierIndex, animateToPosition, callback);
    }

    void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData)
    {
        if (currentPointer == null || eventData.Pointer.PointerId != currentPointer.PointerId)
        {
            return;
        }

        currentPointer.IsTargetPositionLockedOnFocusLock = oldIsTargetPositionLockedOnFocusLock;
        if (!IsTouched && IsEngaged && animateScroller == null)
        {
            if (IsDragging)
            {
                initialScrollerPos = workingScrollerPos;
                CurrentVelocityState = VelocityState.Calculating;
            }

            ResetInteraction();
        }
    }

    void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData)
    {
        if (currentPointer != null)
        {
            return;
        }

        GameObject gameObject = eventData.Pointer.Result?.CurrentPointerTarget;
        if (!(gameObject == null) && gameObject.transform.IsChildOf(base.transform))
        {
            currentPointer = eventData.Pointer;
            oldIsTargetPositionLockedOnFocusLock = currentPointer.IsTargetPositionLockedOnFocusLock;
            if (!(currentPointer is IMixedRealityNearPointer) && currentPointer.Controller.IsRotationAvailable)
            {
                currentPointer.IsTargetPositionLockedOnFocusLock = false;
            }

            initialFocusedObject = gameObject;
            currentPointer.IsFocusLocked = false;
            scrollVelocity = 0f;
            if (TryGetPointerPositionOnPlane(out initialPointerPos))
            {
                initialScrollerPos = ScrollContainer.transform.localPosition;
                CurrentVelocityState = VelocityState.None;
                IsTouched = false;
                IsEngaged = true;
                IsDragging = false;
                OnTouchStarted?.Invoke(initialFocusedObject);
            }
        }
    }

    void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData)
    {
    }

    void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData eventData)
    {
    }

    void IMixedRealityTouchHandler.OnTouchStarted(HandTrackingInputEventData eventData)
    {
        if (currentPointer != null)
        {
            return;
        }

        PokePointer pointer = PointerUtils.GetPointer<PokePointer>(eventData.Handedness);
        GameObject gameObject = pointer.Result?.CurrentPointerTarget;
        if (!(gameObject == null) && gameObject.transform.IsChildOf(base.transform) && HasPassedThroughFrontPlane(pointer))
        {
            currentPointer = pointer;
            StopAllCoroutines();
            CurrentVelocityState = VelocityState.None;
            animateScroller = null;
            if (!IsTouched && !IsEngaged)
            {
                initialPointerPos = currentPointer.Position;
                initialFocusedObject = gameObject;
                initialScrollerPos = ScrollContainer.transform.localPosition;
                IsTouched = true;
                IsEngaged = true;
                IsDragging = false;
                OnTouchStarted?.Invoke(initialFocusedObject);
            }
        }
    }

    void IMixedRealityTouchHandler.OnTouchCompleted(HandTrackingInputEventData eventData)
    {
    }

    void IMixedRealityTouchHandler.OnTouchUpdated(HandTrackingInputEventData eventData)
    {
        if (currentPointer != null && eventData.SourceId == currentPointer.InputSourceParent.SourceId && IsDragging)
        {
            eventData.Use();
        }
    }

    void IMixedRealitySourceStateHandler.OnSourceDetected(SourceStateEventData eventData)
    {
    }

    void IMixedRealitySourceStateHandler.OnSourceLost(SourceStateEventData eventData)
    {
        if (currentPointer != null && eventData.SourceId == currentPointer.InputSourceParent.SourceId && IsEngaged && animateScroller == null)
        {
            if (IsTouched || IsDragging)
            {
                initialScrollerPos = workingScrollerPos;
            }

            ResetInteraction();
            CurrentVelocityState = VelocityState.Calculating;
        }
    }
}

