using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager mapManager;

    [SerializeField]
    MeshFilter terrainMesh;
    [SerializeField]
    MeshRenderer terrainMeshRenderer;

    [SerializeField]
    Texture heatMap;

    [SerializeField]
    public Vector2Int mapSize;
    [SerializeField]
    public float blockSize;
    
    Texture2D texture;


    public NativeArray<bool> map;


    public NativeArray<float3> mapPheromones;

    private void Awake()
    {
        mapManager = this;

        mapPheromones = new NativeArray<float3>(mapSize.x * mapSize.y, Allocator.Persistent);
        map = new NativeArray<bool>(mapSize.x * mapSize.y, Allocator.Persistent);

        GenerateMap();
        DrawMap();
    }

    private void Start()
    {
        texture = new Texture2D(mapSize.x, mapSize.y, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Point;
        InvokeRepeating("TextureFromPheromonMap", 0.5f, 0.1f);

        for (int i = 0; i < mapPheromones.Length; i++)
        {
            mapPheromones[i] = new float3(0,0,0);
            if(i % mapSize.x > 50 && i % mapSize.x < 55 && i / mapSize.x > 50 && i / mapSize.x < 55) mapPheromones[i] = new float3(1, 1, 1);
        }

        for (int i = 0; i < mapPheromones.Length; i++)
        {
            mapPheromones[i] = new float3(0, 0, 0);
            if (i % mapSize.x > 50 && i % mapSize.x < 55 && i / mapSize.x > 50 && i / mapSize.x < 55) mapPheromones[i] = new float3(1, 1, 1);
        }

    }
    private void OnDestroy()
    {
        mapPheromones.Dispose();
        map.Dispose();
    }

    private void Update()
    {

    }



    private void GenerateMap()
    {
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                map[x + y * mapSize.x] = true;
            }
        }


        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                if (!(x > 0 && y > 0 && x < mapSize.x - 1 && y < mapSize.y - 1)) map[x + y * mapSize.x] = false;
            }
        }
    }

    [SerializeField] bool drawCrowdness;
    [SerializeField] bool drawBlood;
    [SerializeField] bool drawHumanSmell;
    [SerializeField] bool drawWalkable;


    [BurstCompile]
    struct CalculatePheromonesSpread : IJob
    {
        public NativeArray<float3> pheromones;
        public NativeArray<float3> copyPheromones;
        [ReadOnly] public NativeArray<bool> walkable;
        public int mapSizeX;
        public int mapSizeY;
        public float3 pheromonTransmitionLoss;
        public float delta;
        public void Execute()
        {
            float3 sumPheromones = new float3(0,0,0);
            float3 sumPheromonesAfter = new float3(0, 0, 0);

            for (int x = 0; x < mapSizeX; x++)
            {
                for (int y = 0; y < mapSizeY; y++)
                {
                    if (!walkable[x + y * mapSizeX]) continue;
                    float3 p = GetPheromones(x, y);
                    pheromones[x + y * mapSizeX] = p + delta * (GetAveragePheromones(x,y) - p);
                    sumPheromones += copyPheromones[x + y * mapSizeX];
                    sumPheromonesAfter += pheromones[x + y * mapSizeX];
                }
            }

            sumPheromones /= sumPheromonesAfter;

            for (int x = 0; x < mapSizeX; x++)
            {
                for (int y = 0; y < mapSizeY; y++)
                {
                    pheromones[x + y * mapSizeX] *= sumPheromones * (1 - pheromonTransmitionLoss * delta);
                }
            }
        }
        public float3 GetAveragePheromones(int x, int y)
        {
            int neighCount = 0;
            float3 pheromones = new float3(0,0,0);

            if (x > 0 && walkable[x - 1 + y * mapSizeX]) // 
            {
                neighCount++;
                pheromones += GetPheromones(x - 1, y);
            }
            if (x < mapSizeX - 1) // 
            {
                neighCount++;
                pheromones += GetPheromones(x + 1, y);
            }
            if (y > 0 && walkable[x + (y - 1) * mapSizeX]) // 
            {
                neighCount++;
                pheromones += GetPheromones(x, y - 1);
            }
            if (y < mapSizeY - 1 && walkable[x + (y + 1) * mapSizeX]) //
            {
                neighCount++;
                pheromones += GetPheromones(x, y + 1);
            }
            return pheromones / neighCount;
        }

        public float3 GetPheromones(int x, int y)
        {
            return copyPheromones[x + y * mapSizeX];
        }
    }

    void TextureFromPheromonMap()
    {

        NativeArray<float3> copyPheromones = new NativeArray<float3>(mapPheromones, Allocator.TempJob);
        NativeArray<float3> secondCopyPheromones = new NativeArray<float3>(mapPheromones, Allocator.TempJob);
        CalculatePheromonesSpread calc = new CalculatePheromonesSpread()
        {
            pheromones = copyPheromones,
            copyPheromones = secondCopyPheromones,
            mapSizeX = mapSize.x,
            mapSizeY = mapSize.y,
            pheromonTransmitionLoss = 0.05f,
            delta = 0.1f,
            walkable = map,
        };
        calc.Run();

        for(int i = 0; i < copyPheromones.Length; i++)
        {
            mapPheromones[i] = copyPheromones[i];
        }

        copyPheromones.Dispose();
        secondCopyPheromones.Dispose();


        if (drawBlood || drawCrowdness || drawHumanSmell)
        {
            for (int x = 0; x < mapSize.x; x++)
            {
                for (int y = 0; y < mapSize.y; y++)
                {
                    Color color = new Color(0, 0, 0, 1);
                    if (drawHumanSmell)
                    {
                        color.b = mapPheromones[x + y * mapSize.y].x;
                    }
                    if (drawCrowdness)
                    {
                        color.g = mapPheromones[x + y * mapSize.y].y;
                    }
                    if (drawBlood)
                    {
                        color.r = mapPheromones[x + y * mapSize.y].z;
                    }
                    texture.SetPixel(x, y, color);
                }
            }
        }
        else if(drawWalkable)
        {
            for (int x = 0; x < mapSize.x; x++)
            {
                for (int y = 0; y < mapSize.y; y++)
                {
                    Color color = new Color(0, 0, 0, 1);
                    if (map[x + y * mapSize.x]) color = Color.clear;
                    else color = Color.red;
                    texture.SetPixel(x, y, color);
                }
            }
        }

        texture.Apply();
        heatMap = texture;
        terrainMeshRenderer.material.SetTexture("_BaseMap", texture);
    }

    public void DrawMap()
    {
        Mesh newterrainMesh = new Mesh();

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        int2 vertMapSize = new int2(mapSize.x + 1, mapSize.y + 1);

        for (int x = 0; x < vertMapSize.x; x++)
        {
            for (int y = 0; y < vertMapSize.y; y++)
            {
                verts.Add(new Vector3(x * blockSize + blockSize/2 - (vertMapSize.x * blockSize) / 2, 1, y * blockSize + blockSize / 2 - (vertMapSize.y * blockSize) / 2));
                uvs.Add(new Vector2((float)x / (vertMapSize.x - 1), (float)y / (vertMapSize.y - 1)));
            }
        }

        List<int> triangles = new List<int>();

        for (int x = 0; x < vertMapSize.x - 1; x++) 
        {
            for (int y = 0; y < vertMapSize.y - 1; y++) 
            {
                triangles.Add(y + x * vertMapSize.y);
                triangles.Add((y + 1) + x * vertMapSize.y);
                triangles.Add(y + (x + 1) * vertMapSize.y);

                triangles.Add((y + 1) + x * vertMapSize.y);
                triangles.Add((y + 1) + (x + 1)* vertMapSize.y);
                triangles.Add(y + (x + 1) * vertMapSize.y);
            }
        }

        newterrainMesh.vertices = verts.ToArray();
        newterrainMesh.triangles = triangles.ToArray();
        newterrainMesh.uv = uvs.ToArray();

        terrainMesh.mesh = newterrainMesh;

    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(-mapSize.x, 0, mapSize.y) * blockSize / 2, new Vector3(mapSize.x, 0, mapSize.y) * blockSize / 2);
        Gizmos.DrawLine(new Vector3(mapSize.x, 0, mapSize.y) * blockSize / 2, new Vector3(mapSize.x, 0, -mapSize.y) * blockSize / 2);
        Gizmos.DrawLine(new Vector3(mapSize.x, 0, -mapSize.y) * blockSize / 2, new Vector3(-mapSize.x, 0, -mapSize.y) * blockSize / 2);
        Gizmos.DrawLine(new Vector3(-mapSize.x, 0, -mapSize.y) * blockSize / 2, new Vector3(-mapSize.x, 0, mapSize.y) * blockSize / 2);
    }
}