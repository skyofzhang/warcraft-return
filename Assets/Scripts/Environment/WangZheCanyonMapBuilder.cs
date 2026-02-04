// 程序化地图（王者峡谷风格-首版占位）：三路 + 河道 + 野区障碍 + 边界墙 + 刷怪点
// 目标：让 Gameplay 场景“即刻可玩”，并与 LevelConfigs.json 的 SP_01~SP_04/SP_BOSS 对齐
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Reflection;

public class WangZheCanyonMapBuilder : MonoBehaviour
{
    public const string RootName = "__WangZheCanyonMap";
    private const int CurrentMapVersion = 9;

    [Header("地图尺寸")]
    public float halfSize = 60f;
    public float groundY = 0f;

    [Header("道路/河道")]
    public float laneWidth = 10f;
    public float riverWidth = 14f;

    [Header("装饰/碰撞")]
    public int jungleObstacleCountPerSide = 10;
    public float obstacleRadiusMin = 1.2f;
    public float obstacleRadiusMax = 2.4f;

    [Header("贴图（可选：存在于 Resources 时自动应用）")]
    [Tooltip("草地/地面（albedo）。例：Environment/Textures/Ground003_1K/Ground003_1K-JPG_Color")]
    public string groundAlbedoPath = "Environment/Textures/Ground003_1K/Ground003_1K-JPG_Color";
    [Tooltip("道路（albedo）。例：Environment/Textures/PavingStones142_1K/PavingStones142_1K-JPG_Color")]
    public string laneAlbedoPath = "Environment/Textures/PavingStones142_1K/PavingStones142_1K-JPG_Color";
    [Tooltip("河道（albedo，可选）。例：Environment/Textures/Ground029_1K/Ground029_1K-JPG_Color")]
    public string riverAlbedoPath = "Environment/Textures/Ground029_1K/Ground029_1K-JPG_Color";
    [Tooltip("岩石/悬崖（albedo）。例：Environment/Textures/Rock050_1K/Rock050_1K-JPG_Color")]
    public string rockAlbedoPath = "Environment/Textures/Rock050_1K/Rock050_1K-JPG_Color";
    [Tooltip("树干（albedo）。例：Environment/Textures/Bark012_1K/Bark012_1K-JPG_Color")]
    public string trunkAlbedoPath = "Environment/Textures/Bark012_1K/Bark012_1K-JPG_Color";

    [Header("材质颜色（URP/Lit 或 Standard）")]
    public Color groundColor = new Color(0.33f, 0.35f, 0.33f, 1f);
    public Color laneColor = new Color(0.25f, 0.25f, 0.25f, 1f);
    public Color riverColor = new Color(0.10f, 0.22f, 0.30f, 0.85f);
    public Color jungleColor = new Color(0.18f, 0.26f, 0.18f, 1f);

    private Material groundMat;
    private Material laneMat;
    private Material riverMat;
    private Material jungleMat;
    private Material borderMat;
    private Material rockMat;
    private Material trunkMat;
    private Material leafMat;

    private void Start()
    {
        BuildIfNeeded();
    }

    public void BuildIfNeeded()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            var marker = existing.GetComponent<MapVersionMarker>();
            if (marker != null && marker.version == CurrentMapVersion) return;

