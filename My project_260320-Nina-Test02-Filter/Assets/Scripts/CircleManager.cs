using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Drei diskrete Größenklassen
public enum CircleSizeCategory { S, M, L }

// Drei diskrete Farbklassen
public enum ColorCategory { Orange, Violet, Red }

[System.Serializable]
public class CircleItem
{
    public string id;                 // z.B. SQ-A_S_O
    public CircleSizeCategory size;   // S, M, L
    public ColorCategory color;       // Orange, Violet, Red
    public int shapeIndex;            // Index in Shape-Textur-Liste

    public CircleItem(string id, CircleSizeCategory size, ColorCategory color, int shapeIndex)
    {
        this.id = id;
        this.size = size;
        this.color = color;
        this.shapeIndex = shapeIndex;
    }
}

[ExecuteAlways]
public class CircleManager : MonoBehaviour
{
    public enum SpaceMode
    {
        Grid2D,
        Space3D
    }

    public enum Cube3DSize
    {
        Size3x3x3 = 27,    // 3³
        Size4x4x4 = 64,    // 4³
        Size5x5x5 = 125    // 5³
    }

    [Header("Setup")]
    public GameObject circlePrefab;
    [Tooltip("Optional: kleine Marker-Objekte zur Visualisierung der Grid-Grenzen.")]
    public GameObject boundaryMarkerPrefab;

    [Header("Layout Settings")]
    public float sizeColumnSpacing = 1.5f;
    public float rowSpacing = 1.2f;
    public float colorLineLength = 10f;
    public float colorSpacingMultiplier = .2f;
    public float baseScale = 0.3f;
    [Tooltip("Zusätzliche Abstufungen innerhalb von S/M/L.")]
    public int sizeVariationSteps = 3;
    [Tooltip("Gesamte Spannweite der Größenvariation innerhalb einer Kategorie.")]
    public float sizeVariationSpread = 0.2f;

    [Header("Grid Settings")]
    [Tooltip("Maximale Anzahl Elemente hintereinander (Tiefe), bevor in derselben Gruppe eine neue Reihe beginnt.")]
    public int maxItemsPerColumn = 5;
    [Tooltip("Wenn aktiv, verwenden Size- und Color-Layout ein identisches Grid (konstante Abstände).")]
    public bool useUniformGridSpacing = true;
    [Tooltip("Fester Zellabstand für das einheitliche Grid.")]
    public float uniformGridCellSize = 1.0f;

    [Header("Grid Layout Mode")]
    [Tooltip("Wenn > 0, wird die Anzahl der Spalten erzwungen; sonst wird sie automatisch berechnet.")]
    public int gridColumnsOverride = 0;
    [Tooltip("Abstand in Y für 3D-Layouts.")]
    public float gridLayerSpacing = 4f;
    [Tooltip("Kantenlänge des 3D-Würfels. 4 ergibt ein sauberes 4x4x4-Grid mit 64 sichtbaren Elementen.")]
    public int cubeGridDimension = 4;
    [Tooltip("Größen-Voreinstellung für 3D-Würfel.")]
    public Cube3DSize cube3DSize = Cube3DSize.Size4x4x4;

    [Header("Boundary Visualization")]
    [Tooltip("Zeige visuelle Begrenzungspunkte des Grids an.")]
    public bool showBoundaryMarkers = true;
    [Tooltip("Padding um die Objekte herum, damit Marker nicht überlappen.")]
    public float boundaryMarkerPadding = 0.5f;
    [Tooltip("Länge der Kreuz-Achsen.")]
    public float boundaryMarkerCrossSize = 0.3f;
    [Tooltip("Dicke der Kreuz-Linien.")]
    public float boundaryMarkerLineWidth = 0.02f;

    [Header("Shape Settings")]
    [Tooltip("Weiß-transparente Form-Texturen (z.B. 512x512 PNGs).")]
    public Texture2D[] shapeTextures;

    [Header("Random Layout Settings")]
    public Vector2 randomRangeX = new Vector2(-4f, 4f);
    public Vector2 randomRangeY = new Vector2(-2f, 2f);
    public Vector2 randomRangeZ = new Vector2(-4f, 4f);

    [Header("Separation Settings")]
    [Tooltip("Aktivieren, um sich überlappende Kreise leicht auseinander zu schieben.")]
    public bool enableSeparation = true;
    [Tooltip("Zusätzlicher Abstand zwischen den Formen in Welt-Einheiten.")]
    public float separationPadding = 0.05f;
    [Tooltip("Anzahl der Iterationen für die Entzerrung.")]
    public int separationIterations = 5;

    [Header("Color Mode")]
    public ColorModeManager colorModeManager;

    [Header("Space Mode")]
    public SpaceMode spaceMode = SpaceMode.Grid2D;

    [Header("Filter Mode")]
    public bool sortBySize = false;
    public bool sortByShape = false;
    public bool sortByColor = false;
    public bool sortRandom = true;

    [Header("Filter Toggles (optional)")]
    public Toggle toggleSize;
    public Toggle toggleShape;
    public Toggle toggleColor;
    public Toggle toggleRandom;
    public Toggle toggleFaceCamera;

    [Header("Optional Controllers")]
    [Tooltip("Optionaler FloatingController, der das leichte Auf/Ab-Bewegen der Elemente steuert.")]
    public FloatingController floatingController;

    [Header("Selection Interaction")]
    public bool enableSelectionInteraction = true;
    [Range(0f, 1f)]
    public float nonSelectedAlpha = 0.3f;
    public CircleSelectionPopup selectionPopup;
    public GaussianSplatManager gaussianSplatManager;

