using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CircleSelectionPopup : MonoBehaviour
{
    [Header("UI")]
    public GameObject popupRoot;
    public Text titleText;
    public Text detailText;
    public TMP_Text titleTMP;
    public TMP_Text detailTMP;
    public Button openSplatButton;

    [Header("Splat")]
    public GaussianSplatManager gaussianSplatManager;

    [Header("World Placement")]
    public bool placeInWorldOnShow = true;
    public Vector3 worldOffset = new Vector3(0f, 0.35f, 0f);
    public bool copyTargetRotationOnShow = true;
    public Vector2 screenOffset = new Vector2(0f, 40f);

    private CircleItem currentItem;
    private string currentTitle;

    void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        if (gaussianSplatManager == null)
            gaussianSplatManager = FindObjectOfType<GaussianSplatManager>(true);

        if (openSplatButton != null)
            openSplatButton.onClick.AddListener(OpenSplatInBrowserFromPopup);
    }

    void Start()
    {
        Hide();
    }

    public void Show(CircleItem item, Transform anchor, string titleOverride = null)
    {
        currentItem = item;
        currentTitle = titleOverride;

        SetVisualActive(true);

        if (placeInWorldOnShow && popupRoot != null && anchor != null)
        {
            PlacePopupAtAnchor(anchor);
        }

        string title = !string.IsNullOrEmpty(titleOverride)
            ? titleOverride
            : (item != null ? item.id : "Element");
        SetText(titleText, titleTMP, title);

        if (item != null)
        {
            string detail = $"Size: {item.size}\nColor: {item.color}\nShape Index: {item.shapeIndex}";
            SetText(detailText, detailTMP, detail);
        }
    }

    public void Hide()
    {
        currentItem = null;
        currentTitle = null;
        SetVisualActive(false);
    }

    public void OpenSplatFromPopup()
    {
        if (gaussianSplatManager == null)
            return;

        bool opened = gaussianSplatManager.TryOpenMappedInOverlay(currentItem, currentTitle);
        if (!opened)
            gaussianSplatManager.OpenTestSplat();
    }

    public void OpenSplatInBrowserFromPopup()
    {
        if (gaussianSplatManager == null)
            return;

        bool opened = gaussianSplatManager.TryOpenMappedInBrowser(currentItem, currentTitle);
        if (!opened)
            gaussianSplatManager.OpenTestSplatInBrowser();
    }

    public void OpenTestSplat()
    {
        if (gaussianSplatManager == null)
            return;

        gaussianSplatManager.OpenTestSplat();
    }

    void SetText(Text legacy, TMP_Text tmp, string value)
    {
        if (legacy != null)
            legacy.text = value;

        if (tmp != null)
            tmp.text = value;
    }

    void SetVisualActive(bool isActive)
    {
        if (popupRoot != null)
            popupRoot.SetActive(isActive);

        if (titleText != null)
            titleText.gameObject.SetActive(isActive);
        if (detailText != null)
            detailText.gameObject.SetActive(isActive);
        if (titleTMP != null)
            titleTMP.gameObject.SetActive(isActive);
        if (detailTMP != null)
            detailTMP.gameObject.SetActive(isActive);
    }

    void PlacePopupAtAnchor(Transform anchor)
    {
        if (popupRoot == null || anchor == null)
            return;

        Canvas canvas = popupRoot.GetComponentInParent<Canvas>();
        Vector3 worldPos = anchor.position + worldOffset;

        // Fall 1: World-Space Canvas oder 3D-Objekt
        if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
        {
            popupRoot.transform.position = worldPos;
            if (copyTargetRotationOnShow)
                popupRoot.transform.rotation = anchor.rotation;
            return;
        }

        // Fall 2: Screen-Space Canvas (Overlay/Camera)
        RectTransform canvasRect = canvas.transform as RectTransform;
        RectTransform popupRect = popupRoot.transform as RectTransform;
        if (canvasRect == null || popupRect == null)
            return;

        Camera eventCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector3 screenPoint = Camera.main != null ? Camera.main.WorldToScreenPoint(worldPos) : Vector3.zero;
        screenPoint.x += screenOffset.x;
        screenPoint.y += screenOffset.y;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, eventCam, out Vector2 localPoint))
            popupRect.anchoredPosition = localPoint;
    }
}