            // Version mismatch: rebuild so visual improvements take effect without manual deletion.
            if (Application.isPlaying) Destroy(existing);
            else DestroyImmediate(existing);
        }
        Build();
    }

    public void Build()
    {
        var root = new GameObject(RootName);
        root.transform.SetParent(transform, false);
        var marker = root.AddComponent<MapVersionMarker>();
        marker.version = CurrentMapVersion;

        EnsureLightingAndPost(root.transform);
        EnsureMaterials();

        // Ground layer
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0) groundLayer = 0;

        // 1) Base ground
        var ground = CreatePlane(root.transform, "_Ground_Base", halfSize * 2f, groundY, groundMat);
        SetLayerRecursively(ground, groundLayer);

        // 2) Lanes (top/mid/bottom) — 用“直线段”拼接，尽量贴近王者峡谷三路结构
        // 左下基地 -> 右上基地
        Vector3 baseA = new Vector3(-halfSize * 0.75f, groundY, -halfSize * 0.75f);
        Vector3 baseB = new Vector3(halfSize * 0.75f, groundY, halfSize * 0.75f);

        // 中路：对角线
        CreateLaneWithBorders(root.transform, "_Lane_Mid", baseA, baseB, laneWidth, groundLayer);

        // 上路：沿上边缘再回到基地
        Vector3 topTurnA = new Vector3(-halfSize * 0.20f, groundY, halfSize * 0.80f);
        Vector3 topTurnB = new Vector3(halfSize * 0.80f, groundY, halfSize * 0.20f);
        CreateLaneWithBorders(root.transform, "_Lane_Top_A", baseA, topTurnA, laneWidth, groundLayer);
        CreateLaneWithBorders(root.transform, "_Lane_Top_B", topTurnA, topTurnB, laneWidth, groundLayer);
        CreateLaneWithBorders(root.transform, "_Lane_Top_C", topTurnB, baseB, laneWidth, groundLayer);

        // 下路：沿下边缘再回到基地
        Vector3 botTurnA = new Vector3(halfSize * 0.20f, groundY, -halfSize * 0.80f);
        Vector3 botTurnB = new Vector3(-halfSize * 0.80f, groundY, -halfSize * 0.20f);
        CreateLaneWithBorders(root.transform, "_Lane_Bot_A", baseA, botTurnB, laneWidth, groundLayer);
        CreateLaneWithBorders(root.transform, "_Lane_Bot_B", botTurnB, botTurnA, laneWidth, groundLayer);
        CreateLaneWithBorders(root.transform, "_Lane_Bot_C", botTurnA, baseB, laneWidth, groundLayer);

        // 3) River：与中路交叉的一条宽带（对角线反向）
        Vector3 riverA = new Vector3(-halfSize * 0.85f, groundY, halfSize * 0.10f);
        Vector3 riverB = new Vector3(halfSize * 0.85f, groundY, -halfSize * 0.10f);
        // 河道略抬高，避免与地面 Z-Fighting
        CreateStrip(root.transform, "_River", riverA, riverB, riverWidth, riverMat, groundLayer, yOffset: 0.02f, addCollider: true, metersPerRepeat: 7.5f);
        CreateRiverBanks(root.transform, riverA, riverB, riverWidth, groundLayer);
        var riverGo = root.transform.Find("_River");
        if (riverGo != null)
        {
            // For MT/RiverWater, the shader animates flow itself; don't double-scroll via TextureScroll.
            if (riverMat != null && riverMat.shader != null && riverMat.shader.name == "MT/RiverWater")
            {
                var old = riverGo.gameObject.GetComponent<TextureScroll>();
                if (old != null) Destroy(old);
            }
            else
            {
                var scroller = riverGo.gameObject.GetComponent<TextureScroll>();
                if (scroller == null) scroller = riverGo.gameObject.AddComponent<TextureScroll>();
                scroller.speed = new Vector2(0.03f, 0.00f);
                scroller.propertyName = "_BaseMap";
                scroller.secondaryPropertyName = "_BumpMap";
                scroller.secondarySpeed = new Vector2(0.015f, 0.010f);
            }
        }

        // 4) Jungle zones：用稍深的地块区域提示“野区”
        CreateJungleZone(root.transform, "_Jungle_Top", new Vector3(0f, groundY, halfSize * 0.45f), new Vector2(halfSize * 1.2f, halfSize * 0.55f), jungleMat, groundLayer);
        CreateJungleZone(root.transform, "_Jungle_Bot", new Vector3(0f, groundY, -halfSize * 0.45f), new Vector2(halfSize * 1.2f, halfSize * 0.55f), jungleMat, groundLayer);

        // 5) Obstacles：给野区放障碍（碰撞），让路线更像 MOBA
        PlaceJungleObstacles(root.transform, baseA, baseB, groundLayer);

        // 5.5) Extra deco：补充灌木/碎石（不阻挡路面），提升“丰富度”
        ScatterDeco(root.transform, baseA, baseB, riverA, riverB, groundLayer);

        // 6) Boundary walls：避免跑出地图
        BuildBoundaryCliffs(root.transform, halfSize, groundLayer);

        // 6.5) Bases：增加基地地台，让出生区更有“目标感”
        BuildBasePads(root.transform, baseA, baseB, groundLayer);

        // 7) Spawn points：与 LevelConfigs.json 对齐（SP_01~SP_04 + SP_BOSS）
        EnsureSpawnPoints(root.transform, baseA, baseB);

        // 8) 让 MonsterSpawner 立刻能用这些点（如果场景里有 MonsterSpawner）
        BindSpawnPointsToSpawner();
    }

    private void EnsureMaterials()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (lit == null) lit = Shader.Find("Standard");
        if (lit == null) lit = Shader.Find("Unlit/Color");

        groundMat = CreateMat(lit, groundColor, smoothness: 0.10f);
        laneMat = CreateMat(lit, laneColor, smoothness: 0.18f);
        borderMat = CreateMat(lit, new Color(0.18f, 0.18f, 0.18f, 1f), smoothness: 0.22f);
        // River: prefer custom URP water shader for fresnel+foam
        var waterShader = Shader.Find("MT/RiverWater");
        if (waterShader != null)
        {
            riverMat = new Material(waterShader);
            riverMat.name = "_Runtime_MT_RiverWater";
            riverMat.SetColor("_BaseColor", riverColor);
            EnsureNoiseTex(riverMat);
        }
        else
        {
            riverMat = CreateMat(lit, riverColor, smoothness: 0.90f, metallic: 0.02f);
        }
        jungleMat = CreateMat(lit, jungleColor, smoothness: 0.12f);
        rockMat = CreateMat(lit, new Color(0.28f, 0.28f, 0.30f, 1f), smoothness: 0.28f);
        trunkMat = CreateMat(lit, new Color(0.26f, 0.21f, 0.15f, 1f), smoothness: 0.10f);
        leafMat = CreateMat(lit, new Color(0.12f, 0.32f, 0.12f, 1f), smoothness: 0.10f);

        // 可选贴图：存在则覆盖 BaseMap，并设置重复；同时尝试绑定 Normal/AO
        ApplyPbrSet(groundMat, groundAlbedoPath, new Vector2(12f, 12f));
        ApplyPbrSet(laneMat, laneAlbedoPath, new Vector2(1f, 1f));
        ApplyPbrSet(borderMat, laneAlbedoPath, new Vector2(1f, 1f));

        // 地表“真实感”补丁：增加高频细节噪声，降低大面积重复纹理的“塑料感”
        EnsureDetailAlbedoNoise(groundMat, detailTiling: 48f, strength: 0.18f, seed: 123);
        EnsureDetailAlbedoNoise(jungleMat, detailTiling: 56f, strength: 0.14f, seed: 456);

        // River water uses custom shader: don't bind "ground" textures; keep base white + shader noise/flow.
        if (riverMat != null && riverMat.shader != null && riverMat.shader.name == "MT/RiverWater")
        {
            riverMat.SetTexture("_BaseMap", Texture2D.whiteTexture);
            riverMat.SetFloat("_Alpha", 0.52f);
            riverMat.SetFloat("_AlphaDeep", 0.78f);
            riverMat.SetFloat("_FresnelPower", 3.9f);
            riverMat.SetFloat("_FresnelIntensity", 0.48f);
            riverMat.SetFloat("_SpecularBoost", 0.95f);
            riverMat.SetFloat("_FoamWidth", 0.035f);
            riverMat.SetFloat("_FoamIntensity", 0.18f);
            riverMat.SetFloat("_ShoreFoamDepth", 0.55f);
            riverMat.SetFloat("_ShoreFoamIntensity", 0.55f);
            riverMat.SetFloat("_DepthMax", 3.5f);
            riverMat.SetFloat("_DepthColorStrength", 0.92f);
            riverMat.SetFloat("_DepthAlphaStrength", 0.85f);
            riverMat.SetFloat("_ChannelDepthPower", 1.75f);
            riverMat.SetColor("_ShallowColor", new Color(0.20f, 0.56f, 0.62f, 1f));
            riverMat.SetColor("_DeepColor", new Color(0.04f, 0.20f, 0.27f, 1f));
            riverMat.SetFloat("_RefractionStrength", 0.028f);
            riverMat.SetFloat("_RefractionFresnelBoost", 0.35f);
        }
        else
        {
            ApplyPbrSet(riverMat, riverAlbedoPath, new Vector2(1f, 1f));
        }

        ApplyPbrSet(rockMat, rockAlbedoPath, new Vector2(1f, 1f));
        ApplyPbrSet(trunkMat, trunkAlbedoPath, new Vector2(1f, 1f));
    }

    private static Material CreateMat(Shader shader, Color color, float smoothness = 0.2f, float metallic = 0f)
    {
        var m = new Material(shader);
        m.name = "_Runtime_" + shader.name.Replace("/", "_");
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        // 透明度（URP/Lit）：这里只做弱支持，不强行改 SurfaceType，避免 SRP keyword 复杂度
        return m;
    }

    private static GameObject CreatePlane(Transform parent, string name, float size, float y, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = new Vector3(0f, y, 0f);
        go.transform.rotation = Quaternion.identity;
        // Unity Plane 默认 10x10
        float s = Mathf.Max(1f, size) / 10f;
        go.transform.localScale = new Vector3(s, 1f, s);
        var r = go.GetComponent<Renderer>();
        if (r != null && mat != null) r.sharedMaterial = mat;
        return go;
    }

    private static GameObject CreateQuad(Transform parent, string name, Vector3 center, Vector2 size, Material mat, int layer, float yOffset = 0f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = new Vector3(center.x, center.y + yOffset, center.z);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var r = go.GetComponent<Renderer>();
        if (r != null && mat != null) r.sharedMaterial = mat;
        SetLayerRecursively(go, layer);
        return go;
    }

    private static void CreateStrip(Transform parent, string name, Vector3 a, Vector3 b, float width, Material mat, int layer, float yOffset = 0f, bool addCollider = true, float metersPerRepeat = 6f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        MeshCollider mc = null;
        if (addCollider) mc = go.AddComponent<MeshCollider>();

        var dir = (b - a);
        dir.y = 0f;
        float len = Mathf.Max(0.01f, dir.magnitude);
        dir /= len;
        var right = new Vector3(-dir.z, 0f, dir.x);
        float hw = Mathf.Max(0.5f, width) * 0.5f;

        Vector3 v0 = a + right * hw; v0.y = a.y + yOffset;
        Vector3 v1 = a - right * hw; v1.y = a.y + yOffset;
        Vector3 v2 = b + right * hw; v2.y = b.y + yOffset;
        Vector3 v3 = b - right * hw; v3.y = b.y + yOffset;

        var mesh = new Mesh();
        mesh.name = name;
        mesh.vertices = new[] { v0, v1, v2, v3 };
        float u = len / Mathf.Max(0.5f, metersPerRepeat);
        mesh.uv = new[]
        {
            new Vector2(0f, 1f), new Vector2(0f, 0f),
            new Vector2(u, 1f), new Vector2(u, 0f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;
        if (mc != null) mc.sharedMesh = mesh;
        if (mr != null && mat != null) mr.sharedMaterial = mat;

        SetLayerRecursively(go, layer);
    }

    private void ScatterDeco(Transform parent, Vector3 baseA, Vector3 baseB, Vector3 riverA, Vector3 riverB, int layer)
    {
        var rng = new System.Random(4242);
        int count = Mathf.Clamp(Mathf.RoundToInt(halfSize * 1.7f), 90, 160);

        var topTurnA = new Vector3(-halfSize * 0.20f, groundY, halfSize * 0.80f);
        var topTurnB = new Vector3(halfSize * 0.80f, groundY, halfSize * 0.20f);
        var botTurnA = new Vector3(halfSize * 0.20f, groundY, -halfSize * 0.80f);
        var botTurnB = new Vector3(-halfSize * 0.80f, groundY, -halfSize * 0.20f);

        // lane+river segments for avoidance
        var segs = new (Vector3 a, Vector3 b, float radius)[]
        {
            (baseA, baseB, laneWidth * 0.80f),
            (baseA, topTurnA, laneWidth * 0.80f),
            (topTurnA, topTurnB, laneWidth * 0.80f),
            (topTurnB, baseB, laneWidth * 0.80f),
            (baseA, botTurnB, laneWidth * 0.80f),
            (botTurnB, botTurnA, laneWidth * 0.80f),
            (botTurnA, baseB, laneWidth * 0.80f),
            (riverA, riverB, riverWidth * 0.85f),
        };

        for (int i = 0; i < count; i++)
        {
            float x = (float)(rng.NextDouble() * 2.0 - 1.0) * (halfSize * 0.95f);
            float z = (float)(rng.NextDouble() * 2.0 - 1.0) * (halfSize * 0.95f);
            var p = new Vector3(x, groundY, z);

            // avoid base areas
            if ((p - baseA).sqrMagnitude < 12f * 12f) continue;
            if ((p - baseB).sqrMagnitude < 12f * 12f) continue;

            bool nearMain = false;
            for (int k = 0; k < segs.Length; k++)
            {
                float d = DistancePointToSegmentXZ(p, segs[k].a, segs[k].b);
                if (d < segs[k].radius) { nearMain = true; break; }
            }
            if (nearMain) continue;

            bool asBush = rng.NextDouble() > 0.28;
            if (asBush)
            {
                var bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bush.name = "_Bush";
                bush.transform.SetParent(parent, true);
                float r = Mathf.Lerp(0.35f, 1.05f, (float)rng.NextDouble());
                bush.transform.position = p + Vector3.up * (r * 0.55f);
                bush.transform.localScale = new Vector3(r * 1.35f, r * 0.95f, r * 1.25f);
                ApplyMat(bush, leafMat, new Color(0.10f, 0.30f, 0.12f, 1f));
                SetLayerRecursively(bush, layer);
                var col = bush.GetComponent<Collider>();
                if (col != null) Destroy(col); // deco only
            }
            else
            {
                var pebble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pebble.name = "_Pebble";
                pebble.transform.SetParent(parent, true);
                float r = Mathf.Lerp(0.18f, 0.55f, (float)rng.NextDouble());
                pebble.transform.position = p + Vector3.up * (r * 0.5f);
                pebble.transform.localScale = new Vector3(r * 1.2f, r * 0.85f, r * 1.1f);
                ApplyMat(pebble, rockMat, new Color(0.26f, 0.26f, 0.28f, 1f));
                SetLayerRecursively(pebble, layer);
                var col = pebble.GetComponent<Collider>();
                if (col != null) Destroy(col); // deco only
            }
        }
    }

    private static float DistancePointToSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector2 pp = new Vector2(p.x, p.z);
        Vector2 aa = new Vector2(a.x, a.z);
        Vector2 bb = new Vector2(b.x, b.z);
        Vector2 ab = bb - aa;
        float ab2 = ab.sqrMagnitude;
        if (ab2 < 0.000001f) return Vector2.Distance(pp, aa);
        float t = Vector2.Dot(pp - aa, ab) / ab2;
        t = Mathf.Clamp01(t);
        Vector2 proj = aa + ab * t;
        return Vector2.Distance(pp, proj);
    }

    private static void CreateJungleZone(Transform parent, string name, Vector3 center, Vector2 size, Material mat, int layer)
    {
        CreateQuad(parent, name, center, size, mat, layer, yOffset: 0.01f);
    }

    private void PlaceJungleObstacles(Transform parent, Vector3 baseA, Vector3 baseB, int layer)
    {
        var rng = new System.Random(1337);

        // 上下两侧各放一些“树/石”占位（组合体），模拟野区曲折（避免像巨大柱子）
        PlaceObstaclesInBand(parent, rng, new Vector3(0f, groundY, halfSize * 0.35f), new Vector2(halfSize * 1.4f, halfSize * 0.45f), jungleObstacleCountPerSide, layer);
        PlaceObstaclesInBand(parent, rng, new Vector3(0f, groundY, -halfSize * 0.35f), new Vector2(halfSize * 1.4f, halfSize * 0.45f), jungleObstacleCountPerSide, layer);

        // 中央河道附近两侧的“草丛”占位
        PlaceObstaclesInBand(parent, rng, new Vector3(-halfSize * 0.10f, groundY, 0f), new Vector2(halfSize * 0.35f, halfSize * 0.25f), 6, layer);
        PlaceObstaclesInBand(parent, rng, new Vector3(halfSize * 0.10f, groundY, 0f), new Vector2(halfSize * 0.35f, halfSize * 0.25f), 6, layer);
    }

    private void PlaceObstaclesInBand(Transform parent, System.Random rng, Vector3 center, Vector2 size, int count, int layer)
    {
        for (int i = 0; i < count; i++)
        {
            float x = center.x + (float)(rng.NextDouble() - 0.5) * size.x;
            float z = center.z + (float)(rng.NextDouble() - 0.5) * size.y;
            float r = Mathf.Lerp(obstacleRadiusMin, obstacleRadiusMax, (float)rng.NextDouble()) * 0.55f;

            // 树（圆柱+球）/岩石（球）随机
            bool asTree = rng.NextDouble() > 0.35;
            if (asTree)
            {
                CreateTree(parent, new Vector3(x, groundY, z), r, layer);
            }
            else
            {
                CreateRock(parent, new Vector3(x, groundY, z), r, layer);
            }
        }
    }

    private void CreateTree(Transform parent, Vector3 pos, float baseR, int layer)
    {
        var go = new GameObject("_Tree");
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.value * 360f, 0f);

        // trunk
        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(go.transform, true);
        trunk.transform.position = pos + Vector3.up * (baseR * 1.2f);
        trunk.transform.localScale = new Vector3(baseR * 0.35f, baseR * 1.2f, baseR * 0.35f);
        ApplyMat(trunk, trunkMat, new Color(0.26f, 0.21f, 0.15f, 1f));
        SetLayerRecursively(trunk, layer);

        // canopy
        var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "Canopy";
        canopy.transform.SetParent(go.transform, true);
        canopy.transform.position = pos + Vector3.up * (baseR * 2.6f);
        canopy.transform.localScale = new Vector3(baseR * 1.5f, baseR * 1.2f, baseR * 1.5f);
        ApplyMat(canopy, leafMat, new Color(0.12f, 0.32f, 0.12f, 1f));
        SetLayerRecursively(canopy, layer);

        // Canopy shouldn't block movement; trunk/rocks are enough.
        var canopyCol = canopy.GetComponent<Collider>();
        if (canopyCol != null) Destroy(canopyCol);
    }

    private void CreateRock(Transform parent, Vector3 pos, float baseR, int layer)
    {
        var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = "_Rock";
        rock.transform.SetParent(parent, true);
        rock.transform.position = pos + Vector3.up * (baseR * 0.6f);
        rock.transform.localScale = new Vector3(baseR * 1.2f, baseR * 0.9f, baseR * 1.1f);
        ApplyMat(rock, rockMat, new Color(0.28f, 0.28f, 0.30f, 1f));
        SetLayerRecursively(rock, layer);
    }

    private static void ApplyMat(GameObject go, Material baseMat, Color tint)
    {
        if (go == null) return;
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        if (baseMat != null)
        {
            var m = new Material(baseMat);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
            else if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
            r.sharedMaterial = m;
        }
    }

    private static void TryApplyAlbedo(Material mat, string resourcesPath, Vector2 tiling)
    {
        if (mat == null) return;
        if (string.IsNullOrEmpty(resourcesPath)) return;
        var tex = Resources.Load<Texture2D>(resourcesPath);
        if (tex == null) return;

        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);

        if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", tiling);
        if (mat.HasProperty("_MainTex")) mat.SetTextureScale("_MainTex", tiling);
    }

    private static void ApplyPbrSet(Material mat, string albedoPath, Vector2 tiling)
    {
        if (mat == null) return;
        if (string.IsNullOrEmpty(albedoPath)) return;

        TryApplyAlbedo(mat, albedoPath, tiling);

        // Derive other maps from ambientCG naming:
        // *_Color -> *_NormalGL / *_AmbientOcclusion / *_Roughness / *_MaskMap (if exists)
        string normalPath = albedoPath.Replace("_Color", "_NormalGL");
        string aoPath = albedoPath.Replace("_Color", "_AmbientOcclusion");
        string maskPath = albedoPath.Replace("_Color", "_MaskMap");

        var normal = Resources.Load<Texture2D>(normalPath);
        if (normal != null)
        {
            if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", normal);
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 0.75f);
            mat.EnableKeyword("_NORMALMAP");
        }

        // Prefer URP MaskMap if present (baked by Tools/CC0/Bake URP MaskMaps)
        var mask = Resources.Load<Texture2D>(maskPath);
        if (mask != null && mat.HasProperty("_MaskMap"))
        {
            mat.SetTexture("_MaskMap", mask);
            // Use alpha channel as smoothness (multiplied by _Smoothness)
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1.0f);
            mat.EnableKeyword("_MASKMAP");
        }
        else
        {
            var ao = Resources.Load<Texture2D>(aoPath);
            if (ao != null)
            {
                if (mat.HasProperty("_OcclusionMap")) mat.SetTexture("_OcclusionMap", ao);
                if (mat.HasProperty("_OcclusionStrength")) mat.SetFloat("_OcclusionStrength", 1.0f);
                mat.EnableKeyword("_OCCLUSIONMAP");
            }
        }
    }

    private static void EnsureNoiseTex(Material mat)
    {
        if (mat == null) return;
        if (!mat.HasProperty("_NoiseTex")) return;
        bool hasNoise = mat.GetTexture("_NoiseTex") != null;

        // Also ensure a usable normal map for water so specular doesn't become blotchy.
        bool wantsBump = mat.HasProperty("_BumpMap");
        bool hasBump = wantsBump && mat.GetTexture("_BumpMap") != null;

        if (!hasNoise || (wantsBump && !hasBump))
        {
            const int size = 96;
            float scale = 0.11f;

            float SampleN(int x, int y)
            {
                return Mathf.PerlinNoise(x * scale, y * scale);
            }

            if (!hasNoise)
            {
                var noise = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: true, linear: true);
                noise.wrapMode = TextureWrapMode.Repeat;
                noise.filterMode = FilterMode.Bilinear;
                var cols = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float n = SampleN(x, y);
                        byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(n * 255f), 0, 255);
                        cols[y * size + x] = new Color32(b, b, b, 255);
                    }
                }
                noise.SetPixels32(cols);
                noise.Apply(updateMipmaps: true, makeNoLongerReadable: true);
                mat.SetTexture("_NoiseTex", noise);
            }

            if (wantsBump && !hasBump)
            {
                var normal = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: true, linear: true);
                normal.wrapMode = TextureWrapMode.Repeat;
                normal.filterMode = FilterMode.Bilinear;
                var colsN = new Color32[size * size];
                float strength = 2.2f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int xm = (x - 1 + size) % size;
                        int xp = (x + 1) % size;
                        int ym = (y - 1 + size) % size;
                        int yp = (y + 1) % size;

                        float dx = (SampleN(xp, y) - SampleN(xm, y)) * strength;
                        float dy = (SampleN(x, yp) - SampleN(x, ym)) * strength;
                        Vector3 n = new Vector3(-dx, -dy, 1f).normalized;
                        byte r = (byte)Mathf.Clamp(Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255f), 0, 255);
                        byte g = (byte)Mathf.Clamp(Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255f), 0, 255);
                        byte b = (byte)Mathf.Clamp(Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255f), 0, 255);
                        colsN[y * size + x] = new Color32(r, g, b, 255);
                    }
                }
                normal.SetPixels32(colsN);
                normal.Apply(updateMipmaps: true, makeNoLongerReadable: true);
                mat.SetTexture("_BumpMap", normal);
            }
        }
    }

    private static void EnsureDetailAlbedoNoise(Material mat, float detailTiling, float strength, int seed)
    {
        if (mat == null) return;
        if (!mat.HasProperty("_DetailAlbedoMap")) return;

        // 已设置则不重复生成
        if (mat.GetTexture("_DetailAlbedoMap") != null) return;

        int size = 128;
        float freq = 0.07f;
        strength = Mathf.Clamp(strength, 0.02f, 0.35f);

        float SampleN(int x, int y, float f)
        {
            return Mathf.PerlinNoise((x + seed) * f, (y + seed) * f);
        }

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: true, linear: false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        var cols = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float n1 = SampleN(x, y, freq);
                float n2 = SampleN(x, y, freq * 2.1f);
                float v = (n1 * 0.65f + n2 * 0.35f);
                v = Mathf.Pow(v, 1.15f);

                // around 0.5, slightly up/down
                float c = Mathf.Lerp(0.5f - strength, 0.5f + strength, v);
                byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(c * 255f), 0, 255);
                cols[y * size + x] = new Color32(b, b, b, 255);
            }
        }
        tex.SetPixels32(cols);
        tex.Apply(updateMipmaps: true, makeNoLongerReadable: true);

        mat.SetTexture("_DetailAlbedoMap", tex);
        mat.SetTextureScale("_DetailAlbedoMap", new Vector2(detailTiling, detailTiling));
        if (mat.HasProperty("_DetailAlbedoMapScale")) mat.SetFloat("_DetailAlbedoMapScale", 1.0f);
        mat.EnableKeyword("_DETAIL_MULX2");
    }

    private void CreateLaneWithBorders(Transform parent, string name, Vector3 a, Vector3 b, float width, int layer)
    {
        // main
        CreateStrip(parent, name, a, b, width, laneMat, layer, yOffset: 0.03f, addCollider: true, metersPerRepeat: 4.8f);

        // borders
        float borderW = Mathf.Clamp(width * 0.18f, 0.8f, 1.4f);
        var dir = (b - a); dir.y = 0f;
        float len = Mathf.Max(0.01f, dir.magnitude);
        dir /= len;
        var right = new Vector3(-dir.z, 0f, dir.x);
        float offset = width * 0.5f + borderW * 0.45f;

        CreateStrip(parent, name + "_Border_R", a + right * offset, b + right * offset, borderW, borderMat, layer, yOffset: 0.05f, addCollider: false, metersPerRepeat: 3.2f);
        CreateStrip(parent, name + "_Border_L", a - right * offset, b - right * offset, borderW, borderMat, layer, yOffset: 0.05f, addCollider: false, metersPerRepeat: 3.2f);
    }

    private void CreateRiverBanks(Transform parent, Vector3 a, Vector3 b, float width, int layer)
    {
        float bankW = Mathf.Clamp(width * 0.22f, 1.2f, 2.0f);
        var dir = (b - a); dir.y = 0f;
        float len = Mathf.Max(0.01f, dir.magnitude);
        dir /= len;
        var right = new Vector3(-dir.z, 0f, dir.x);
        float offset = width * 0.5f + bankW * 0.45f;

        CreateStrip(parent, "_RiverBank_R", a + right * offset, b + right * offset, bankW, rockMat, layer, yOffset: 0.04f, addCollider: false, metersPerRepeat: 4.6f);
        CreateStrip(parent, "_RiverBank_L", a - right * offset, b - right * offset, bankW, rockMat, layer, yOffset: 0.04f, addCollider: false, metersPerRepeat: 4.6f);
    }

    private void BuildBasePads(Transform parent, Vector3 baseA, Vector3 baseB, int layer)
    {
        CreateBasePad(parent, "_Base_A", baseA, layer);
        CreateBasePad(parent, "_Base_B", baseB, layer);
    }

    private void CreateBasePad(Transform parent, string name, Vector3 pos, int layer)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, true);
        root.transform.position = pos;

        var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "Pad";
        pad.transform.SetParent(root.transform, true);
        pad.transform.position = pos + Vector3.up * 0.03f;
        pad.transform.localScale = new Vector3(9f, 0.06f, 9f);
        var rr = pad.GetComponent<Renderer>();
        if (rr != null) rr.sharedMaterial = borderMat != null ? borderMat : laneMat;
        SetLayerRecursively(pad, layer);
        var padCol = pad.GetComponent<Collider>();
        if (padCol != null) Destroy(padCol);

        // crystal/obelisk
        var ob = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ob.name = "Obelisk";
        ob.transform.SetParent(root.transform, true);
        ob.transform.position = pos + Vector3.up * 1.8f;
        ob.transform.localScale = new Vector3(0.9f, 1.8f, 0.9f);
        var or = ob.GetComponent<Renderer>();
        if (or != null) or.sharedMaterial = riverMat;
        SetLayerRecursively(ob, layer);
        var obCol = ob.GetComponent<Collider>();
        if (obCol != null) Destroy(obCol);
    }

    private void EnsureLightingAndPost(Transform root)
    {
        // 只生成一次（runtime）
        if (root == null) return;
        if (root.Find("__Lighting") != null) return;

        EnsureCameraDepthTexture();

        var lighting = new GameObject("__Lighting");
        lighting.transform.SetParent(root, true);

        // Sun light
        var sunGo = new GameObject("Sun");
        sunGo.transform.SetParent(lighting.transform, true);
        sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var light = sunGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.35f;
        light.shadows = LightShadows.Soft;
        light.color = new Color(1f, 0.98f, 0.92f, 1f);
        RenderSettings.sun = light;

        // Procedural skybox (built-in shader) to avoid flat background.
        var skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader != null)
        {
            var sky = new Material(skyShader);
            sky.SetFloat("_SunSize", 0.03f);
            sky.SetFloat("_SunSizeConvergence", 6f);
            sky.SetFloat("_AtmosphereThickness", 1.1f);
            sky.SetColor("_SkyTint", new Color(0.52f, 0.62f, 0.75f, 1f));
            sky.SetColor("_GroundColor", new Color(0.18f, 0.18f, 0.20f, 1f));
            sky.SetFloat("_Exposure", 1.25f);
            RenderSettings.skybox = sky;
        }

        // Ambient + Fog
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.36f, 0.45f, 0.55f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.30f, 0.34f, 0.30f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        RenderSettings.ambientIntensity = 1.1f;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.50f, 0.58f, 0.62f, 1f);
        // exp^2 mode uses density
        RenderSettings.fogDensity = 0.0035f;

        // Reflection probe for nicer specular on water/stone (cheap single probe).
        var rpGo = new GameObject("ReflectionProbe");
        rpGo.transform.SetParent(lighting.transform, true);
        rpGo.transform.position = new Vector3(0f, 3.5f, 0f);
        var rp = rpGo.AddComponent<ReflectionProbe>();
        rp.mode = ReflectionProbeMode.Realtime;
        rp.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        rp.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        rp.size = new Vector3(halfSize * 2.2f, 12f, halfSize * 2.2f);
        rp.intensity = 1.1f;

        // 后处理（可选）：部分工程可能未安装 SRP Core/URP 包（Volume/VolumeProfile 类型不存在）。
        // 这里用反射：存在 Volume 系统时创建 Global Volume，并在存在 URP 时添加 Bloom/ColorAdjustments/Vignette。
        var profile = TryCreateGlobalVolumeProfile(lighting.transform);
        if (profile == null) return;

        TryAddUrpOverride(profile,
            typeName: "UnityEngine.Rendering.Universal.Bloom, Unity.RenderPipelines.Universal.Runtime",
            floatOverrides: new (string member, float value)[] { ("intensity", 0.42f), ("threshold", 1.05f) });

        TryAddUrpOverride(profile,
            typeName: "UnityEngine.Rendering.Universal.ColorAdjustments, Unity.RenderPipelines.Universal.Runtime",
            floatOverrides: new (string member, float value)[] { ("postExposure", 0.18f), ("contrast", 12f), ("saturation", 10f) });

        TryAddUrpOverride(profile,
            typeName: "UnityEngine.Rendering.Universal.Vignette, Unity.RenderPipelines.Universal.Runtime",
            floatOverrides: new (string member, float value)[] { ("intensity", 0.18f), ("smoothness", 0.4f) });

        TryAddUrpOverride(profile,
            typeName: "UnityEngine.Rendering.Universal.ChromaticAberration, Unity.RenderPipelines.Universal.Runtime",
            floatOverrides: new (string member, float value)[] { ("intensity", 0.03f) });
    }

    private static void EnsureCameraDepthTexture()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            // Fallback: pick any enabled camera (some scenes might not tag MainCamera)
            var cams = UnityEngine.Object.FindObjectsOfType<Camera>(true);
            for (int i = 0; i < cams.Length; i++)
            {
                if (cams[i] != null && cams[i].enabled) { cam = cams[i]; break; }
            }
        }
        if (cam == null) return;

        // Built-in fallback
        cam.depthTextureMode |= DepthTextureMode.Depth;

        // URP: UniversalAdditionalCameraData.requiresDepthTexture = true (via reflection to avoid hard dependency)
        var t = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime", throwOnError: false);
        if (t == null) return;

        var comp = cam.GetComponent(t);
        if (comp == null) comp = cam.gameObject.AddComponent(t);
        if (comp == null) return;

        var p = t.GetProperty("requiresDepthTexture", BindingFlags.Instance | BindingFlags.Public);
        if (p != null && p.CanWrite)
        {
            try { p.SetValue(comp, true, null); } catch { }
        }
        var p2 = t.GetProperty("requiresOpaqueTexture", BindingFlags.Instance | BindingFlags.Public);
        if (p2 != null && p2.CanWrite)
        {
            try { p2.SetValue(comp, true, null); } catch { }
        }
    }

    private static object TryCreateGlobalVolumeProfile(Transform parent)
    {
        if (parent == null) return null;

        // SRP Core types (may not exist in Built-in pipeline projects)
        var volumeType = Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime", throwOnError: false);
        var profileType = Type.GetType("UnityEngine.Rendering.VolumeProfile, Unity.RenderPipelines.Core.Runtime", throwOnError: false);
        if (volumeType == null || profileType == null) return null;

        var volGo = new GameObject("GlobalVolume");
        volGo.transform.SetParent(parent, true);
        var volComp = volGo.AddComponent(volumeType);
        if (volComp == null) return null;

        var profile = ScriptableObject.CreateInstance(profileType);
        if (profile == null) return null;

        // Set Volume.isGlobal / priority / profile
        TrySetMember(volComp, "isGlobal", true);
        TrySetMember(volComp, "priority", 10f);
        TrySetMember(volComp, "profile", profile);

        return profile;
    }

    private static void TryAddUrpOverride(object profile, string typeName, (string member, float value)[] floatOverrides)
    {
        if (profile == null || string.IsNullOrEmpty(typeName)) return;
        var t = Type.GetType(typeName, throwOnError: false);
        if (t == null) return;

        object comp = AddVolumeComponent(profile, t, overrides: true);
        if (comp == null) return;

        if (floatOverrides != null)
        {
            for (int i = 0; i < floatOverrides.Length; i++)
            {
                SetVolumeParameterFloatOverride(comp, floatOverrides[i].member, floatOverrides[i].value);
            }
        }
    }

    private static object AddVolumeComponent(object profile, Type componentType, bool overrides)
    {
        if (profile == null || componentType == null) return null;
        var profileType = profile.GetType();

        // 优先使用非泛型 Add(Type,bool)（存在于 SRP Core 的较新版本）
        var addType = profileType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null,
            new[] { typeof(Type), typeof(bool) }, null);
        if (addType != null)
        {
            return addType.Invoke(profile, new object[] { componentType, overrides });
        }

        // 兼容旧版本：使用 Add<T>(bool) 的泛型反射
        MethodInfo genericAdd = null;
        var methods = profileType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        for (int i = 0; i < methods.Length; i++)
        {
            var m = methods[i];
            if (m.Name != "Add" || !m.IsGenericMethodDefinition) continue;
            var ps = m.GetParameters();
            if (ps.Length == 1 && ps[0].ParameterType == typeof(bool))
            {
                genericAdd = m;
                break;
            }
        }
        if (genericAdd == null) return null;

        var closed = genericAdd.MakeGenericMethod(componentType);
        return closed.Invoke(profile, new object[] { overrides });
    }

    private static void TrySetMember(object obj, string memberName, object value)
    {
        if (obj == null || string.IsNullOrEmpty(memberName)) return;
        var t = obj.GetType();
        var f = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
        if (f != null)
        {
            if (value == null || f.FieldType.IsInstanceOfType(value))
            {
                f.SetValue(obj, value);
                return;
            }
            // try numeric conversion
            try { f.SetValue(obj, Convert.ChangeType(value, f.FieldType)); } catch { }
            return;
        }
        var p = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
        if (p != null && p.CanWrite)
        {
            if (value == null || p.PropertyType.IsInstanceOfType(value))
            {
                p.SetValue(obj, value, null);
                return;
            }
            try { p.SetValue(obj, Convert.ChangeType(value, p.PropertyType), null); } catch { }
        }
    }

    private static void SetVolumeParameterFloatOverride(object volumeComponent, string memberName, float value)
    {
        if (volumeComponent == null || string.IsNullOrEmpty(memberName)) return;
        var t = volumeComponent.GetType();

        // VolumeComponent 通常暴露的是 public field：例如 Bloom.intensity / Vignette.intensity
        object paramObj = null;
        var f = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
        if (f != null) paramObj = f.GetValue(volumeComponent);
        else
        {
            var p = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (p != null) paramObj = p.GetValue(volumeComponent, null);
        }
        if (paramObj == null) return;

        // 参数类型一般有 Override(float)
        var overrideMethod = paramObj.GetType().GetMethod("Override", BindingFlags.Instance | BindingFlags.Public, null,
            new[] { typeof(float) }, null);
        if (overrideMethod != null)
        {
            overrideMethod.Invoke(paramObj, new object[] { value });
            return;
        }

        // 兜底：如果没有 Override，就尝试写入 value 字段/属性
        var valueField = paramObj.GetType().GetField("value", BindingFlags.Instance | BindingFlags.Public);
        if (valueField != null && valueField.FieldType == typeof(float))
        {
            valueField.SetValue(paramObj, value);
            return;
        }
        var valueProp = paramObj.GetType().GetProperty("value", BindingFlags.Instance | BindingFlags.Public);
        if (valueProp != null && valueProp.PropertyType == typeof(float) && valueProp.CanWrite)
        {
            valueProp.SetValue(paramObj, value, null);
        }
    }

    private void BuildBoundaryCliffs(Transform parent, float half, int layer)
    {
        var root = new GameObject("_BoundaryCliffs");
        root.transform.SetParent(parent, true);

        // Break long edges into segments to avoid "one big wall" low-poly look.
        int seg = Mathf.Clamp(Mathf.RoundToInt(half / 6f), 8, 18);
        float t = 2.2f;
        float depth = 6.5f;
        float minH = 3.6f;
        float maxH = 7.2f;

        var rng = new System.Random(2027);

        // North/South along X
        for (int i = 0; i < seg; i++)
        {
            float u0 = (i / (float)seg) * 2f - 1f;
            float u1 = ((i + 1) / (float)seg) * 2f - 1f;
            float x0 = u0 * half;
            float x1 = u1 * half;
            float cx = (x0 + x1) * 0.5f;
            float sx = Mathf.Abs(x1 - x0) + 0.2f;
            float h = Mathf.Lerp(minH, maxH, (float)rng.NextDouble());

            CreateCliff(root.transform, $"Cliff_N_{i}", new Vector3(cx, h * 0.5f, half + depth * 0.5f), new Vector3(sx, h, depth), layer);
            CreateCliff(root.transform, $"Cliff_S_{i}", new Vector3(cx, h * 0.5f, -half - depth * 0.5f), new Vector3(sx, h, depth), layer);
        }

        // East/West along Z
        for (int i = 0; i < seg; i++)
        {
            float u0 = (i / (float)seg) * 2f - 1f;
            float u1 = ((i + 1) / (float)seg) * 2f - 1f;
            float z0 = u0 * half;
            float z1 = u1 * half;
            float cz = (z0 + z1) * 0.5f;
            float sz = Mathf.Abs(z1 - z0) + 0.2f;
            float h = Mathf.Lerp(minH, maxH, (float)rng.NextDouble());

            CreateCliff(root.transform, $"Cliff_E_{i}", new Vector3(half + depth * 0.5f, h * 0.5f, cz), new Vector3(depth, h, sz), layer);
            CreateCliff(root.transform, $"Cliff_W_{i}", new Vector3(-half - depth * 0.5f, h * 0.5f, cz), new Vector3(depth, h, sz), layer);
        }
    }

    private void CreateCliff(Transform parent, string name, Vector3 pos, Vector3 scale, int layer)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.isStatic = true;
        SetLayerRecursively(go, layer);
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = rockMat;
    }

    private static void EnsureSpawnPoints(Transform parent, Vector3 baseA, Vector3 baseB)
    {
        var root = new GameObject("_SpawnPoints");
        root.transform.SetParent(parent, true);

        CreatePoint(root.transform, "SP_01", new Vector3(-25f, 0f, -10f));
        CreatePoint(root.transform, "SP_02", new Vector3(-10f, 0f, -25f));
        CreatePoint(root.transform, "SP_03", new Vector3(25f, 0f, 10f));
        CreatePoint(root.transform, "SP_04", new Vector3(10f, 0f, 25f));
        CreatePoint(root.transform, "SP_BOSS", new Vector3(0f, 0f, 0f));
    }

    private static void CreatePoint(Transform parent, string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
    }

    private void BindSpawnPointsToSpawner()
    {
        var spawner = FindObjectOfType<MonsterSpawner>();
        if (spawner == null) return;

        // 只在未配置的情况下做兜底绑定，避免覆盖手工配置
        if (spawner.spawnPoints != null && spawner.spawnPoints.Length > 0) return;

        var pointsRoot = GameObject.Find(RootName + "/_SpawnPoints");
        if (pointsRoot == null) pointsRoot = GameObject.Find("_SpawnPoints");
        if (pointsRoot == null) return;

        var ts = pointsRoot.GetComponentsInChildren<Transform>(true);
        var list = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < ts.Length; i++)
        {
            var t = ts[i];
            if (t == null || t == pointsRoot.transform) continue;
            if (t.name.StartsWith("SP_", System.StringComparison.OrdinalIgnoreCase))
                list.Add(t);
        }

        spawner.spawnPoints = list.ToArray();
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null) return;
        var stack = new System.Collections.Generic.Stack<Transform>();
        stack.Push(go.transform);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (t == null) continue;
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++)
                stack.Push(t.GetChild(i));
        }
    }
}