    private CircleItem[] items;
    private List<Transform> spawnedCircles = new List<Transform>();
    private List<Quaternion> spawnedCircleBaseRotations = new List<Quaternion>();
    private List<Texture2D> activeShapeTextures = new List<Texture2D>();
    private List<Vector3> spaceSlots = new List<Vector3>();
    private List<Transform> boundaryMarkers = new List<Transform>();
    private List<BoundaryMarkerHandle> markerHandles = new List<BoundaryMarkerHandle>();
    private Vector3 gridCenterPosition = Vector3.zero;
    private List<int> cachedRandomIndices = new List<int>();
    private float[] cachedTieBreakers;
    private bool preserveDistributionOnNextApply = false;
    private int selectedItemIndex = -1;

    void Awake()
    {
        if (colorModeManager == null)
            colorModeManager = GetComponent<ColorModeManager>();

        if (selectionPopup == null)
            selectionPopup = FindObjectOfType<CircleSelectionPopup>(true);

        if (gaussianSplatManager == null)
            gaussianSplatManager = FindObjectOfType<GaussianSplatManager>(true);

        if (colorModeManager != null && colorModeManager.circleManager == null)
            colorModeManager.circleManager = this;
        GenerateItems();
    }

    void Start()
    {
        SpawnCircles();
        UpdateCubeGridDimensionFromPreset();
        RebuildSpaceSlots();
        GenerateBoundaryMarkers();
        SetBillboardsEnabled(false, true);
        if (toggleFaceCamera != null)
        {
            toggleFaceCamera.SetIsOnWithoutNotify(false);
            // Füge Listener hinzu, damit Toggle-Clicks registriert werden
            toggleFaceCamera.onValueChanged.AddListener(SetFaceCamera);
        }
        ApplyFilters();

        if (colorModeManager != null)
            colorModeManager.ApplyToAll();
    }

    void UpdateCubeGridDimensionFromPreset()
    {
        int cubeSize = 4;
        switch (cube3DSize)
        {
            case Cube3DSize.Size3x3x3:
                cubeSize = 3;
                break;
            case Cube3DSize.Size4x4x4:
                cubeSize = 4;
                break;
            case Cube3DSize.Size5x5x5:
                cubeSize = 5;
                break;
        }
        cubeGridDimension = cubeSize;
    }

    void GenerateBoundaryMarkers()
    {
        // Alte Marker aufräumen
        foreach (var marker in boundaryMarkers)
        {
            if (marker != null)
            {
                if (Application.isPlaying)
                    Destroy(marker.gameObject);
                else
                    DestroyImmediate(marker.gameObject);
            }
        }
        boundaryMarkers.Clear();
        markerHandles.Clear();

        if (!showBoundaryMarkers || spaceSlots == null || spaceSlots.Count == 0)
            return;

        // Bestimme die Ecken des Grids basierend auf Space-Slots
        Vector3 min = spaceSlots[0];
        Vector3 max = spaceSlots[0];

        foreach (var slot in spaceSlots)
        {
            min.x = Mathf.Min(min.x, slot.x);
            min.y = Mathf.Min(min.y, slot.y);
            min.z = Mathf.Min(min.z, slot.z);
            max.x = Mathf.Max(max.x, slot.x);
            max.y = Mathf.Max(max.y, slot.y);
            max.z = Mathf.Max(max.z, slot.z);
        }

        gridCenterPosition = (min + max) * 0.5f;

        // Padding um die größten Objekte herum
        float maxScale = baseScale * (1f + (2) * 0.7f); // S/M/L max scale
        float paddingOffset = (maxScale * 0.5f) + boundaryMarkerPadding;

        // 2D-Grid: 4 Ecken am Boden (Y = min.y)
        if (spaceMode == SpaceMode.Grid2D)
        {
            Vector3[] corners = new Vector3[]
            {
                new Vector3(min.x - paddingOffset, min.y, min.z - paddingOffset),
                new Vector3(max.x + paddingOffset, min.y, min.z - paddingOffset),
                new Vector3(max.x + paddingOffset, min.y, max.z + paddingOffset),
                new Vector3(min.x - paddingOffset, min.y, max.z + paddingOffset)
            };

            foreach (var corner in corners)
            {
                CreateBoundaryMarker(corner);
            }
        }
        // 3D-Würfel: 8 Ecken
        else if (spaceMode == SpaceMode.Space3D)
        {
            Vector3[] corners = new Vector3[]
            {
                new Vector3(min.x - paddingOffset, min.y - paddingOffset, min.z - paddingOffset),
                new Vector3(max.x + paddingOffset, min.y - paddingOffset, min.z - paddingOffset),
                new Vector3(max.x + paddingOffset, min.y - paddingOffset, max.z + paddingOffset),
                new Vector3(min.x - paddingOffset, min.y - paddingOffset, max.z + paddingOffset),
                new Vector3(min.x - paddingOffset, max.y + paddingOffset, min.z - paddingOffset),
                new Vector3(max.x + paddingOffset, max.y + paddingOffset, min.z - paddingOffset),
                new Vector3(max.x + paddingOffset, max.y + paddingOffset, max.z + paddingOffset),
                new Vector3(min.x - paddingOffset, max.y + paddingOffset, max.z + paddingOffset)
            };

            foreach (var corner in corners)
            {
                CreateBoundaryMarker(corner);
            }
        }
    }

    void CreateBoundaryMarker(Vector3 position)
    {
        // Neues GameObject für das 3D-Kreuz
        GameObject crossGO = new GameObject("BoundaryMarker_Cross");
        crossGO.transform.SetParent(transform);
        crossGO.transform.localPosition = position;

        // Erstelle drei dünne Quader für die Achsen (X, Y, Z)
        CreateCrossAxis(crossGO, "Axis_X", Vector3.right);    // X-Achse
        CreateCrossAxis(crossGO, "Axis_Y", Vector3.up);       // Y-Achse
        CreateCrossAxis(crossGO, "Axis_Z", Vector3.forward);  // Z-Achse

        // Große unsichtbare Greif-Zone um das Kreuz
        GameObject grabZone = new GameObject("GrabZone");
        grabZone.transform.SetParent(crossGO.transform);
        grabZone.transform.localPosition = Vector3.zero;
        var grabCollider = grabZone.AddComponent<BoxCollider>();
        float grabSize = Mathf.Max(0.8f, boundaryMarkerCrossSize * 3.0f);
        grabCollider.size = Vector3.one * grabSize;

        // Füge das Handle-Script für Interaktion hinzu
        BoundaryMarkerHandle handle = crossGO.AddComponent<BoundaryMarkerHandle>();
        handle.circleManager = this;
        handle.axisMask = (spaceMode == SpaceMode.Grid2D) ? 5 : 7; // 2D: XZ, 3D: XYZ
        markerHandles.Add(handle);

        boundaryMarkers.Add(crossGO.transform);
    }

