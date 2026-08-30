using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CommerceUILayoutBuilder
{
    private static readonly Vector3 CameraSafeScale = new Vector3(0.58f, 0.58f, 1f);

    [MenuItem("Tools/ProjectGame2D/UI/Fit Commerce Windows To Camera")]
    public static void Apply()
    {
        ShopCraftingUI commerceUI = UnityEngine.Object.FindAnyObjectByType<ShopCraftingUI>(FindObjectsInactive.Include);
        if (commerceUI == null) throw new InvalidOperationException("ShopCraftingUI was not found in the active scene.");

        Transform backdrop = RequireChild(commerceUI.transform, "Backdrop");
        FitWindow(RequireChild(backdrop, "ShopWindow"));
        FitWindow(RequireChild(backdrop, "CraftingWindow"));

        EditorUtility.SetDirty(commerceUI.gameObject);
        EditorSceneManager.MarkSceneDirty(commerceUI.gameObject.scene);
        EditorSceneManager.SaveScene(commerceUI.gameObject.scene);
        Debug.Log("CommerceUIRoot windows fitted to the 800x450 camera safe area.");
    }

    private static void FitWindow(Transform window)
    {
        RectTransform rect = window.GetComponent<RectTransform>();
        if (rect == null) throw new InvalidOperationException(window.name + " requires a RectTransform.");

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = CameraSafeScale;
    }

    private static Transform RequireChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null) throw new InvalidOperationException("Required Commerce UI object missing: " + parent.name + "/" + name);
        return child;
    }
}
