using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class GaussianSplatManager : MonoBehaviour
{
    [System.Serializable]
    public class SplatMapping
    {
        [Tooltip("Optional: Shape-Texture direkt zuweisen")]
        public Texture2D shapeTexture;

        [Tooltip("Optional: Key als Text, z.B. circle-a (ohne .png)")]
        public string key;

        [Tooltip("Nur die numerische superspl.at ID")]
        public string splatId;
    }

    [Header("Viewer")]
    public string baseUrl = "https://superspl.at/s?id=";
    public string testSplatId = "37148813";
    public int viewerWidth = 800;
    public int viewerHeight = 500;

    [Header("Behavior")]
    public bool openOnItemSelection = false;

    [Header("Mapping (Item/Texture -> Splat)")]
    public List<SplatMapping> mappings = new List<SplatMapping>();

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void OpenGaussianSplatOverlay(string url, int width, int height);

    [DllImport("__Internal")]
    private static extern void OpenGaussianSplatInNewTab(string url);

    [DllImport("__Internal")]
    private static extern void CloseGaussianSplatOverlay();
#endif

    public void OpenTestSplat()
    {
        OpenByIdInOverlay(testSplatId);
    }

    public void OpenTestSplatInBrowser()
    {
        OpenByIdInBrowser(testSplatId);
    }

    public void OpenMappedByKey(string key)
    {
        string id = FindMappedSplatIdByKey(key);
        if (!string.IsNullOrEmpty(id))
            OpenByIdInOverlay(id);
    }

    public void OpenMappedByShapeTexture(Texture2D shapeTexture)
    {
        string id = FindMappedSplatIdByTexture(shapeTexture);
        if (!string.IsNullOrEmpty(id))
            OpenByIdInOverlay(id);
    }

    public void OpenById(string id)
    {
        OpenByIdInOverlay(id);
    }

    public void OpenByIdInOverlay(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        OpenUrlInOverlay(baseUrl + id.Trim());
    }

    public void OpenByIdInBrowser(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        OpenUrlInBrowser(baseUrl + id.Trim());
    }

    public void OpenUrl(string url)
    {
        OpenUrlInOverlay(url);
    }

    public void OpenUrlInOverlay(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

#if UNITY_WEBGL && !UNITY_EDITOR
        OpenGaussianSplatOverlay(url, viewerWidth, viewerHeight);
#else
        Application.OpenURL(url);
#endif
    }

    public void OpenUrlInBrowser(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

#if UNITY_WEBGL && !UNITY_EDITOR
        OpenGaussianSplatInNewTab(url);
#else
        Application.OpenURL(url);
#endif
    }

    public void CloseViewer()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        CloseGaussianSplatOverlay();
#endif
    }

    public void HandleItemSelected(CircleItem item, string textureTitle)
    {
        if (!openOnItemSelection)
            return;

        string id = FindMappedSplatId(textureTitle, item != null ? item.id : null);
        if (!string.IsNullOrEmpty(id))
            OpenByIdInOverlay(id);
    }

    public bool TryOpenMapped(CircleItem item, string textureTitle)
    {
        return TryOpenMappedInOverlay(item, textureTitle);
    }

    public bool TryOpenMappedInOverlay(CircleItem item, string textureTitle)
    {
        string id = FindMappedSplatId(textureTitle, item != null ? item.id : null);
        if (string.IsNullOrEmpty(id))
            return false;

        OpenByIdInOverlay(id);
        return true;
    }

    public bool TryOpenMappedInBrowser(CircleItem item, string textureTitle)
    {
        string id = FindMappedSplatId(textureTitle, item != null ? item.id : null);
        if (string.IsNullOrEmpty(id))
            return false;

        OpenByIdInBrowser(id);
        return true;
    }

    string FindMappedSplatIdByKey(string key)
    {
        return FindMappedSplatId(key, null);
    }

    string FindMappedSplatIdByTexture(Texture2D shapeTexture)
    {
        if (shapeTexture == null)
            return null;

        string textureKey = NormalizeKey(shapeTexture.name);
        return FindMappedSplatId(textureKey, null);
    }

    string FindMappedSplatId(string textureTitle, string itemId)
    {
        string textureKey = NormalizeKey(textureTitle);
        string itemKey = NormalizeKey(itemId);

        for (int i = 0; i < mappings.Count; i++)
        {
            SplatMapping m = mappings[i];
            if (m == null || string.IsNullOrWhiteSpace(m.splatId))
                continue;

            if (m.shapeTexture != null)
            {
                string textureAssetKey = NormalizeKey(m.shapeTexture.name);
                if (!string.IsNullOrEmpty(textureAssetKey) && (textureAssetKey == textureKey || textureAssetKey == itemKey))
                    return m.splatId.Trim();
            }

            string mappingKey = NormalizeKey(m.key);
            if (!string.IsNullOrEmpty(mappingKey) && (mappingKey == textureKey || mappingKey == itemKey))
                return m.splatId.Trim();
        }

        return null;
    }

    string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string k = value.Trim().ToLowerInvariant();
        if (k.EndsWith(".png"))
            k = k.Substring(0, k.Length - 4);
        return k;
    }
}
