using UnityEngine;

public static class EnemyVisualHelper {
    const string ShortsObjectName = "ShaoKahn";
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static MaterialPropertyBlock propertyBlock;

    public static Renderer ResolveShortsRenderer(Transform enemyRoot, Renderer assigned) {
        if (IsShortsRenderer(assigned)) {
            return assigned;
        }

        return FindShortsRenderer(enemyRoot);
    }

    public static Renderer FindShortsRenderer(Transform enemyRoot) {
        foreach (Transform child in enemyRoot.GetComponentsInChildren<Transform>(true)) {
            if (child.name != ShortsObjectName) {
                continue;
            }

            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null) {
                return renderer;
            }
        }

        foreach (Renderer renderer in enemyRoot.GetComponentsInChildren<Renderer>(true)) {
            if (IsShortsMaterial(renderer.sharedMaterial)) {
                return renderer;
            }
        }

        return null;
    }

    public static void ApplyShortsColor(Renderer shortsRenderer, Color color) {
        if (shortsRenderer == null) {
            return;
        }

        if (propertyBlock == null) {
            propertyBlock = new MaterialPropertyBlock();
        }

        shortsRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(ColorId, color);
        shortsRenderer.SetPropertyBlock(propertyBlock);
    }

    static bool IsShortsRenderer(Renderer renderer) {
        if (renderer == null) {
            return false;
        }

        if (renderer.gameObject.name == ShortsObjectName) {
            return true;
        }

        return IsShortsMaterial(renderer.sharedMaterial);
    }

    static bool IsShortsMaterial(Material material) {
        return material != null && material.name.StartsWith("ShaoKahn_Diff");
    }
}
