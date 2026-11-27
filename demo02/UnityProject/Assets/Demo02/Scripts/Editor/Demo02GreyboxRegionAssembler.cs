using Demo02.Core;
using Demo02.Interaction;
using Demo02.WorldState;
using UnityEngine;

namespace Demo02.Editor
{
    /// <summary>
    /// Builds terrain, architecture, roads, areas and travel portals.
    /// </summary>
    internal static class Demo02GreyboxRegionAssembler
    {
        public static void BuildOfficialStreet(Transform root)
        {
            GameObject ground =
                Demo02SceneObjectFactory.CreateBlock(
                    root,
                    "Ground",
                    new Vector3(0f, -0.5f, 5f),
                    new Vector3(20f, 1f, 54f),
                    new Color(0.55f, 0.52f, 0.46f)
                );
            ground.isStatic = true;
            CreateStonePaving(
                root,
                "OfficialStreetPaving",
                new Vector3(0f, 0f, 5f),
                19f,
                52f,
                new Color(0.46f, 0.43f, 0.38f)
            );

            float[] buildingZPositions =
                { -14f, -4f, 6f, 16f, 26f };
            foreach (float z in buildingZPositions)
            {
                GameObject left = Demo02SceneObjectFactory.CreateBlock(
                    root,
                    $"LeftBuilding_{z:0}",
                    new Vector3(-9f, 3f, z),
                    new Vector3(6f, 6f, 8f),
                    new Color(0.23f, 0.28f, 0.34f)
                );
                GameObject right = null;
                if (!Mathf.Approximately(z, 6f))
                {
                    right = Demo02SceneObjectFactory.CreateBlock(
                        root,
                        $"RightBuilding_{z:0}",
                        new Vector3(9f, 3f, z),
                        new Vector3(6f, 6f, 8f),
                        new Color(0.3f, 0.27f, 0.32f)
                    );
                }
                left.isStatic = true;
                if (right != null)
                {
                    right.isStatic = true;
                }

                int styleIndex =
                    Mathf.RoundToInt((z + 14f) / 10f);
                CreateShopFacadeDetails(
                    root,
                    $"LeftBuilding_{z:0}",
                    left.transform.position,
                    new Vector3(6f, 6f, 8f),
                    facadeDirection: 1f,
                    styleIndex: styleIndex,
                    includeAwning: z > 10f
                );
                if (right != null)
                {
                    CreateShopFacadeDetails(
                        root,
                        $"RightBuilding_{z:0}",
                        right.transform.position,
                        new Vector3(6f, 6f, 8f),
                        facadeDirection: -1f,
                        styleIndex: styleIndex + 1,
                        includeAwning: z > 10f
                    );
                }
            }

            GameObject endWall =
                Demo02SceneObjectFactory.CreateBlock(
                    root,
                    "EndWall",
                    new Vector3(0f, 2f, 33f),
                    new Vector3(18f, 4f, 2f),
                    new Color(0.36f, 0.3f, 0.28f)
                );
            endWall.isStatic = true;

            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Crate_A",
                new Vector3(4.3f, 0.75f, -2f),
                new Vector3(1.5f, 1.5f, 1.5f),
                new Color(0.45f, 0.3f, 0.17f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Crate_B",
                new Vector3(-4.3f, 1f, 14f),
                new Vector3(2f, 2f, 2f),
                new Color(0.45f, 0.3f, 0.17f)
            );

            BuildWarehouseCourtyard(root);
            Demo02SceneObjectFactory.CreateWorldArea(
                root,
                "Area_OfficialStreet",
                new Vector3(0f, 1f, -8f),
                new Vector3(13f, 3f, 16f),
                "official-street",
                "官署前街"
            );
            Demo02SceneObjectFactory.CreateWorldArea(
                root,
                "Area_SaltStreet",
                new Vector3(0f, 1f, 6f),
                new Vector3(9f, 3f, 12f),
                "salt-street",
                "盐仓横街"
            );
            Demo02SceneObjectFactory.CreateWorldArea(
                root,
                "Area_TideAlley",
                new Vector3(0f, 1f, 22f),
                new Vector3(9f, 3f, 20f),
                "tide-alley",
                "回潮巷"
            );
        }

