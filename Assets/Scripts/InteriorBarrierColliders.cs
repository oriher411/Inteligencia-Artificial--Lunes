using UnityEngine;

public static class InteriorBarrierColliderFix {

    public static void Prepare(Transform barriersRoot) {
        if (barriersRoot == null) {
            return;
        }

        foreach (Transform barrier in barriersRoot) {
            RemoveChildColliders(barrier);

            BoxCollider boxCollider = barrier.GetComponent<BoxCollider>();
            if (boxCollider == null) {
                boxCollider = barrier.gameObject.AddComponent<BoxCollider>();
            }

            FitBoxColliderToRenderers(barrier, boxCollider);
        }
    }

    static void RemoveChildColliders(Transform barrierRoot) {
        foreach (Collider collider in barrierRoot.GetComponentsInChildren<Collider>()) {
            if (collider.transform == barrierRoot) {
                continue;
            }

            if (Application.isPlaying) {
                Object.Destroy(collider);
            } else {
                Object.DestroyImmediate(collider);
            }
        }
    }

    static void FitBoxColliderToRenderers(Transform root, BoxCollider boxCollider) {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) {
            return;
        }

        Bounds localBounds = new Bounds(root.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);

        foreach (Renderer renderer in renderers) {
            Bounds worldBounds = renderer.bounds;
            localBounds.Encapsulate(root.InverseTransformPoint(worldBounds.min));
            localBounds.Encapsulate(root.InverseTransformPoint(worldBounds.max));
        }

        boxCollider.center = localBounds.center;
        boxCollider.size = localBounds.size;
    }
}