    void CreateCrossAxis(GameObject parent, string axisName, Vector3 direction)
    {
        // Erstelle einen dünnen Quader (Cube) statt Cylinder
        GameObject axisGO = GameObject.CreatePrimitive(PrimitiveType.Cube);

        axisGO.name = axisName;
        axisGO.transform.SetParent(parent.transform);
        axisGO.transform.localPosition = Vector3.zero;
        axisGO.transform.localRotation = Quaternion.identity;

        // Skaliere je nach Richtung:
        // X-Achse: lang in X, dünn in Y/Z
        // Y-Achse: lang in Y, dünn in X/Z
        // Z-Achse: lang in Z, dünn in X/Y
        Vector3 scale = new Vector3(boundaryMarkerLineWidth, boundaryMarkerLineWidth, boundaryMarkerLineWidth);
        
        if (direction == Vector3.right)
            scale.x = boundaryMarkerCrossSize;
        else if (direction == Vector3.up)
            scale.y = boundaryMarkerCrossSize;
        else if (direction == Vector3.forward)
            scale.z = boundaryMarkerCrossSize;

        axisGO.transform.localScale = scale;

        // Schwarzes Material
        var renderer = axisGO.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material blackMat = new Material(Shader.Find("Standard"));
            blackMat.color = Color.black;
            renderer.material = blackMat;
        }