        public static void BuildCottageCourtyard(
            Transform root,
            Transform mainSpawn)
        {
            GameObject courtyard =
                DownloadedMapArtSetup.InstantiateCottageCourtyard(
                    root,
                    out Transform courtyardSpawn
                );
            Demo02SceneObjectFactory.CreateWorldArea(
                root,
                "Area_CottageCourtyard",
                new Vector3(0f, 1f, -100f),
                new Vector3(26f, 5f, 26f),
                "cottage-courtyard",
                "林间小屋"
            );

            GameObject entrance = Demo02SceneObjectFactory.CreateBlock(
                root,
                "Gate_CottageCourtyard",
                new Vector3(-5.95f, 1.2f, -4f),
                new Vector3(0.25f, 2.4f, 1.2f),
                new Color(0.32f, 0.58f, 0.36f)
            );
            entrance.AddComponent<LocalWorldPortal>().Configure(
                courtyardSpawn,
                "cottage-courtyard",
                "林间小屋",
                "E  前往林间小屋"
            );

            GameObject exit = Demo02SceneObjectFactory.CreateBlock(
                courtyard.transform,
                "Gate_ReturnFromCottage",
                new Vector3(
                    courtyardSpawn.position.x - 4f,
                    1.2f,
                    courtyardSpawn.position.z + 1f
                ),
                new Vector3(0.45f, 2.2f, 0.25f),
                new Color(0.72f, 0.54f, 0.24f)
            );
            exit.AddComponent<LocalWorldPortal>().Configure(
                mainSpawn,
                "official-street",
                "官署前街",
                "E  返回官署前街"
            );
        }

