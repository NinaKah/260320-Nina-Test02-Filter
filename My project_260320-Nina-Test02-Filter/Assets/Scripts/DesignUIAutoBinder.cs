using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Reflection;

public class DesignUIAutoBinder : MonoBehaviour
{
    public MonoBehaviour designManager;
    public bool includeInactiveChildren = true;
    public bool registerPanelGraphics = true;
    public bool registerTexts = true;
    public bool registerSelectables = true;

    void Awake()
    {
        if (designManager == null)
            designManager = FindDesignManagerComponent();
    }

    void Start()
    {
        RefreshBindings();
    }

    [ContextMenu("Refresh Bindings")]
    public void RefreshBindings()
    {
        if (designManager == null)
            return;

        if (registerPanelGraphics)
        {
            Graphic[] graphics = GetComponentsInChildren<Graphic>(includeInactiveChildren);
            for (int i = 0; i < graphics.Length; i++)
                InvokeIfExists("RegisterPanelGraphic", graphics[i]);
        }

        if (registerTexts)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(includeInactiveChildren);
            for (int i = 0; i < texts.Length; i++)
                InvokeIfExists("RegisterText", texts[i]);
        }

        if (registerSelectables)
        {
            Selectable[] selectables = GetComponentsInChildren<Selectable>(includeInactiveChildren);
            for (int i = 0; i < selectables.Length; i++)
                InvokeIfExists("RegisterSelectable", selectables[i]);
        }

        InvokeIfExists("ApplyTheme");
        InvokeIfExists("ApplyTextStyles");
    }

    private MonoBehaviour FindDesignManagerComponent()
    {
        MonoBehaviour[] all = FindObjectsOfType<MonoBehaviour>(true);
        for (int i = 0; i < all.Length; i++)
        {
            MonoBehaviour mb = all[i];
            if (mb == null) continue;
            if (mb.GetType().Name == "DesignManager")
                return mb;
        }
        return null;
    }

    private void InvokeIfExists(string methodName, params object[] args)
    {
        if (designManager == null)
            return;

        Type t = designManager.GetType();
        MethodInfo method = t.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null)
            return;

        method.Invoke(designManager, args);
    }
}
