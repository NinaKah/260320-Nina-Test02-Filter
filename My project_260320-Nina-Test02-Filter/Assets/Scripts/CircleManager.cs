using System.Collections.Generic;
using UnityEngine;

public enum CircleSizeCategory { XS, S, M, L, XL }

[System.Serializable]
public class CircleItem
{
    public string id;
    public CircleSizeCategory size;
    public float colorValue; // 0–1 entlang eines Gradients von Blau nach Rot

    public CircleItem(string id, CircleSizeCategory size, float colorValue)
    {
        this.id = id;
        this.size = size;
        this.colorValue = colorValue;
    }
}

public class CircleManager : MonoBehaviour
{
    [Header("Setup")]
    public GameObject circlePrefab;

    [Header("Layout Settings")]
    public float sizeColumnSpacing = 1.5f;
    public float rowSpacing = 1.2f;
    public float colorLineLength = 10f;
    public float baseScale = 0.3f;

    private CircleItem[] items;
    private List<Transform> spawnedCircles = new List<Transform>();

    void Awake()
    {
        // Fiktive "Datenbank": 100 Einträge, 5 Größen, Farbwerte durchmischt
        int count = 100;
        items = new CircleItem[count];

        for (int i = 0; i < count; i++)
        {
            // Größe zufällig wählen (ungefähr gleich verteilt über XS..XL)
            CircleSizeCategory size = (CircleSizeCategory)Random.Range(0, 5);

            // Farbwert zufällig im Spektrum 0..1
            float colorValue = Random.value;
            string id = $"Item_{i + 1:00}";
            items[i] = new CircleItem(id, size, colorValue);
        }
    }

    void Start()
    {
        SpawnCircles();
        ApplyLayoutBySize();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            ApplyLayoutBySize();

        if (Input.GetKeyDown(KeyCode.Alpha2))
            ApplyLayoutByColor();
    }

    void SpawnCircles()
    {
        foreach (Transform t in spawnedCircles)
            Destroy(t.gameObject);
        spawnedCircles.Clear();

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var go = Instantiate(circlePrefab, Vector3.zero, Quaternion.identity, transform);
            spawnedCircles.Add(go.transform);

            // Größe aus Kategorie ableiten (stärkere Unterschiede)
            int sizeIndex = (int)item.size; // 0..4
            float scaleFactor = 1f + sizeIndex * 0.7f;
            go.transform.localScale = Vector3.one * baseScale * scaleFactor;

            // Farbe aus knalligem Gradient: Hellblau -> Dunkelblau -> Pink -> Rot
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color lightBlue = new Color(0.6f, 0.8f, 1.0f);
                Color darkBlue  = new Color(0.1f, 0.1f, 0.5f);
                Color pink      = new Color(1.0f, 0.3f, 0.8f);
                Color red       = new Color(1.0f, 0.1f, 0.1f);

                float t = Mathf.Clamp01(item.colorValue);
                Color c;
                if (t < 0.33f)
                {
                    float lt = t / 0.33f;
                    c = Color.Lerp(lightBlue, darkBlue, lt);
                }
                else if (t < 0.66f)
                {
                    float lt = (t - 0.33f) / 0.33f;
                    c = Color.Lerp(darkBlue, pink, lt);
                }
                else
                {
                    float lt = (t - 0.66f) / 0.34f;
                    c = Color.Lerp(pink, red, lt);
                }

                renderer.material.color = c;
            }
        }
    }

    // Layout 1: nach Größe (Size) – Spalten für XS, S, M, L, XL
    public void ApplyLayoutBySize()
    {
        if (items == null || items.Length == 0) return;

        int sizeCount = System.Enum.GetValues(typeof(CircleSizeCategory)).Length; // 5
        int[] rowCounter = new int[sizeCount];

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            int sizeIndex = (int)item.size; // 0..4

            float x = (sizeIndex - (sizeCount - 1) * 0.5f) * sizeColumnSpacing;
            int row = rowCounter[sizeIndex];
            float z = row * rowSpacing;
            rowCounter[sizeIndex]++;

            Vector3 pos = new Vector3(x, 0f, z);
            spawnedCircles[i].localPosition = pos;
        }
    }

    // Layout 2: nach Farbe – entlang einer Linie basierend auf colorValue
    public void ApplyLayoutByColor()
    {
        if (items == null || items.Length == 0) return;

        float min = 0f;
        float max = 1f;
        float range = Mathf.Max(0.0001f, max - min);

        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            float t = (item.colorValue - min) / range; // 0..1
            float x = (t - 0.5f) * colorLineLength;
            Vector3 pos = new Vector3(x, 0f, 0f);
            spawnedCircles[i].localPosition = pos;
        }
    }
}