using UnityEngine;

/// <summary>
/// Runtime visual attachment helper for Player/Monsters.
/// Uses Resources models (FBX/prefab) so the project can run without hand-made prefabs.
/// </summary>
public static class CharacterVisualFactory
{
    public const string PlayerDefaultModelPath = "Models/Player/Player_Elf";

    public static string GetMonsterModelPath(int monsterId)
    {
        // Map current MonsterConfigs ids to CC0 models.
        // 101 鱼人 -> Slime, 102 狗头人 -> Goblin, 201 兽人酋长 -> Skeleton (boss-like)
        switch (monsterId)
        {
            case 101: return "Models/Monsters/Slime";
            case 102: return "Models/Monsters/Goblin_Male";
            case 201: return "Models/Monsters/Skeleton";
            default: return "Models/Monsters/Zombie_Male";
        }
    }

    public static Transform AttachVisual(GameObject owner, string resourcesPath, string childName = "_Visual", float yawDegrees = 180f)
    {
        if (owner == null) return null;
        if (string.IsNullOrEmpty(resourcesPath)) return null;
        if (owner.transform.Find(childName) != null) return owner.transform.Find(childName);

        var prefab = Resources.Load<GameObject>(resourcesPath);
        if (prefab == null) return null;

        var go = Object.Instantiate(prefab, owner.transform);
        go.name = childName;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
        go.transform.localScale = Vector3.one;

        // Remove colliders/rigidbodies from visuals (physics is owned by root)
        var cols = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++) Object.Destroy(cols[i]);
        var rbs = go.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rbs.Length; i++) Object.Destroy(rbs[i]);

        // Hit flash component for combat feedback
        if (go.GetComponent<HitFlash>() == null) go.AddComponent<HitFlash>();

        return go.transform;
    }
}