        public static void CreateMapReadabilityDressing(Transform root)
        {
            Transform group =
                new GameObject("MapReadabilityDressing").transform;
            group.SetParent(root, false);

            Color roadEdge = new(0.30f, 0.29f, 0.26f);
            Color walkway = new(0.59f, 0.55f, 0.47f);
            CreateStreetEdgeRun(
                group,
                "OfficialStreet",
                centerX: 0f,
                centerZ: 5f,
                halfWidth: 5.8f,
                length: 52f,
                roadEdge,
                walkway
            );
            CreateStreetEdgeRun(
                group,
                "DongguanStreet",
                centerX: 60f,
                centerZ: 58f,
                halfWidth: 5.05f,
                length: 154f,
                roadEdge,
                walkway
            );
            CreateStreetEdgeRun(
                group,
                "CaiyiStreet",
                centerX: 60f,
                centerZ: 225f,
                halfWidth: 4.95f,
                length: 128f,
                roadEdge,
                new Color(0.54f, 0.54f, 0.5f)
            );

            Demo02SceneObjectFactory.CreateBlock(
                group,
                "OfficialStreet_PlazaInlay",
                new Vector3(0f, 0.025f, 6f),
                new Vector3(6.6f, 0.035f, 5.2f),
                new Color(0.52f, 0.49f, 0.42f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                group,
                "CourtyardGate_Left",
                new Vector3(6.15f, 2.2f, 3f),
                new Vector3(0.45f, 4.4f, 0.45f),
                new Color(0.34f, 0.24f, 0.15f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                group,
                "CourtyardGate_Right",
                new Vector3(6.15f, 2.2f, 9f),
                new Vector3(0.45f, 4.4f, 0.45f),
                new Color(0.34f, 0.24f, 0.15f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                group,
                "CourtyardGate_Beam",
                new Vector3(6.15f, 4.55f, 6f),
                new Vector3(0.5f, 0.65f, 6.45f),
                new Color(0.42f, 0.3f, 0.18f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                group,
                "OfficialStreet_EndGate_Left",
                new Vector3(-5.1f, 2.45f, 31.3f),
                new Vector3(0.55f, 4.9f, 0.65f),
                new Color(0.32f, 0.26f, 0.2f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                group,
                "OfficialStreet_EndGate_Right",
                new Vector3(5.1f, 2.45f, 31.3f),
                new Vector3(0.55f, 4.9f, 0.65f),
                new Color(0.32f, 0.26f, 0.2f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                group,
                "OfficialStreet_EndGate_Beam",
                new Vector3(0f, 4.8f, 31.3f),
                new Vector3(10.75f, 0.7f, 0.72f),
                new Color(0.44f, 0.33f, 0.2f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                group,
                "OfficialStreet_EndGate_Plaque",
                new Vector3(0f, 5.28f, 31.2f),
                new Vector3(3.8f, 0.55f, 0.24f),
                new Color(0.78f, 0.59f, 0.25f)
            );
        }

        private static void CreateStreetEdgeRun(
            Transform parent,
            string streetName,
            float centerX,
            float centerZ,
            float halfWidth,
            float length,
            Color roadEdge,
            Color walkway)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float edgeX = centerX + side * halfWidth;
                float walkX = centerX + side * (halfWidth + 0.22f);
                Demo02SceneObjectFactory.CreateBlock(
                    parent,
                    $"{streetName}_Curb_{(side < 0 ? "Left" : "Right")}",
                    new Vector3(edgeX, 0.055f, centerZ),
                    new Vector3(0.14f, 0.11f, length),
                    roadEdge
                );
                Demo02SceneObjectFactory.CreateBlock(
                    parent,
                    $"{streetName}_Walk_{(side < 0 ? "Left" : "Right")}",
                    new Vector3(walkX, 0.025f, centerZ),
                    new Vector3(0.34f, 0.05f, length),
                    walkway
                );
            }
        }

        private static void BuildWarehouseCourtyard(Transform root)
        {
            Demo02SceneObjectFactory.CreateBlock(
                root,
                "CourtyardGround",
                new Vector3(18f, -0.55f, 6f),
                new Vector3(24f, 1f, 20f),
                new Color(0.48f, 0.44f, 0.36f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Courtyard_NorthWall",
                new Vector3(18f, 2f, -4f),
                new Vector3(24f, 4f, 2f),
                new Color(0.24f, 0.28f, 0.3f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Courtyard_SouthWall",
                new Vector3(18f, 2f, 16f),
                new Vector3(24f, 4f, 2f),
                new Color(0.24f, 0.28f, 0.3f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Courtyard_EndWall",
                new Vector3(30f, 2f, 6f),
                new Vector3(2f, 4f, 20f),
                new Color(0.3f, 0.27f, 0.25f)
            );
            BuildWarehouseInterior(root);
            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Courtyard_Crate_A",
                new Vector3(12f, 0.75f, 2f),
                new Vector3(1.5f, 1.5f, 1.5f),
                new Color(0.5f, 0.32f, 0.17f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Courtyard_Crate_B",
                new Vector3(14f, 1f, 10f),
                new Vector3(2f, 2f, 2f),
                new Color(0.5f, 0.32f, 0.17f)
            );
            Demo02SceneObjectFactory.CreateWorldArea(
                root,
                "Area_WarehouseCourtyard",
                new Vector3(12f, 1f, 6f),
                new Vector3(10f, 3f, 18f),
                "warehouse-courtyard",
                "旧仓内院"
            );
        }

        private static void BuildWarehouseInterior(Transform root)
        {
            Color wallColor = new(0.28f, 0.34f, 0.36f);
            Color woodColor = new(0.45f, 0.3f, 0.18f);

            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Warehouse_Floor",
                new Vector3(23f, 0.03f, 9f),
                new Vector3(10f, 0.12f, 7f),
                new Color(0.38f, 0.31f, 0.23f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Warehouse_NorthWall",
                new Vector3(23f, 2.5f, 12.5f),
                new Vector3(10f, 5f, 0.4f),
                wallColor
            );
            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Warehouse_SouthWall",
                new Vector3(23f, 2.5f, 5.5f),
                new Vector3(10f, 5f, 0.4f),
                wallColor
            );
            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Warehouse_EastWall",
                new Vector3(28f, 2.5f, 9f),
                new Vector3(0.4f, 5f, 7f),
                wallColor
            );
            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Warehouse_WestWall_North",
                new Vector3(18f, 2.5f, 11.5f),
                new Vector3(0.4f, 5f, 2.4f),
                wallColor
            );
            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Warehouse_WestWall_South",
                new Vector3(18f, 2.5f, 6.2f),
                new Vector3(0.4f, 5f, 1.6f),
                wallColor
            );
            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Warehouse_DoorBeam",
                new Vector3(18f, 4.4f, 8.8f),
                new Vector3(0.5f, 0.7f, 3f),
                woodColor
            );
            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Warehouse_Shelf_A",
                new Vector3(25.5f, 1.3f, 10.8f),
                new Vector3(3.5f, 2.6f, 0.8f),
                woodColor
            );
            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Warehouse_Shelf_B",
                new Vector3(21f, 1.1f, 7f),
                new Vector3(2.8f, 2.2f, 0.8f),
                woodColor
            );
            Demo02SceneObjectFactory.CreateBlock(
                root,
                "Warehouse_Table",
                new Vector3(23f, 0.6f, 9f),
                new Vector3(2.4f, 0.8f, 1.4f),
                new Color(0.5f, 0.34f, 0.2f)
            );
            Demo02SceneObjectFactory.CreateWorldArea(
                root,
                "Area_WarehouseInterior",
                new Vector3(23f, 1f, 9f),
                new Vector3(8f, 3f, 5.5f),
                "warehouse-interior",
                "旧仓室内"
            );
        }

        public static void BuildDongguanStreet(
            Transform root,
            Transform guanglingSpawn,
            out Transform areaRoot,
            out Transform dongguanSpawn)
        {
            areaRoot = new GameObject("DongguanStreet").transform;
            areaRoot.SetParent(root);

            Demo02SceneObjectFactory.CreateBlock(
                areaRoot,
                "Dongguan_Ground",
                new Vector3(60f, -0.55f, 58f),
                new Vector3(20f, 1f, 160f),
                new Color(0.52f, 0.48f, 0.4f)
            );
            CreateStonePaving(
                areaRoot,
                "DongguanPaving",
                new Vector3(60f, 0f, 58f),
                19f,
                158f,
                new Color(0.43f, 0.4f, 0.35f)
            );

            float[] shopPositions =
            {
                -12f, 0f, 12f, 24f, 36f, 48f,
                60f, 72f, 84f, 96f, 108f, 120f
            };
            for (int index = 0; index < shopPositions.Length; index++)
            {
                float z = shopPositions[index];
                Color leftColor = index % 2 == 0
                    ? new Color(0.38f, 0.25f, 0.2f)
                    : new Color(0.42f, 0.31f, 0.22f);
                Color rightColor = index % 2 == 0
                    ? new Color(0.29f, 0.3f, 0.27f)
                    : new Color(0.36f, 0.24f, 0.24f);

                Demo02SceneObjectFactory.CreateBlock(
                    areaRoot,
                    $"Dongguan_LeftShop_{index + 1}",
                    new Vector3(52f, 3f, z),
                    new Vector3(6f, 6f, 8f),
                    leftColor
                );
                Demo02SceneObjectFactory.CreateBlock(
                    areaRoot,
                    $"Dongguan_RightShop_{index + 1}",
                    new Vector3(68f, 3f, z),
                    new Vector3(6f, 6f, 8f),
                    rightColor
                );
                CreateShopFacadeDetails(
                    areaRoot,
                    $"Dongguan_LeftShop_{index + 1}",
                    new Vector3(52f, 3f, z),
                    new Vector3(6f, 6f, 8f),
                    facadeDirection: 1f,
                    styleIndex: index
                );
                CreateShopFacadeDetails(
                    areaRoot,
                    $"Dongguan_RightShop_{index + 1}",
                    new Vector3(68f, 3f, z),
                    new Vector3(6f, 6f, 8f),
                    facadeDirection: -1f,
                    styleIndex: index + 1
                );
            }

            Demo02SceneObjectFactory.CreateBlock(
                areaRoot,
                "Dongguan_EndWall",
                new Vector3(60f, 2f, 135f),
                new Vector3(18f, 4f, 2f),
                new Color(0.36f, 0.27f, 0.22f)
            );
            CreateDongguanArches(areaRoot);
            DownloadedMapArtSetup.InstantiateDongguanPavilion(areaRoot);
            DownloadedMapArtSetup.InstantiateDongguanTreeLine(areaRoot);
            Demo02SceneObjectFactory.CreateBlock(
                areaRoot,
                "Dongguan_Well",
                new Vector3(56.9f, 0.55f, 59f),
                new Vector3(1.8f, 1.1f, 1.8f),
                new Color(0.34f, 0.4f, 0.42f)
            );
            CreateDongguanStalls(areaRoot);

            GameObject dongguanSpawnObject = new("DongguanSpawn");
            dongguanSpawnObject.transform.SetParent(areaRoot);
            dongguanSpawnObject.transform.position =
                new Vector3(60f, 0f, -16f);
            WorldSpawnPoint dongguanAnchor =
                dongguanSpawnObject.AddComponent<WorldSpawnPoint>();
            dongguanAnchor.Configure("dongguan_start");
            dongguanAnchor.ConfigureAsAnchor("东关街", false);
            dongguanSpawn = dongguanSpawnObject.transform;
            Demo02SceneObjectFactory.CreateWorldAnchor(
                areaRoot,
                "Anchor_DongguanStreet",
                dongguanAnchor,
                new Color(0.31f, 0.78f, 0.72f)
            );
            Demo02GreyboxGameplayAssembler.CreateStreamingRegion(
                areaRoot,
                "dongguan-patrol"
            );
            Demo02SceneObjectFactory.CreateWorldArea(
                areaRoot,
                "Area_DongguanStreet",
                new Vector3(60f, 1f, 56f),
                new Vector3(10f, 3f, 154f),
                "dongguan-street",
                "东关街"
            );

            GameObject gateway = Demo02SceneObjectFactory.CreateBlock(
                root,
                "Gate_ToDongguan",
                new Vector3(-4.7f, 1.2f, -14f),
                new Vector3(1.5f, 2.4f, 0.35f),
                new Color(0.86f, 0.62f, 0.22f)
            );
            gateway.transform.rotation =
                Quaternion.Euler(0f, 90f, 0f);
            gateway.AddComponent<LocalWorldPortal>().Configure(
                dongguanSpawn,
                "dongguan-street",
                "东关街",
                "E  前往东关街"
            );

            GameObject returnGate =
                Demo02SceneObjectFactory.CreateBlock(
                    areaRoot,
                    "Gate_ReturnGuangling",
                    new Vector3(60f, 1.2f, -21f),
                    new Vector3(1.5f, 2.4f, 0.35f),
                    new Color(0.8f, 0.55f, 0.2f)
                );
            returnGate.AddComponent<LocalWorldPortal>().Configure(
                guanglingSpawn,
                "official-street",
                "官署前街",
                "E  返回官署前街"
            );
        }

        public static void BuildCaiyiStreet(
            Transform root,
            Transform dongguanSpawn,
            out Transform areaRoot,
            out Transform caiyiSpawn)
        {
            areaRoot = new GameObject("CaiyiStreet").transform;
            areaRoot.SetParent(root);

            Demo02SceneObjectFactory.CreateBlock(
                areaRoot,
                "Caiyi_Ground",
                new Vector3(60f, -0.56f, 225f),
                new Vector3(18f, 1f, 130f),
                new Color(0.4f, 0.42f, 0.43f)
            );
            CreateStonePaving(
                areaRoot,
                "CaiyiPaving",
                new Vector3(60f, 0.005f, 225f),
                17f,
                128f,
                new Color(0.35f, 0.36f, 0.36f)
            );

            float[] shopPositions =
            {
                170f, 181f, 192f, 203f, 214f, 225f,
                236f, 247f, 258f, 269f, 280f
            };
            for (int index = 0; index < shopPositions.Length; index++)
            {
                float z = shopPositions[index];
                float height = 4.5f + (index % 3) * 0.8f;
                Color leftWall = index % 2 == 0
                    ? new Color(0.58f, 0.55f, 0.5f)
                    : new Color(0.5f, 0.45f, 0.4f);
                Color rightWall = index % 2 == 0
                    ? new Color(0.5f, 0.42f, 0.38f)
                    : new Color(0.57f, 0.52f, 0.46f);

                Demo02SceneObjectFactory.CreateBlock(
                    areaRoot,
                    $"Caiyi_LeftShop_{index + 1}",
                    new Vector3(52.5f, height * 0.5f, z),
                    new Vector3(5f, height, 9f),
                    leftWall
                );
                Demo02SceneObjectFactory.CreateBlock(
                    areaRoot,
                    $"Caiyi_RightShop_{index + 1}",
                    new Vector3(67.5f, height * 0.5f, z),
                    new Vector3(5f, height, 9f),
                    rightWall
                );
                Demo02SceneObjectFactory.CreateBlock(
                    areaRoot,
                    $"Caiyi_LeftAwning_{index + 1}",
                    new Vector3(55.8f, 2.9f, z),
                    new Vector3(2.2f, 0.18f, 3.2f),
                    index % 2 == 0
                        ? new Color(0.72f, 0.22f, 0.18f)
                        : new Color(0.22f, 0.48f, 0.5f)
                );
                Demo02SceneObjectFactory.CreateBlock(
                    areaRoot,
                    $"Caiyi_RightAwning_{index + 1}",
                    new Vector3(64.2f, 2.9f, z),
                    new Vector3(2.2f, 0.18f, 3.2f),
                    index % 2 == 0
                        ? new Color(0.68f, 0.46f, 0.16f)
                        : new Color(0.56f, 0.22f, 0.32f)
                );
                CreateShopFacadeDetails(
                    areaRoot,
                    $"Caiyi_LeftShop_{index + 1}",
                    new Vector3(52.5f, height * 0.5f, z),
                    new Vector3(5f, height, 9f),
                    facadeDirection: 1f,
                    styleIndex: index + 1
                );
                CreateShopFacadeDetails(
                    areaRoot,
                    $"Caiyi_RightShop_{index + 1}",
                    new Vector3(67.5f, height * 0.5f, z),
                    new Vector3(5f, height, 9f),
                    facadeDirection: -1f,
                    styleIndex: index + 2
                );
            }

            for (int index = 0; index < 8; index++)
            {
                float z = 174f + index * 15f;
                CreateCaiyiTree(
                    areaRoot,
                    $"Caiyi_Tree_{index + 1}",
                    new Vector3(
                        index % 2 == 0 ? 55.2f : 64.8f,
                        0f,
                        z
                    )
                );
            }
            for (int index = 0; index < 6; index++)
            {
                Demo02SceneObjectFactory.CreateBlock(
                    areaRoot,
                    $"Caiyi_CrosswalkStart_{index + 1}",
                    new Vector3(56.2f + index * 1.5f, 0.02f, 170f),
                    new Vector3(0.8f, 0.04f, 4.2f),
                    new Color(0.85f, 0.85f, 0.78f)
                );
            }
            Demo02SceneObjectFactory.CreateBlock(
                areaRoot,
                "Caiyi_EndWall",
                new Vector3(60f, 2f, 290f),
                new Vector3(18f, 4f, 2f),
                new Color(0.32f, 0.29f, 0.26f)
            );

            GameObject caiyiSpawnObject = new("CaiyiSpawn");
            caiyiSpawnObject.transform.SetParent(areaRoot);
            caiyiSpawnObject.transform.position =
                new Vector3(60f, 0f, 166f);
            WorldSpawnPoint caiyiAnchor =
                caiyiSpawnObject.AddComponent<WorldSpawnPoint>();
            caiyiAnchor.Configure("caiyi_start");
            caiyiAnchor.ConfigureAsAnchor("彩衣街", false);
            caiyiSpawn = caiyiSpawnObject.transform;
            Demo02SceneObjectFactory.CreateWorldAnchor(
                areaRoot,
                "Anchor_CaiyiStreet",
                caiyiAnchor,
                new Color(0.78f, 0.38f, 0.24f)
            );
            Demo02GreyboxGameplayAssembler.CreateStreamingRegion(
                areaRoot,
                "caiyi-lookout"
            );
            Demo02SceneObjectFactory.CreateWorldArea(
                areaRoot,
                "Area_CaiyiStreet",
                new Vector3(60f, 1f, 230f),
                new Vector3(10f, 3f, 120f),
                "caiyi-street",
                "彩衣街"
            );

            GameObject caiyiGate = Demo02SceneObjectFactory.CreateBlock(
                areaRoot,
                "Gate_ReturnDongguan",
                new Vector3(60f, 1.2f, 162f),
                new Vector3(1.5f, 2.4f, 0.35f),
                new Color(0.55f, 0.32f, 0.18f)
            );
            caiyiGate.AddComponent<LocalWorldPortal>().Configure(
                dongguanSpawn,
                "dongguan-street",
                "东关街",
                "E  返回东关街"
            );

            GameObject dongguanToCaiyi =
                Demo02SceneObjectFactory.CreateBlock(
                    root,
                    "Gate_DongguanToCaiyi",
                    new Vector3(64.5f, 1.2f, -17.5f),
                    new Vector3(1.5f, 2.4f, 0.35f),
                    new Color(0.85f, 0.32f, 0.2f)
                );
            dongguanToCaiyi.transform.rotation =
                Quaternion.Euler(0f, -90f, 0f);
            dongguanToCaiyi
                .AddComponent<LocalWorldPortal>()
                .Configure(
                    caiyiSpawn,
                    "caiyi-street",
                    "彩衣街",
                    "E  前往彩衣街"
                );
        }

        private static void CreateDongguanArches(Transform areaRoot)
        {
            Demo02SceneObjectFactory.CreateBlock(
                areaRoot,
                "Dongguan_Arch_Left",
                new Vector3(56.5f, 2f, -20f),
                new Vector3(0.8f, 4f, 0.8f),
                new Color(0.5f, 0.32f, 0.18f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                areaRoot,
                "Dongguan_Arch_Right",
                new Vector3(63.5f, 2f, -20f),
                new Vector3(0.8f, 4f, 0.8f),
                new Color(0.5f, 0.32f, 0.18f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                areaRoot,
                "Dongguan_Arch_Beam",
                new Vector3(60f, 4.1f, -20f),
                new Vector3(7.8f, 0.8f, 0.8f),
                new Color(0.62f, 0.42f, 0.2f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                areaRoot,
                "Dongguan_MidArch_Left",
                new Vector3(56.5f, 2f, 58f),
                new Vector3(0.7f, 4f, 0.7f),
                new Color(0.62f, 0.4f, 0.2f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                areaRoot,
                "Dongguan_MidArch_Right",
                new Vector3(63.5f, 2f, 58f),
                new Vector3(0.7f, 4f, 0.7f),
                new Color(0.62f, 0.4f, 0.2f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                areaRoot,
                "Dongguan_MidArch_Beam",
                new Vector3(60f, 4f, 58f),
                new Vector3(7.8f, 0.7f, 0.7f),
                new Color(0.72f, 0.5f, 0.24f)
            );
        }

        private static void CreateDongguanStalls(Transform areaRoot)
        {
            Demo02SceneObjectFactory.CreateBlock(
                areaRoot,
                "Dongguan_Stall_A",
                new Vector3(57f, 0.7f, 8f),
                new Vector3(3f, 1.4f, 2f),
                new Color(0.7f, 0.35f, 0.22f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                areaRoot,
                "Dongguan_Stall_B",
                new Vector3(63f, 0.7f, 38f),
                new Vector3(3f, 1.4f, 2f),
                new Color(0.25f, 0.5f, 0.48f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                areaRoot,
                "Dongguan_Stall_C",
                new Vector3(57f, 0.7f, 84f),
                new Vector3(3f, 1.4f, 2f),
                new Color(0.4f, 0.54f, 0.3f)
            );
            Demo02SceneObjectFactory.CreateBlock(
                areaRoot,
                "Dongguan_Stall_D",
                new Vector3(63f, 0.7f, 112f),
                new Vector3(3f, 1.4f, 2f),
                new Color(0.66f, 0.38f, 0.18f)
            );
        }

        private static void CreateCaiyiTree(
            Transform parent,
            string name,
            Vector3 position)
        {
            Demo02SceneObjectFactory.CreateDecorBlock(
                parent,
                $"{name}_Trunk",
                position + Vector3.up * 1.4f,
                new Vector3(0.35f, 1.4f, 0.35f),
                new Color(0.3f, 0.2f, 0.12f),
                PrimitiveType.Cylinder
            );
            Demo02SceneObjectFactory.CreateDecorBlock(
                parent,
                $"{name}_Canopy",
                position + Vector3.up * 3.5f,
                new Vector3(3.4f, 1.8f, 3.4f),
                new Color(0.18f, 0.42f, 0.2f),
                PrimitiveType.Sphere
            );
        }

        private static void CreateShopFacadeDetails(
            Transform parent,
            string namePrefix,
            Vector3 center,
            Vector3 size,
            float facadeDirection,
            int styleIndex,
            bool includeAwning = true)
        {
            Color roofColor = new(0.11f, 0.12f, 0.125f);
            Color woodColor = new(0.25f, 0.16f, 0.1f);
            Color doorColor = new(0.16f, 0.105f, 0.075f);
            Color lanternColor = new(0.72f, 0.105f, 0.075f);
            Color lanternCap = new(0.82f, 0.58f, 0.18f);
            Color[] bannerColors =
            {
                new Color(0.64f, 0.16f, 0.12f),
                new Color(0.16f, 0.42f, 0.43f),
                new Color(0.68f, 0.46f, 0.16f)
            };
            Color bannerColor =
                bannerColors[styleIndex % bannerColors.Length];
            float frontX =
                center.x + facadeDirection * size.x * 0.5f;
            float roofY = center.y + size.y * 0.5f + 0.28f;

            GameObject leftRoof =
                Demo02SceneObjectFactory.CreateDecorBlock(
                    parent,
                    $"{namePrefix}_RoofLeft",
                    new Vector3(
                        center.x - size.x * 0.24f,
                        roofY + 0.22f,
                        center.z
                    ),
                    new Vector3(
                        size.x * 0.64f,
                        0.18f,
                        size.z + 1.25f
                    ),
                    roofColor
                );
            leftRoof.transform.rotation =
                Quaternion.Euler(0f, 0f, 18f);
            GameObject rightRoof =
                Demo02SceneObjectFactory.CreateDecorBlock(
                    parent,
                    $"{namePrefix}_RoofRight",
                    new Vector3(
                        center.x + size.x * 0.24f,
                        roofY + 0.22f,
                        center.z
                    ),
                    new Vector3(
                        size.x * 0.64f,
                        0.18f,
                        size.z + 1.25f
                    ),
                    roofColor
                );
            rightRoof.transform.rotation =
                Quaternion.Euler(0f, 0f, -18f);
            Demo02SceneObjectFactory.CreateDecorBlock(
                parent,
                $"{namePrefix}_RoofRidge",
                new Vector3(center.x, roofY + 0.58f, center.z),
                new Vector3(0.28f, 0.22f, size.z + 1.3f),
                new Color(0.18f, 0.19f, 0.19f)
            );
            Demo02SceneObjectFactory.CreateDecorBlock(
                parent,
                $"{namePrefix}_Eave",
                new Vector3(
                    frontX + facadeDirection * 0.18f,
                    roofY - 0.08f,
                    center.z
                ),
                new Vector3(0.28f, 0.18f, size.z + 1.05f),
                woodColor
            );
            if (includeAwning)
            {
                GameObject awning =
                    Demo02SceneObjectFactory.CreateDecorBlock(
                        parent,
                        $"{namePrefix}_Awning",
                        new Vector3(
                            frontX + facadeDirection * 0.72f,
                            3.15f,
                            center.z
                        ),
                        new Vector3(1.05f, 0.13f, size.z * 0.44f),
                        bannerColor
                    );
                awning.transform.rotation = Quaternion.Euler(
                    0f,
                    0f,
                    -8f * facadeDirection
                );
                Demo02SceneObjectFactory.CreateDecorBlock(
                    parent,
                    $"{namePrefix}_AwningValance",
                    new Vector3(
                        frontX + facadeDirection * 1.16f,
                        2.92f,
                        center.z
                    ),
                    new Vector3(0.07f, 0.34f, size.z * 0.44f),
                    Color.Lerp(bannerColor, Color.black, 0.2f)
                );
            }
            Demo02SceneObjectFactory.CreateDecorBlock(
                parent,
                $"{namePrefix}_Door",
                new Vector3(
                    frontX + facadeDirection * 0.04f,
                    1.35f,
                    center.z
                ),
                new Vector3(0.14f, 2.5f, Mathf.Min(2f, size.z * 0.3f)),
                doorColor
            );
            Demo02SceneObjectFactory.CreateDecorBlock(
                parent,
                $"{namePrefix}_DoorStep",
                new Vector3(
                    frontX + facadeDirection * 0.34f,
                    0.1f,
                    center.z
                ),
                new Vector3(
                    0.75f,
                    0.2f,
                    Mathf.Min(2.5f, size.z * 0.34f)
                ),
                new Color(0.27f, 0.27f, 0.25f)
            );
            Demo02SceneObjectFactory.CreateDecorBlock(
                parent,
                $"{namePrefix}_SignBoard",
                new Vector3(
                    frontX + facadeDirection * 0.15f,
                    3.62f,
                    center.z
                ),
                new Vector3(0.12f, 0.72f, 1.45f),
                new Color(0.13f, 0.09f, 0.06f)
            );
            Demo02SceneObjectFactory.CreateDecorBlock(
                parent,
                $"{namePrefix}_SignFace",
                new Vector3(
                    frontX + facadeDirection * 0.22f,
                    3.62f,
                    center.z
                ),
                new Vector3(0.06f, 0.56f, 1.18f),
                bannerColor
            );

            foreach (float windowOffset in new[]
                     {
                         -size.z * 0.24f,
                         size.z * 0.24f
                     })
            {
                Demo02SceneObjectFactory.CreateDecorBlock(
                    parent,
                    $"{namePrefix}_Window_{windowOffset:0}",
                    new Vector3(
                        frontX + facadeDirection * 0.08f,
                        1.85f,
                        center.z + windowOffset
                    ),
                    new Vector3(0.08f, 0.92f, 1.05f),
                    new Color(0.12f, 0.16f, 0.16f)
                );
            }
            Demo02SceneObjectFactory.CreateDecorBlock(
                parent,
                $"{namePrefix}_Banner",
                new Vector3(
                    frontX + facadeDirection * 0.09f,
                    3.95f,
                    center.z + size.z * 0.31f
                ),
                new Vector3(0.08f, 1.45f, 0.72f),
                bannerColor
            );
            foreach (float zOffset in new[]
                     {
                         -size.z * 0.34f,
                         size.z * 0.34f
                     })
            {
                Demo02SceneObjectFactory.CreateDecorBlock(
                    parent,
                    $"{namePrefix}_Column_{zOffset:0}",
                    new Vector3(
                        frontX + facadeDirection * 0.12f,
                        1.55f,
                        center.z + zOffset
                    ),
                    new Vector3(0.16f, 3.1f, 0.16f),
                    woodColor
                );
            }

            float lanternZ = center.z - size.z * 0.35f;
            Demo02SceneObjectFactory.CreateDecorBlock(
                parent,
                $"{namePrefix}_Lantern",
                new Vector3(
                    frontX + facadeDirection * 0.34f,
                    2.72f,
                    lanternZ
                ),
                new Vector3(0.34f, 0.48f, 0.34f),
                lanternColor,
                PrimitiveType.Sphere
            );
            Demo02SceneObjectFactory.CreateDecorBlock(
                parent,
                $"{namePrefix}_LanternCap",
                new Vector3(
                    frontX + facadeDirection * 0.34f,
                    3f,
                    lanternZ
                ),
                new Vector3(0.24f, 0.1f, 0.24f),
                lanternCap
            );
        }

        private static void CreateStonePaving(
            Transform parent,
            string name,
            Vector3 center,
            float width,
            float length,
            Color baseColor)
        {
            int laneCount =
                Mathf.Max(3, Mathf.RoundToInt(width / 2.4f));
            float laneWidth = width / laneCount;
            for (int lane = 0; lane < laneCount; lane++)
            {
                float x = center.x
                    - width * 0.5f
                    + laneWidth * (lane + 0.5f);
                Color stoneColor = lane % 2 == 0
                    ? baseColor
                    : Color.Lerp(
                        baseColor,
                        new Color(0.23f, 0.24f, 0.23f),
                        0.16f
                    );
                Demo02SceneObjectFactory.CreateDecorBlock(
                    parent,
                    $"{name}_Lane_{lane + 1}",
                    new Vector3(x, center.y + 0.012f, center.z),
                    new Vector3(laneWidth * 0.94f, 0.024f, length),
                    stoneColor
                );
            }

            Color seamColor =
                Color.Lerp(baseColor, Color.black, 0.34f);
            for (float z = center.z - length * 0.5f + 3f;
                z < center.z + length * 0.5f;
                z += 4.5f)
            {
                Demo02SceneObjectFactory.CreateDecorBlock(
                    parent,
                    $"{name}_Seam_{z:0}",
                    new Vector3(center.x, center.y + 0.03f, z),
                    new Vector3(width * 0.96f, 0.02f, 0.055f),
                    seamColor
                );
            }
            Demo02SceneObjectFactory.CreateDecorBlock(
                parent,
                $"{name}_CurbLeft",
                new Vector3(
                    center.x - width * 0.5f + 0.2f,
                    center.y + 0.12f,
                    center.z
                ),
                new Vector3(0.38f, 0.24f, length),
                new Color(0.3f, 0.3f, 0.28f)
            );
            Demo02SceneObjectFactory.CreateDecorBlock(
                parent,
                $"{name}_CurbRight",
                new Vector3(
                    center.x + width * 0.5f - 0.2f,
                    center.y + 0.12f,
                    center.z
                ),
                new Vector3(0.38f, 0.24f, length),
                new Color(0.3f, 0.3f, 0.28f)
            );
        }
    }
}
