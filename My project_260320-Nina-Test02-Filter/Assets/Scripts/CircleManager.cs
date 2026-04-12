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

    [Header("Setup")]
    public GameObject circlePrefab;

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

    [Header("Optional Controllers")]
    [Tooltip("Optionaler FloatingController, der das leichte Auf/Ab-Bewegen der Elemente steuert.")]
    public FloatingController floatingController;

    private CircleItem[] items;
    private List<Transform> spawnedCircles = new List<Transform>();
    private List<Texture2D> activeShapeTextures = new List<Texture2D>();
    private List<Vector3> spaceSlots = new List<Vector3>();

    void Awake()
    {
        if (colorModeManager == null)
            colorModeManager = GetComponent<ColorModeManager>();

        if (colorModeManager != null && colorModeManager.circleManager == null)
            colorModeManager.circleManager = this;
        GenerateItems();
    }

    void Start()
    {
        SpawnCircles();
        RebuildSpaceSlots();
        ApplyFilters();

        if (colorModeManager != null)
            colorModeManager.ApplyToAll();

    }

    public void SetSpaceMode(SpaceMode mode)
    {
        if (spaceMode == mode)
            return;

        spaceMode = mode;
        RebuildSpaceSlots();
        ApplyFilters();
    }

    void Update()
    {
        if (!Application.isPlaying)
            return;
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

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var go = Instantiate(circlePrefab, Vector3.zero, Quaternion.identity, transform);
            spawnedCircles.Add(go.transform);

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
        sortBySize = true;
        sortByColor = false;
        sortRandom = false;
        ApplyFilters();
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
        sortRandom = false;
        ApplyFilters();
    }

    // Layout 4: komplett zufällige Position im Raum
    public void ApplyRandomLayout()
    {
        if (items == null || items.Length == 0) return;
        sortBySize = false;
        sortByColor = false;
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
            BuildRandomSlots3D();
        }
    }

    void BuildGridSlots2D()
    {
        int count = items.Length;
        int columns = gridColumnsOverride > 0 ? gridColumnsOverride : Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt((float)count / columns);

        float cell = useUniformGridSpacing ? uniformGridCellSize : baseScale;
        float stepX = cell * sizeColumnSpacing;
        float stepZ = cell * rowSpacing;

        float width = (columns - 1) * stepX;
        float depth = (rows - 1) * stepZ;

        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            int col = i % columns;

            float x = col * stepX - width * 0.5f;
            float z = row * stepZ - depth * 0.5f;
            spaceSlots.Add(new Vector3(x, 0f, z));
        }
    }

    void BuildRandomSlots3D()
    {
        // Geordnetes 3D-Grid innerhalb der Random-Range, damit es sichtbar 3D bleibt
        int count = items.Length;

        int cols = Mathf.CeilToInt(Mathf.Pow(count, 1f / 3f));
        int rows = cols;
        int layers = Mathf.CeilToInt((float)count / (cols * rows));
        layers = Mathf.Max(1, layers);

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
        if (isOn)
        {
            sortByColor = false;
            sortByShape = false;
            sortRandom = false;
        }
        else
        {
            sortRandom = true;
        }
        ApplyFilters();
    }

    public void SetSortByColor(bool isOn)
    {
        sortByColor = isOn;
        if (isOn)
        {
            sortBySize = false;
            sortByShape = false;
            sortRandom = false;
        }
        else
        {
            sortRandom = true;
        }
        ApplyFilters();
    }

    public void SetSortByShape(bool isOn)
    {
        sortByShape = isOn;
        if (isOn)
        {
            sortBySize = false;
            sortByColor = false;
            sortRandom = false;
        }
        else
        {
            sortRandom = true;
        }
        ApplyFilters();
    }

    public void SetSortRandom(bool isOn)
    {
        sortRandom = isOn;
        if (isOn)
        {
            sortBySize = false;
            sortByColor = false;
            sortByShape = false;
        }
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

        if (sortRandom)
        {
            for (int i = 0; i < indices.Count; i++)
            {
                int swapIndex = Random.Range(i, indices.Count);
                (indices[i], indices[swapIndex]) = (indices[swapIndex], indices[i]);
            }
        }
        else if (sortBySize || sortByColor || sortByShape)
        {
            // Ordnung: aktive Filter zuerst (Size -> Shape -> Color),
            // danach die inaktiven als stabile Tie-Breaker.
            indices.Sort((a, b) =>
            {
                int result;

                // Active first
                if (sortBySize)
                {
                    result = GetSizeSortValue(items[a]).CompareTo(GetSizeSortValue(items[b]));
                    if (result != 0) return result;
                }
                if (sortByShape)
                {
                    result = GetShapeSortKey(items[a].shapeIndex).CompareTo(GetShapeSortKey(items[b].shapeIndex));
                    if (result != 0) return result;
                }
                if (sortByColor)
                {
                    result = GetColorSortKey(items[a].color).CompareTo(GetColorSortKey(items[b].color));
                    if (result != 0) return result;
                }

                // Feste sekundäre Ordnung für Klarheit: Size -> Shape -> Color
                result = GetSizeSortValue(items[a]).CompareTo(GetSizeSortValue(items[b]));
                if (result != 0) return result;

                result = GetShapeSortKey(items[a].shapeIndex).CompareTo(GetShapeSortKey(items[b].shapeIndex));
                if (result != 0) return result;

                result = GetColorSortKey(items[a].color).CompareTo(GetColorSortKey(items[b].color));
                if (result != 0) return result;

                return items[a].shapeIndex.CompareTo(items[b].shapeIndex);
            });
        }

        AssignToSlots(indices);
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

    void EnsureSpaceSlots()
    {
        if (spaceSlots == null || spaceSlots.Count != items.Length)
            RebuildSpaceSlots();
    }

    void AssignToSlots(List<int> indices)
    {
        int count = Mathf.Min(indices.Count, spaceSlots.Count);
        for (int i = 0; i < count; i++)
        {
            int itemIndex = indices[i];
            spawnedCircles[itemIndex].localPosition = spaceSlots[i];
        }

        if (enableSeparation)
            ApplySeparation();

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
            ApplySeparation();

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
            ApplySeparation();

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
    void ApplySeparation()
    {
        if (spawnedCircles == null || spawnedCircles.Count == 0)
            return;

        for (int iter = 0; iter < Mathf.Max(1, separationIterations); iter++)
        {
            for (int i = 0; i < spawnedCircles.Count; i++)
            {
                for (int j = i + 1; j < spawnedCircles.Count; j++)
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

            if (Application.isPlaying)
            {
                var mat = renderer.material;
                mat.color = c;
            }
            else if (renderer.sharedMaterial != null)
            {
                renderer.sharedMaterial.color = c;
            }
        }
    }

    // Toggle: Face Camera (an = mitdrehen)
    public void SetFaceCamera(bool isOn)
    {
        SetBillboardsEnabled(isOn);
    }

    // Toggle: Freeze (an = keine Billboard-Ausrichtung)
    public void SetFreeze(bool isOn)
    {
        if (isOn)
            SetBillboardsEnabled(false);
    }

    void SetBillboardsEnabled(bool enabled)
    {
        for (int i = 0; i < spawnedCircles.Count; i++)
        {
            var bb = spawnedCircles[i].GetComponent<Billboard>();
            if (bb != null)
                bb.enableBillboard = enabled;
        }
    }
}