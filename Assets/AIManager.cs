using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class AIManager : MonoBehaviour
{
    public static AIManager aIManager;
    [SerializeField]
    GameObject zombiePrefab;

    [SerializeField]
    int zombieCount;

    public struct Zombie
    {
        public float2 position;
        public int state;
        public float speed;
        public float senseStrength;
        public float radius;
    }

    List<Transform> zombieTransforms = new List<Transform>();
    public List<Zombie> zombies = new List<Zombie>();
    //NativeArray<Zombie> zombiesArray;

    TransformAccessArray m_AccessArray;

    MapManager mapManager;

    private void Awake()
    {
        aIManager = this;
    }

    private void Start()
    {
        mapManager = MapManager.mapManager;
        for (int i = 0; i < zombieCount; i++)
        {
            int2 pos;

            do
            {
                pos.x = UnityEngine.Random.Range(0, mapManager.mapSize.x);
                pos.y = UnityEngine.Random.Range(0, mapManager.mapSize.y);
            } while (!mapManager.map[pos.x + pos.y * mapManager.mapSize.x]);

            float2 spawnOffset = new float2(UnityEngine.Random.Range(0f, MapManager.mapManager.blockSize), UnityEngine.Random.Range(0f, MapManager.mapManager.blockSize)) / 2;

            float2 spawnPointy = new float2(pos.x * mapManager.blockSize + mapManager.blockSize - (mapManager.mapSize.x * mapManager.blockSize) / 2, pos.y * mapManager.blockSize + mapManager.blockSize - (mapManager.mapSize.y * mapManager.blockSize) / 2);

            Zombie zombie = new Zombie() {
                position = spawnPointy - spawnOffset,
                state = 0,
                speed = UnityEngine.Random.Range(1f, 3f),
                senseStrength = UnityEngine.Random.Range(0.1f, 1f),
                radius = 1f,
            };

            zombies.Add(zombie);
            zombieTransforms.Add(Instantiate(zombiePrefab, new Vector3(zombie.position.x, 1.5f, zombie.position.y), quaternion.identity).transform);
        }

        m_AccessArray = new TransformAccessArray(zombieTransforms.ToArray());
        //zombiesArray = new NativeArray<Zombie>(zombies.ToArray(), Allocator.Persistent);
    }
    [SerializeField] bool simulate;
    private void Update()
    {
        if (!simulate) return;
        SortZombies();
        NativeArray<Zombie> zombiesArrayNative = new NativeArray<Zombie>(zombies.ToArray(), Allocator.TempJob);
        AiDecision aiDecision = new AiDecision()
        {
            deltaTime = Time.deltaTime,
            zombies = zombiesArrayNative,
            mapPheromones = mapManager.mapPheromones,
            blockSize = mapManager.blockSize,
            mapSizeX = mapManager.mapSize.x,
            mapSizeY = mapManager.mapSize.y,
            walkable = mapManager.map,
            crowd = MapManager.mapManager.crowdedness,
        };

        JobHandle handle = aiDecision.Schedule(m_AccessArray.length, 64);
        handle.Complete();

        ZombieCollision colision = new ZombieCollision() { zombies = zombiesArrayNative, mapSizeX = mapManager.mapSize.x, mapSizeY = mapManager.mapSize.y, blockSize = mapManager.blockSize, walkable = mapManager.map };
        colision.Run();

        for (int i = 0; i < zombies.Count; i++)
        {
            zombies[i] = zombiesArrayNative[i];
            zombieTransforms[i].position = new Vector3(zombies[i].position.x, 1.5f, zombies[i].position.y);
        }
        
        zombiesArrayNative.Dispose();
    }

    private void OnDestroy()
    {
        //zombiesArray.Dispose();
        m_AccessArray.Dispose();
    }

    void SortZombies()
    {
        int i, j;

        for (i = 1; i < zombies.Count; i++)
        {
            for (j = i - 1; j >= 0; j--)
            {
                if (zombies[j].position.x - zombies[j].radius > zombies[j + 1].position.x - zombies[j + 1].radius)
                {
                    Zombie temp = zombies[j];
                    zombies[j] = zombies[j + 1];
                    zombies[j + 1] = temp;
                }
                else
                {
                    break;
                }
            }
        }
    }



    //[BurstCompile]
    struct AiDecision : IJobParallelFor
    {
        [ReadOnly] public float deltaTime;
        [ReadOnly] public NativeArray<float3> mapPheromones;
        [ReadOnly] public NativeArray<bool> walkable;
        [ReadOnly] public NativeArray<int> crowd;
        public NativeArray<Zombie> zombies;
        public float blockSize;
        public int mapSizeX;
        public int mapSizeY;
        

        public void Execute(int index)
        {
            Zombie zombie = zombies[index];

            int2 pos = GetPosition(index);
            float3 myPheromones = mapPheromones[pos.x + pos.y * mapSizeX];

            if (myPheromones.x > 0.1 * zombie.senseStrength) zombie.state = 1;
            else zombie.state = 0;
            //if (myPheromones.y > 0.01) zombie.state = 2;
            //if (myPheromones.x > 0.01) zombie.state = 3;

            float2 desirePos = zombie.position;

            if (zombie.state == 0)
            {
                desirePos = zombie.position + GetDirectionToWalk(index) * deltaTime * zombie.speed;
            }
            else if(zombie.state == 1)
            {
                float2 pheromones = new float2();
                if (pos.x + 1 < mapSizeX) pheromones.x += mapPheromones[pos.x + 1 + pos.y * mapSizeX].x;
                if (pos.x - 1 > 0) pheromones.x -= mapPheromones[pos.x - 1 + pos.y * mapSizeX].x;
                if (pos.y + 1 < mapSizeY) pheromones.y += mapPheromones[pos.x + (pos.y + 1) * mapSizeX].x;
                if (pos.y - 1 > 0) pheromones.y -= mapPheromones[pos.x + (pos.y - 1) * mapSizeX].x;


                if (pheromones.x != 0 || pheromones.y != 0) pheromones = math.normalize(pheromones);
                desirePos = zombie.position + pheromones * deltaTime * zombie.speed;
            }

            int2 tile = GetPositionOnGrid(desirePos);
            if (walkable[tile.x + tile.y * mapSizeX])
            {
                zombie.position = desirePos;
            }
            zombies[index] = zombie;
        }

        private float2 GetDirectionToWalk(int index)
        {
            int2 pos = GetPositionOnGrid(zombies[index].position);
            int myCrowd = crowd[pos.x + pos.y * mapSizeX] - 1;

            if (pos.x + 1 < mapSizeX && myCrowd > crowd[pos.x + 1 + pos.y * mapSizeX]) return new float2(1, 0);
            if (pos.x - 1 > 0 && myCrowd > crowd[pos.x - 1 + pos.y * mapSizeX]) return new float2(-1, 0);
            if (pos.y + 1 < mapSizeY && myCrowd > crowd[pos.x + (pos.y + 1) * mapSizeX]) return new float2(0, 1);
            if (pos.y - 1 > 0 && myCrowd > crowd[pos.x + (pos.y - 1) * mapSizeX]) return new float2(0, -1);

            return new float2(0, 0);
        }
        int2 GetPositionOnGrid(float2 pos)
        {
            return new int2((int)math.floor(pos.x / blockSize + mapSizeX / 2), (int)math.floor(pos.y / blockSize + mapSizeY / 2));
        }

        int2 GetPosition(int index)
        {
            return new int2((int)math.round(zombies[index].position.x / blockSize + mapSizeX / 2), (int)math.round(zombies[index].position.y / blockSize + mapSizeY / 2));
        }


    }

    [BurstCompile]
    struct ZombieCollision : IJob
    {
        public NativeArray<Zombie> zombies;
        [ReadOnly] public NativeArray<bool> walkable;
        public float blockSize;
        public int mapSizeX;
        public int mapSizeY;

        public void Execute()
        {
            int i, j;
            for (i = 0; i < zombies.Length; i++)
            {
                var bot1 = zombies[i];
                for (j = i + 1; j < zombies.Length; j++)
                {
                    var bot2 = zombies[j];
                    if (bot2.position.x - bot2.radius > bot1.position.x + bot1.radius)
                    {
                        break;
                    }
                    else
                    {
                        float2 delta = bot2.position - bot1.position;
                        float dist = math.length(delta);
                        if (dist < bot1.radius + bot2.radius)
                        {
                            float2 moveVector = math.normalizesafe(delta) * (dist - (bot1.radius + bot2.radius)) * .4f;

                            float2 moveVector1 = moveVector;
                            float2 moveVector2 = -moveVector;

                            int2 newPos = GetPositionOnGrid(bot1.position + moveVector1);
                            if(!walkable[newPos.x + newPos.y * mapSizeX])
                            {
                                moveVector1.x = 0;
                                moveVector1.y = 0;
                            }

                            int2 newPosTwo = GetPositionOnGrid(bot2.position + moveVector2);
                            if (!walkable[newPosTwo.x + newPosTwo.y * mapSizeX])
                            {
                                moveVector2.x = 0;
                                moveVector2.y = 0;
                            }

                            bot1.position += moveVector1;
                            zombies[i] = bot1;
                            bot2.position += moveVector2;
                            zombies[j] = bot2;
                        }
                    }
                }
            }
        }

        int2 GetPositionOnGrid(float2 pos)
        {
            return new int2((int)math.floor(pos.x / blockSize + mapSizeX / 2), (int)math.floor(pos.y / blockSize + mapSizeY / 2));
        }
    }


}