        // Größerer Pick-Bereich für einfaches Anklicken
        var box = axisGO.GetComponent<BoxCollider>();
        if (box != null)
        {
            float pickThickness = Mathf.Max(0.25f, boundaryMarkerCrossSize * 1.25f);
            float pickLength = Mathf.Max(boundaryMarkerCrossSize * 2.5f, 0.7f);

            if (direction == Vector3.right)
                box.size = new Vector3(pickLength, pickThickness, pickThickness);
            else if (direction == Vector3.up)
                box.size = new Vector3(pickThickness, pickLength, pickThickness);
            else
                box.size = new Vector3(pickThickness, pickThickness, pickLength);
        }
    }

    public void SetSpaceMode(SpaceMode mode)
    {
        if (spaceMode == mode)
            return;

        spaceMode = mode;
        UpdateCubeGridDimensionFromPreset();
        RebuildSpaceSlots();
        GenerateBoundaryMarkers();
        ApplyFilters();
    }

    void Update()
    {
        if (!Application.isPlaying)
            return;

        if (enableSelectionInteraction)
            HandleSelectionInput();
    }

    void HandleSelectionInput()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            CircleSelectable selectable = hit.collider.GetComponentInParent<CircleSelectable>();
            if (selectable != null && selectable.circleManager == this)
            {
                SelectItem(selectable.itemIndex);
                return;
            }
        }

        ClearSelectedItem();
    }

    void SpawnCircles()
    {
        // Sicherheits-Netz: alle bisherigen Kinder entfernen (falls Liste nicht mehr stimmt)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        spawnedCircles.Clear();
        spawnedCircleBaseRotations.Clear();

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var go = Instantiate(circlePrefab, Vector3.zero, Quaternion.identity, transform);
            spawnedCircles.Add(go.transform);
            spawnedCircleBaseRotations.Add(go.transform.localRotation);

            var selectable = go.GetComponent<CircleSelectable>();
            if (selectable == null)
                selectable = go.AddComponent<CircleSelectable>();
            selectable.circleManager = this;
            selectable.itemIndex = i;

            var collider = go.GetComponent<Collider>();
            if (collider == null)
                go.AddComponent<BoxCollider>();

            // Größe aus Kategorie ableiten (drei Stufen S,M,L)
            int sizeIndex = (int)item.size; // 0..2
            float scaleFactor = GetSizeScaleFactor(item);
            go.transform.localScale = Vector3.one * baseScale * scaleFactor;

            // Form-Textur anhand shapeIndex zuweisen
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Texture2D tex = null;
                if (activeShapeTextures != null && activeShapeTextures.Count > 0)
                {
                    int si = Mathf.Clamp(item.shapeIndex, 0, activeShapeTextures.Count - 1);
                    tex = activeShapeTextures[si];
                }

                Color c = colorModeManager != null ? colorModeManager.GetColor(item.color) : Color.white;

                // Im Play-Mode eigenes Instanz-Material, im Edit-Mode sharedMaterial, um Leaks zu vermeiden
                if (Application.isPlaying)
                {
                    var mat = renderer.material;
                    if (tex != null) mat.mainTexture = tex;
                    mat.color = c;
                }
                else if (renderer.sharedMaterial != null)
                {
                    if (tex != null) renderer.sharedMaterial.mainTexture = tex;
                    renderer.sharedMaterial.color = c;
                }
            }
        }

        if (floatingController != null)
        {
            floatingController.parentTransform = transform;
            floatingController.SetTargets(spawnedCircles);
            floatingController.CaptureBasePositions();
        }

        if (colorModeManager != null)
            colorModeManager.ApplyToAll();
    }

    void GenerateItems()
    {
        BuildActiveShapeList();
        // Systematische Kombination: jede Shape × 3 Größen × 3 Farben
        int availableShapes = (activeShapeTextures != null && activeShapeTextures.Count > 0) ? activeShapeTextures.Count : 1;
        int shapeCount = Mathf.Min(availableShapes, 9);

        List<CircleItem> list = new List<CircleItem>();

        CircleSizeCategory[] sizes = { CircleSizeCategory.S, CircleSizeCategory.M, CircleSizeCategory.L };
        ColorCategory[] colors = { ColorCategory.Orange, ColorCategory.Violet, ColorCategory.Red };

        // Codes für Shapes (basierend auf deiner Benennung 01–09)
        string[] familyCodes = { "SQ", "CI", "TR" }; // Square, Circle, Triangle
        char[] variantLetters = { 'A', 'B', 'C' };

        for (int shapeIndex = 0; shapeIndex < shapeCount; shapeIndex++)
        {
            int familyIndex = Mathf.Clamp(shapeIndex / 3, 0, familyCodes.Length - 1);
            int variantIndex = shapeIndex % 3;

            string famCode = familyCodes[familyIndex];
            char varLetter = variantLetters[variantIndex];

            foreach (var size in sizes)
            {
                foreach (var color in colors)
                {
                    string sizeCode = size == CircleSizeCategory.S ? "S" : size == CircleSizeCategory.M ? "M" : "L";
                    string colorCode = color == ColorCategory.Orange ? "O" : color == ColorCategory.Violet ? "V" : "R";

                    string id = $"{famCode}-{varLetter}_{sizeCode}_{colorCode}";
                    list.Add(new CircleItem(id, size, color, shapeIndex));
                }
            }
        }

        items = list.ToArray();
    }

    void BuildActiveShapeList()
    {
        activeShapeTextures.Clear();
        if (shapeTextures == null || shapeTextures.Length == 0)
            return;

        for (int i = 0; i < shapeTextures.Length; i++)
        {
            if (shapeTextures[i] != null)
                activeShapeTextures.Add(shapeTextures[i]);
        }
    }

    [ContextMenu("Rebuild 2D Elements")]
    public void Rebuild()
    {
        GenerateItems();
        SpawnCircles();
        ApplyLayoutBySize();
    }

    // Layout 1: nach Größe (Size) – Spalten für XS, S, M, L, XL
    public void ApplyLayoutBySize()
    {
        if (items == null || items.Length == 0) return;
        SetSortBySize(true);
    }

    // Layout 2: nach Farb-Kategorie – drei Spalten (eine pro Farbe), gleiche Achsen wie Size-Layout
    public void ApplyLayoutByColor()
    {
        if (items == null || items.Length == 0) return;
        SetSortByColor(true);
    }

    // Layout: nach Shape-Typ (SQ, CI, TR)
    public void ApplyLayoutByShape()
    {
        if (items == null || items.Length == 0) return;
        SetSortByShape(true);
    }

    // Layout 3: Kombination aus Größe (Z) und Farbe (X)
    public void ApplyLayoutBySizeAndColor()
    {
        sortBySize = true;
        sortByColor = true;
        sortByShape = false;
        sortRandom = false;
        ApplyFilters();
    }

    // Layout 4: komplett zufällige Position im Raum
    public void ApplyRandomLayout()
    {
        if (items == null || items.Length == 0) return;
        sortBySize = false;
        sortByColor = false;
        sortByShape = false;
        sortRandom = true;
        ApplyFilters();
    }

    public void RebuildSpaceSlots()
    {
        if (items == null || items.Length == 0) return;

        spaceSlots.Clear();

        if (spaceMode == SpaceMode.Grid2D)
        {
            BuildGridSlots2D();
        }
        else
        {
            BuildCubeSlots3D(Get3DVisibleCount());
        }
    }

    void BuildGridSlots2D()
    {
        int count = items.Length;
        int columns = gridColumnsOverride > 0 ? gridColumnsOverride : Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt((float)count / columns);

        // 2D skaliert direkt über die Marker-Ranges
        float minX = randomRangeX.x;
        float maxX = randomRangeX.y;
        float minZ = randomRangeZ.x;
        float maxZ = randomRangeZ.y;

        // Fallback, falls Ranges unbrauchbar sind
        if (Mathf.Abs(maxX - minX) < 0.0001f || Mathf.Abs(maxZ - minZ) < 0.0001f)
        {
            float cell = useUniformGridSpacing ? uniformGridCellSize : baseScale;
            float stepX = cell * sizeColumnSpacing;
            float stepZ = cell * rowSpacing;
            float width = (columns - 1) * stepX;
            float depth = (rows - 1) * stepZ;

            minX = -width * 0.5f;
            maxX = width * 0.5f;
            minZ = -depth * 0.5f;
            maxZ = depth * 0.5f;
        }

        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            int col = i % columns;

            float x = columns > 1 ? Mathf.Lerp(minX, maxX, (float)col / (columns - 1)) : (minX + maxX) * 0.5f;
            float z = rows > 1 ? Mathf.Lerp(minZ, maxZ, (float)row / (rows - 1)) : (minZ + maxZ) * 0.5f;
            spaceSlots.Add(new Vector3(x, 0f, z));
        }

        // Für klare Filter-Lesbarkeit: Slots links -> rechts, dann vorne -> hinten
        spaceSlots.Sort((a, b) =>
        {
            int c = a.x.CompareTo(b.x);
            if (c != 0) return c;
            c = a.z.CompareTo(b.z);
            if (c != 0) return c;
            return a.y.CompareTo(b.y);
        });
    }

    void BuildCubeSlots3D(int count)
    {
        // Geordnetes 3D-Grid innerhalb der Random-Range, damit es sichtbar 3D bleibt
        count = Mathf.Max(1, count);

        int cubeSize = Mathf.Max(1, cubeGridDimension);
        int cols = cubeSize;
        int rows = cols;
        int layers = cubeSize;

        float minX = randomRangeX.x;
        float maxX = randomRangeX.y;
        float minY = randomRangeY.x;
        float maxY = randomRangeY.y;
        float minZ = randomRangeZ.x;
        float maxZ = randomRangeZ.y;

        float stepX = cols > 1 ? (maxX - minX) / (cols - 1) : 0f;
        float stepY = layers > 1 ? (maxY - minY) / (layers - 1) : 0f;
        float stepZ = rows > 1 ? (maxZ - minZ) / (rows - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            int layer = i / (cols * rows);
            int indexInLayer = i % (cols * rows);
            int row = indexInLayer / cols;
            int col = indexInLayer % cols;

            float x = minX + col * stepX;
            float y = minY + layer * stepY;
            float z = minZ + row * stepZ;

            spaceSlots.Add(new Vector3(x, y, z));
        }

        // Für klare Sortierung: Slots von links nach rechts (X), dann vorne nach hinten (Z), dann Y
        spaceSlots.Sort((a, b) =>
        {
            int c = a.x.CompareTo(b.x);
            if (c != 0) return c;
            c = a.z.CompareTo(b.z);
            if (c != 0) return c;
            return a.y.CompareTo(b.y);
        });
    }

    public void SetSortBySize(bool isOn)
    {
        sortBySize = isOn;

        SyncRandomStateToFilters();
        InvalidateDistributionCaches();
        ApplyFilters();
    }

    public void SetSortByColor(bool isOn)
    {
        sortByColor = isOn;

        SyncRandomStateToFilters();
        InvalidateDistributionCaches();
        ApplyFilters();
    }

    public void SetSortByShape(bool isOn)
    {
        sortByShape = isOn;

        SyncRandomStateToFilters();
        InvalidateDistributionCaches();
        ApplyFilters();
    }

    public void SetSortRandom(bool isOn)
    {
        if (!isOn && !AnyStructuredFiltersActive())
        {
            sortRandom = true;

            if (toggleRandom != null)
                toggleRandom.SetIsOnWithoutNotify(true);

            ApplyFilters();
            return;
        }

        sortRandom = isOn;
        if (isOn)
        {
            sortBySize = false;
            sortByColor = false;
            sortByShape = false;

            if (toggleSize != null) toggleSize.SetIsOnWithoutNotify(false);
            if (toggleColor != null) toggleColor.SetIsOnWithoutNotify(false);
            if (toggleShape != null) toggleShape.SetIsOnWithoutNotify(false);
        }
        InvalidateDistributionCaches();
        ApplyFilters();
    }

    public void ApplyRandomAndClearFilters()
    {
        sortRandom = true;
        sortBySize = false;
        sortByColor = false;
        sortByShape = false;

        if (toggleSize != null) toggleSize.SetIsOnWithoutNotify(false);
        if (toggleShape != null) toggleShape.SetIsOnWithoutNotify(false);
        if (toggleColor != null) toggleColor.SetIsOnWithoutNotify(false);
        if (toggleRandom != null) toggleRandom.SetIsOnWithoutNotify(true);

        InvalidateDistributionCaches();
        ApplyFilters();
    }

    void ApplyFilters()
    {
        if (spawnedCircles == null || spawnedCircles.Count != items.Length)
        {
            SpawnCircles();
            RebuildSpaceSlots();
        }

        EnsureSpaceSlots();

        if (!sortBySize && !sortByColor && !sortByShape && !sortRandom)
            return;

        List<int> indices = new List<int>(items.Length);
        for (int i = 0; i < items.Length; i++) indices.Add(i);

        int visibleCount = GetVisibleItemCount();

        if (cachedTieBreakers == null || cachedTieBreakers.Length != items.Length)
        {
            cachedTieBreakers = new float[items.Length];
            for (int i = 0; i < cachedTieBreakers.Length; i++)
                cachedTieBreakers[i] = Random.value;
        }

        if (sortRandom)
        {
            if (preserveDistributionOnNextApply && cachedRandomIndices != null && cachedRandomIndices.Count == items.Length)
            {
                indices.Clear();
                indices.AddRange(cachedRandomIndices);
            }
            else
            {
                for (int i = 0; i < indices.Count; i++)
                {
                    int swapIndex = Random.Range(i, indices.Count);
                    (indices[i], indices[swapIndex]) = (indices[swapIndex], indices[i]);
                }

                cachedRandomIndices.Clear();
                cachedRandomIndices.AddRange(indices);
            }
        }
        else if (sortBySize || sortByColor || sortByShape)
        {
            if (!preserveDistributionOnNextApply)
            {
                for (int i = 0; i < cachedTieBreakers.Length; i++)
                    cachedTieBreakers[i] = Random.value;
            }

            // Ordnung: aktive Filter links->rechts (Size -> Color -> Shape).
            // Inaktive Eigenschaften bleiben ungerichtet (zufälliger Tie-Breaker).
            indices.Sort((a, b) =>
            {
                int result;

                // Active first
                if (sortBySize)
                {
                    result = GetSizeSortValue(items[a]).CompareTo(GetSizeSortValue(items[b]));
                    if (result != 0) return result;
                }
                if (sortByColor)
                {
                    result = GetColorSortKey(items[a].color).CompareTo(GetColorSortKey(items[b].color));
                    if (result != 0) return result;
                }
                if (sortByShape)
                {
                    result = GetShapeSortKey(items[a].shapeIndex).CompareTo(GetShapeSortKey(items[b].shapeIndex));
                    if (result != 0) return result;
                }

                // Keine weitere inhaltliche Sortierung: Rest zufällig lassen.
                return cachedTieBreakers[a].CompareTo(cachedTieBreakers[b]);
            });
        }

        AssignToSlots(indices, visibleCount);
        preserveDistributionOnNextApply = false;
    }

    // Hinweis: Die Filter-Toggles sollen nicht in einer ToggleGroup sein.

    float GetSizeScaleFactor(CircleItem item)
    {
        int sizeIndex = (int)item.size; // 0..2
        float baseFactor = 1f + sizeIndex * 0.7f;

        int steps = Mathf.Max(1, sizeVariationSteps);
        if (steps <= 1 || sizeVariationSpread <= 0f)
            return baseFactor;

        int variationIndex = (item.shapeIndex + (int)item.color) % steps;
        float t = steps == 1 ? 0f : (float)variationIndex / (steps - 1);
        float variation = (t - 0.5f) * sizeVariationSpread;
        return baseFactor + variation;
    }

    float GetSizeSortValue(CircleItem item)
    {
        // Sortiere strikt nach Kategorie S/M/L (nicht nach Variation)
        return (int)item.size;
    }

    int Get3DVisibleCount()
    {
        int cubeSize = Mathf.Max(1, cubeGridDimension);
        return Mathf.Min(items.Length, cubeSize * cubeSize * cubeSize);
    }

    int GetVisibleItemCount()
    {
        if (spaceMode == SpaceMode.Space3D)
            return Get3DVisibleCount();

        return items != null ? items.Length : 0;
    }

    void SyncRandomStateToFilters()
    {
        sortRandom = !AnyStructuredFiltersActive();

        if (toggleRandom != null)
            toggleRandom.SetIsOnWithoutNotify(sortRandom);
    }

    bool AnyStructuredFiltersActive()
    {
        return sortBySize || sortByColor || sortByShape;
    }

    public void OnMarkerMoved(BoundaryMarkerHandle movedMarker)
    {
        if (boundaryMarkers == null || boundaryMarkers.Count == 0)
            return;

        // Nur die aktiv gezogene Achse anpassen (damit Verkleinern möglich ist)
        int movedAxisMask = movedMarker != null ? movedMarker.GetCurrentDragAxisMask() : 0;
        Vector3 movedRelPos = movedMarker != null
            ? movedMarker.transform.localPosition - gridCenterPosition
            : Vector3.zero;

        float paddingOffset = GetBoundaryPaddingOffset();

        float halfX = Mathf.Max(0.2f, Mathf.Max(Mathf.Abs(randomRangeX.x), Mathf.Abs(randomRangeX.y)));
        float halfY = Mathf.Max(0.2f, Mathf.Max(Mathf.Abs(randomRangeY.x), Mathf.Abs(randomRangeY.y)));
        float halfZ = Mathf.Max(0.2f, Mathf.Max(Mathf.Abs(randomRangeZ.x), Mathf.Abs(randomRangeZ.y)));

        if ((movedAxisMask & 1) != 0)
            halfX = Mathf.Max(0.2f, Mathf.Abs(movedRelPos.x) - paddingOffset);
        if ((movedAxisMask & 2) != 0 && spaceMode == SpaceMode.Space3D)
            halfY = Mathf.Max(0.2f, Mathf.Abs(movedRelPos.y) - paddingOffset);
        if ((movedAxisMask & 4) != 0)
            halfZ = Mathf.Max(0.2f, Mathf.Abs(movedRelPos.z) - paddingOffset);

        // Aktualisiere die Random-Ranges basierend auf Marker-Positionen
        randomRangeX = new Vector2(-halfX, halfX);
        randomRangeZ = new Vector2(-halfZ, halfZ);
        if (spaceMode == SpaceMode.Space3D)
            randomRangeY = new Vector2(-halfY, halfY);

        // Baue das Grid neu
        preserveDistributionOnNextApply = true;
        RebuildSpaceSlots();
        UpdateBoundaryMarkersFromRanges();
        ApplyFilters();
    }

    void InvalidateDistributionCaches()
    {
        cachedRandomIndices.Clear();
        if (cachedTieBreakers != null)
        {
            for (int i = 0; i < cachedTieBreakers.Length; i++)
                cachedTieBreakers[i] = Random.value;
        }
    }

    float GetBoundaryPaddingOffset()
    {
        float maxScale = baseScale * (1f + 2f * 0.7f); // S/M/L max scale
        return (maxScale * 0.5f) + boundaryMarkerPadding;
    }

    void UpdateBoundaryMarkersFromRanges()
    {
        if (!showBoundaryMarkers || boundaryMarkers == null)
            return;

        float p = GetBoundaryPaddingOffset();

        float xMin = gridCenterPosition.x + randomRangeX.x - p;
        float xMax = gridCenterPosition.x + randomRangeX.y + p;
        float zMin = gridCenterPosition.z + randomRangeZ.x - p;
        float zMax = gridCenterPosition.z + randomRangeZ.y + p;

        if (spaceMode == SpaceMode.Grid2D && boundaryMarkers.Count >= 4)
        {
            boundaryMarkers[0].localPosition = new Vector3(xMin, gridCenterPosition.y, zMin);
            boundaryMarkers[1].localPosition = new Vector3(xMax, gridCenterPosition.y, zMin);
            boundaryMarkers[2].localPosition = new Vector3(xMax, gridCenterPosition.y, zMax);
            boundaryMarkers[3].localPosition = new Vector3(xMin, gridCenterPosition.y, zMax);
            return;
        }

        float yMin = gridCenterPosition.y + randomRangeY.x - p;
        float yMax = gridCenterPosition.y + randomRangeY.y + p;

        if (spaceMode == SpaceMode.Space3D && boundaryMarkers.Count >= 8)
        {
            boundaryMarkers[0].localPosition = new Vector3(xMin, yMin, zMin);
            boundaryMarkers[1].localPosition = new Vector3(xMax, yMin, zMin);
            boundaryMarkers[2].localPosition = new Vector3(xMax, yMin, zMax);
            boundaryMarkers[3].localPosition = new Vector3(xMin, yMin, zMax);
            boundaryMarkers[4].localPosition = new Vector3(xMin, yMax, zMin);
            boundaryMarkers[5].localPosition = new Vector3(xMax, yMax, zMin);
            boundaryMarkers[6].localPosition = new Vector3(xMax, yMax, zMax);
            boundaryMarkers[7].localPosition = new Vector3(xMin, yMax, zMax);
        }
    }

    void EnsureSpaceSlots()
    {
        if (spaceSlots == null || spaceSlots.Count != items.Length)
            RebuildSpaceSlots();
    }

    void AssignToSlots(List<int> indices, int visibleCount)
    {
        int count = Mathf.Min(indices.Count, spaceSlots.Count, visibleCount);
        for (int i = 0; i < count; i++)
        {
            int itemIndex = indices[i];
            if (!spawnedCircles[itemIndex].gameObject.activeSelf)
                spawnedCircles[itemIndex].gameObject.SetActive(true);
            spawnedCircles[itemIndex].localPosition = spaceSlots[i];
        }

        for (int i = count; i < indices.Count; i++)
        {
            int itemIndex = indices[i];
            if (spawnedCircles[itemIndex].gameObject.activeSelf)
                spawnedCircles[itemIndex].gameObject.SetActive(false);
        }

        if (enableSeparation)
            ApplySeparation(items.Length);

        if (floatingController != null)
            floatingController.CaptureBasePositions();
    }

    int GetShapeGroup(int shapeIndex)
    {
        // 3 Formen, 3 Varianten je Form → 0..2
        return Mathf.Clamp(shapeIndex / 3, 0, 2);
    }

    int GetShapeSortKey(int shapeIndex)
    {
        // Wunsch-Reihenfolge: Circle -> Triangle -> Square
        int group = GetShapeGroup(shapeIndex); // 0: SQ, 1: CI, 2: TR
        switch (group)
        {
            case 1: // Circle
                return 0;
            case 2: // Triangle
                return 1;
            case 0: // Square
            default:
                return 2;
        }
    }

    float GetColorSortKey(ColorCategory color)
    {
        if (colorModeManager != null)
        {
            Color c = colorModeManager.GetBaseColor(color);
            // Helligkeit für links->rechts: hell -> dunkel
            return c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
        }

        // Fallback: Violet -> Orange -> Red
        switch (color)
        {
            case ColorCategory.Violet:
                return 0f;
            case ColorCategory.Orange:
                return 0.5f;
            case ColorCategory.Red:
            default:
                return 1f;
        }
    }

    void ApplyLayoutBySize3D()
    {
        int sizeCount = System.Enum.GetValues(typeof(CircleSizeCategory)).Length; // 3
        int[] groupCounters = new int[sizeCount];

        GetGridSteps(sizeCount, out float columnStep, out float rowStep);
        float layerStep = useUniformGridSpacing ? uniformGridCellSize * gridLayerSpacing : baseScale * gridLayerSpacing;

        List<int> indices = new List<int>(items.Length);
        for (int i = 0; i < items.Length; i++) indices.Add(i);
        for (int i = 0; i < indices.Count; i++)
        {
            int swapIndex = Random.Range(i, indices.Count);
            (indices[i], indices[swapIndex]) = (indices[swapIndex], indices[i]);
        }

        int maxPerColumn = Mathf.Max(1, maxItemsPerColumn);

        foreach (int itemIndex in indices)
        {
            var item = items[itemIndex];
            int sizeIndex = (int)item.size;

            int indexInGroup = groupCounters[sizeIndex]++;
            int depthIndex = indexInGroup % maxPerColumn;
            int layerIndex = (indexInGroup / maxPerColumn) % maxPerColumn;
            int sideIndex = indexInGroup / (maxPerColumn * maxPerColumn);

            float baseX = (sizeIndex - (sizeCount - 1) * 0.5f) * columnStep;
            float x = baseX + sideIndex * columnStep * 0.5f;
            float y = layerIndex * layerStep;
            float z = depthIndex * rowStep;

            spawnedCircles[itemIndex].localPosition = new Vector3(x, y, z);
        }

        if (enableSeparation)
            ApplySeparation(items.Length);

        if (floatingController != null)
            floatingController.CaptureBasePositions();
    }

    void ApplyLayoutByColor3D()
    {
        int colorCount = 3;
        int[] groupCounters = new int[colorCount];

        GetGridSteps(System.Enum.GetValues(typeof(CircleSizeCategory)).Length, out float columnStep, out float rowStep);
        float layerStep = useUniformGridSpacing ? uniformGridCellSize * gridLayerSpacing : baseScale * gridLayerSpacing;

        List<int> indices = new List<int>(items.Length);
        for (int i = 0; i < items.Length; i++) indices.Add(i);
        for (int i = 0; i < indices.Count; i++)
        {
            int swapIndex = Random.Range(i, indices.Count);
            (indices[i], indices[swapIndex]) = (indices[swapIndex], indices[i]);
        }

        int maxPerColumn = Mathf.Max(1, maxItemsPerColumn);

        foreach (int itemIndex in indices)
        {
            var item = items[itemIndex];
            int colorIndex = (int)item.color;

            int indexInGroup = groupCounters[colorIndex]++;
            int depthIndex = indexInGroup % maxPerColumn;
            int layerIndex = (indexInGroup / maxPerColumn) % maxPerColumn;
            int sideIndex = indexInGroup / (maxPerColumn * maxPerColumn);

            float baseX = (colorIndex - (colorCount - 1) * 0.5f) * columnStep;
            float x = baseX + sideIndex * columnStep * 0.5f;
            float y = layerIndex * layerStep;
            float z = depthIndex * rowStep;

            spawnedCircles[itemIndex].localPosition = new Vector3(x, y, z);
        }

        if (enableSeparation)
            ApplySeparation(items.Length);

        if (floatingController != null)
            floatingController.CaptureBasePositions();
    }

    void GetGridSteps(int sizeCount, out float columnStep, out float rowStep)
    {
        if (useUniformGridSpacing)
        {
            columnStep = uniformGridCellSize * sizeColumnSpacing;
            rowStep = uniformGridCellSize * rowSpacing;
            return;
        }

        float maxScaleFactor = 1f + (sizeCount - 1) * 0.7f;
        float cellSize = baseScale * maxScaleFactor;
        columnStep = cellSize * sizeColumnSpacing;
        rowStep = cellSize * rowSpacing;
    }

    /// <summary>
    /// Schiebt sich überlappende Kreise in kleinen Schritten auseinander,
    /// damit sie sich nicht überdecken. Einfaches O(n^2)-Relaxationsverfahren.
    /// </summary>
    void ApplySeparation(int visibleCount)
    {
        if (spawnedCircles == null || spawnedCircles.Count == 0)
            return;

        int count = Mathf.Min(visibleCount, spawnedCircles.Count, spaceSlots.Count);

        for (int iter = 0; iter < Mathf.Max(1, separationIterations); iter++)
        {
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    Vector3 a = spawnedCircles[i].localPosition;
                    Vector3 b = spawnedCircles[j].localPosition;

                    Vector3 diff = a - b;
                    float dist = diff.magnitude;
                    if (dist <= 0.0001f)
                    {
                        // zufällige kleine Verschiebung, falls exakt gleich
                        diff = new Vector3(Random.value, 0f, Random.value).normalized * 0.01f;
                        dist = diff.magnitude;
                    }

                    // effektive "Radien" aus aktueller Skalierung ableiten (wir gehen von 1x1-Quad aus)
                    float radiusA = spawnedCircles[i].localScale.x * 0.5f;
                    float radiusB = spawnedCircles[j].localScale.x * 0.5f;
                    float desiredDist = Mathf.Max(0.0001f, radiusA + radiusB + separationPadding);

                    if (dist < desiredDist)
                    {
                        float push = (desiredDist - dist) * 0.5f;
                        Vector3 dir = diff.normalized;
                        a += dir * push;
                        b -= dir * push;
                        spawnedCircles[i].localPosition = a;
                        spawnedCircles[j].localPosition = b;
                    }
                }
            }
        }
    }

    public void ApplyColorsFromManager(ColorModeManager manager)
    {
        if (manager == null || items == null || spawnedCircles == null || spawnedCircles.Count == 0)
            return;

        for (int i = 0; i < spawnedCircles.Count; i++)
        {
            var renderer = spawnedCircles[i].GetComponent<Renderer>();
            if (renderer == null) continue;

            Color c = manager.GetColor(items[i].color);
            bool isNonSelected = enableSelectionInteraction && selectedItemIndex >= 0 && i != selectedItemIndex;

            if (isNonSelected)
            {
                float g = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
                c = new Color(g, g, g, Mathf.Clamp01(nonSelectedAlpha));
            }
            else
            {
                c.a = 1f;
            }

            if (Application.isPlaying)
            {
                var mat = renderer.material;
                ConfigureMaterialForTransparency(mat);
                mat.color = c;
            }
            else if (renderer.sharedMaterial != null)
            {
                renderer.sharedMaterial.color = c;
            }
        }
    }

    void ConfigureMaterialForTransparency(Material mat)
    {
        if (mat == null || !mat.HasProperty("_Mode"))
            return;

        // Standard Shader: Fade Mode
        mat.SetFloat("_Mode", 2f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    public void SelectItem(int itemIndex)
    {
        if (items == null || itemIndex < 0 || itemIndex >= items.Length)
            return;

        selectedItemIndex = itemIndex;

        if (colorModeManager != null)
            ApplyColorsFromManager(colorModeManager);

        Transform selectedTransform = itemIndex < spawnedCircles.Count ? spawnedCircles[itemIndex] : null;
        string titleFromTexture = GetTextureTitleForItem(itemIndex);

        if (selectionPopup != null)
        {
            selectionPopup.Show(items[itemIndex], selectedTransform, titleFromTexture);
        }

        if (gaussianSplatManager != null)
            gaussianSplatManager.HandleItemSelected(items[itemIndex], titleFromTexture);
    }

    string GetTextureTitleForItem(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= spawnedCircles.Count)
            return null;

        var r = spawnedCircles[itemIndex].GetComponent<Renderer>();
        if (r == null || r.material == null || r.material.mainTexture == null)
            return null;

        string texName = r.material.mainTexture.name;
        if (string.IsNullOrEmpty(texName))
            return null;

        if (!texName.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            texName += ".png";

        return texName;
    }

    public void ClearSelectedItem()
    {
        if (selectedItemIndex < 0)
            return;

        selectedItemIndex = -1;

        if (colorModeManager != null)
            ApplyColorsFromManager(colorModeManager);

        if (selectionPopup != null)
            selectionPopup.Hide();
    }

    // Toggle: Face Camera (an = mitdrehen)
    public void SetFaceCamera(bool isOn)
    {
        // Sync Toggle UI
        if (toggleFaceCamera != null)
            toggleFaceCamera.SetIsOnWithoutNotify(isOn);

        // Steuere Billboards
        SetBillboardsEnabled(isOn, true);
    }

    // Zusätzliche Methode für externe Face Camera Kontrolle
    public void ToggleFaceCamera()
    {
        bool currentState = toggleFaceCamera != null ? toggleFaceCamera.isOn : false;
        SetFaceCamera(!currentState);
    }

    // Toggle: Freeze (an = keine Billboard-Ausrichtung)
    public void SetFreeze(bool isOn)
    {
        if (isOn)
            SetBillboardsEnabled(false, true);
    }

    void SetBillboardsEnabled(bool enabled, bool resetRotationsWhenDisabled = false)
    {
        for (int i = 0; i < spawnedCircles.Count; i++)
        {
            var bb = spawnedCircles[i].GetComponent<Billboard>();
            if (bb != null)
                bb.enableBillboard = enabled;

            if (!enabled && resetRotationsWhenDisabled)
            {
                if (i < spawnedCircleBaseRotations.Count)
                    spawnedCircles[i].localRotation = spawnedCircleBaseRotations[i];
            }
        }
    }
}