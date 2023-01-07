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
    GameObject humanPrefab;

    [SerializeField]
    int zombieCount;
    [SerializeField]
    int humanCount;


    public struct Zombie
    {
        public enum ZombieState {Iddle, Allert, Attacing};
        public float2 position;

        public ZombieState state;
        public float speed;
        public float damage;
        public float senseStrength;
        public float radius;
        public int positionOnGrid;
        public int cameFromPositionOnGrid;
        public float rotation;

        public bool sporeBurst;
    }

    public struct Human
    {
        public enum HumanState { Walking, Running};
        public bool active;
        public float2 position;
        public HumanState state;
        public float hp;
        public float maxHp;
        public float speed;
        public float radius;
        public float avoidanceRadius;
        public int positionOnGrid;
        public float rotation;
    }


    List<Transform> zombieTransforms = new List<Transform>();
    public List<Zombie> zombies = new List<Zombie>();
    List<Transform> humanTransforms = new List<Transform>();
    public List<Human> humans = new List<Human>();
    //NativeArray<Zombie> zombiesArray;

    TransformAccessArray z_AccessArray;
    TransformAccessArray h_AccessArray;
    MapManager mapManager;


    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

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
                speed = UnityEngine.Random.Range(0.3f, 0.4f),
                senseStrength = UnityEngine.Random.Range(0.1f, 1f),
                damage = UnityEngine.Random.Range(0.4f, 0.5f),
                radius = 1f,
                rotation = 0,
                sporeBurst = UnityEngine.Random.Range(0f, 1f) > 0.5 ? true : false,
            };

            zombies.Add(zombie);
            zombieTransforms.Add(Instantiate(zombiePrefab, new Vector3(zombie.position.x, 1.5f, zombie.position.y), quaternion.identity).transform);
        }

        for (int i = 0; i < humanCount; i++)
        {
            int2 pos = new int2(0,0);

            float2 spawnPointy = new float2(pos.x * mapManager.blockSize + mapManager.blockSize - (mapManager.mapSize.x * mapManager.blockSize) / 2, pos.y * mapManager.blockSize + mapManager.blockSize - (mapManager.mapSize.y * mapManager.blockSize) / 2);

            Human human = new Human()
            {
                active = false,
                position = spawnPointy,
                state = 0,
                speed = UnityEngine.Random.Range(0.5f, 0.7f),
                radius = 1f,
                avoidanceRadius = 2f,
                maxHp = 1,
                hp = 1,
                rotation = 0,
            };

            humans.Add(human);
            GameObject temp = Instantiate(humanPrefab, new Vector3(human.position.x, 1.5f, human.position.y), quaternion.identity);
            temp.SetActive(false);
            humanTransforms.Add(temp.transform);
        }


        z_AccessArray = new TransformAccessArray(zombieTransforms.ToArray());
        h_AccessArray = new TransformAccessArray(humanTransforms.ToArray());
        //zombiesArray = new NativeArray<Zombie>(zombies.ToArray(), Allocator.Persistent);
    }
    [SerializeField] bool simulate;


    private void Update()
    {
        if (!simulate) return;
        SortZombies();
        NativeArray<Zombie> zombiesArrayNative = new NativeArray<Zombie>(zombies.ToArray(), Allocator.TempJob);
        NativeArray<Human> humanArrayNative = new NativeArray<Human>(humans.ToArray(), Allocator.TempJob);
        ZombieDecision zombieDecision = new ZombieDecision()
        {
            deltaTime = Time.deltaTime,
            zombies = zombiesArrayNative,
            humans = humanArrayNative,
            mapPheromones = mapManager.mapPheromones,
            blockSize = mapManager.blockSize,
            mapSizeX = mapManager.mapSize.x,
            mapSizeY = mapManager.mapSize.y,
            walkable = mapManager.map,
            //crowd = MapManager.mapManager.crowdedness,
        };

        JobHandle handle = zombieDecision.Schedule(z_AccessArray.length, 64);
        handle.Complete();


        HumanDecision humanDecision = new HumanDecision()
        {
            deltaTime = Time.deltaTime,
            humans = humanArrayNative,
            zombies = zombiesArrayNative,
            mapPheromones = mapManager.mapPheromones,
            blockSize = mapManager.blockSize,
            mapSizeX = mapManager.mapSize.x,
            mapSizeY = mapManager.mapSize.y,
            walkable = mapManager.map,
        };


        JobHandle humanHandle = humanDecision.Schedule(h_AccessArray.length, 64);
        humanHandle.Complete();


        NativeArray<float4> copyPheromones = new NativeArray<float4>(mapManager.mapPheromones, Allocator.TempJob);

        ZombiePheromones zombiePheromones = new ZombiePheromones()
        {
            deltaTime = Time.deltaTime,
            zombies = zombiesArrayNative,
            mapPheromones = copyPheromones,
            mapSizeX = mapManager.mapSize.x,
            mapSizeY = mapManager.mapSize.y,
            blockSize = mapManager.blockSize,
        };
        zombiePheromones.Run();

        HumanPheromones humanPheromones = new HumanPheromones()
        {
            deltaTime = Time.deltaTime,
            humans = humanArrayNative,
            mapPheromones = copyPheromones,
            mapSizeX = mapManager.mapSize.x,
            mapSizeY = mapManager.mapSize.y,
            blockSize = mapManager.blockSize,
        };
        humanPheromones.Run();


        for (int i = 0; i < copyPheromones.Length; i++)
        {
            mapManager.mapPheromones[i] = copyPheromones[i];
        }

        ZombieCollision colision = new ZombieCollision() { 
            zombies = zombiesArrayNative, 
            mapSizeX = mapManager.mapSize.x, 
            mapSizeY = mapManager.mapSize.y, 
            blockSize = mapManager.blockSize, 
            walkable = mapManager.map,
        };
        colision.Run();

        HumanColision humancolision = new HumanColision()
        {
            humans = humanArrayNative,
            zombies = zombiesArrayNative,
            timeDelta = Time.deltaTime,
            mapSizeX = mapManager.mapSize.x,
            mapSizeY = mapManager.mapSize.y,
            blockSize = mapManager.blockSize,
            walkable = mapManager.map,
        };
        humancolision.Run();


        for (int i = 0; i < zombies.Count; i++)
        {
            zombies[i] = zombiesArrayNative[i];
            //if (zombies[i].state == 1) Debug.Log("hunting");
            zombieTransforms[i].position = new Vector3(zombies[i].position.x, 0, zombies[i].position.y);
            zombieTransforms[i].rotation = Quaternion.Euler(0, zombies[i].rotation - 90, 0);
        }

        for(int i = 0; i < humanCount; i++)
        {
            humans[i] = humanArrayNative[i];
            if (!humans[i].active && humanTransforms[i].gameObject.activeSelf) humanTransforms[i].gameObject.SetActive(false);
            humanTransforms[i].position = new Vector3(humans[i].position.x, 0, humans[i].position.y);
            humanTransforms[i].rotation = Quaternion.Euler(0, humans[i].rotation - 90, 0);
        }

        zombiesArrayNative.Dispose();
        humanArrayNative.Dispose();
        copyPheromones.Dispose();
    }

    private void OnDestroy()
    {
        z_AccessArray.Dispose();
        h_AccessArray.Dispose();
    }


    public void SpawnHuman(float2 pos)
    {
        int2 mapPos = new int2((int)math.floor(pos.x / mapManager.blockSize + mapManager.mapSize.x / 2), (int)math.floor(pos.y / mapManager.blockSize + mapManager.mapSize.y / 2));
        if (!mapManager.map[mapPos.x + mapPos.y * mapManager.mapSize.x]) return;


        for (int i = 0; i < humans.Count; i++)
        {
            Human human = humans[i];

            if (!human.active)
            {
                humanTransforms[i].gameObject.SetActive(true);
                human.position = pos;
                human.active = true;
                human.hp = human.maxHp;
                humans[i] = human;
                break;
            }
        }
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

        for (i = 1; i < humans.Count; i++)
        {
            for (j = i - 1; j >= 0; j--)
            {
                if (humans[i].active && humans[j].active && humans[j].position.x - humans[j].radius > humans[j + 1].position.x - humans[j + 1].radius)
                {
                    Human temp = humans[j];
                    humans[j] = humans[j + 1];
                    humans[j + 1] = temp;
                }
                else
                {
                    break;
                }
            }
        }

    }

    [BurstCompile]
    struct HumanDecision : IJobParallelFor
    {
        [ReadOnly] public float deltaTime;
        [ReadOnly] public NativeArray<bool> walkable;
        [ReadOnly] public NativeArray<float4> mapPheromones;
        [ReadOnly] public NativeArray<Zombie> zombies;
        public NativeArray<Human> humans;
        public float blockSize;
        public int mapSizeX;
        public int mapSizeY;

        public void Execute(int index)
        {
            Human human = humans[index];

            if (human.hp <= 0)
            {
                human.active = false;
                humans[index] = human;
                return;
            }

            int2 posOnGrid = GetPositionOnGrid(human.position);

            human.positionOnGrid = posOnGrid.x + posOnGrid.y * mapSizeX;

            float2 dir = math.normalize(new float2(0, 1) * 0.4f + GetAvoidance(human) * 0.6f);
            float2 desirePos = human.position + dir * deltaTime * human.speed;

            int2 tile = GetPositionOnGrid(desirePos);

            if (walkable[tile.x + tile.y * mapSizeX])
            {
                human.positionOnGrid = tile.x + tile.y * mapSizeX;
                human.position = desirePos;
            }

            humans[index] = human;
        }


        float2 GetAvoidance(Human human)
        {
            int j;
            float2 direction = new float2();
            for (j = 0; j < zombies.Length; j++)
            {
                var bot2 = zombies[j];
                if (bot2.position.x - bot2.radius > human.position.x + human.radius + human.avoidanceRadius)
                {
                    break;
                }
                else
                {
                    float2 delta = bot2.position - human.position;
                    float dist = math.length(delta);
                    if (dist < human.radius + human.avoidanceRadius + bot2.radius)
                    {
                        float2 moveVector = math.normalizesafe(delta) * (dist - (human.radius + human.avoidanceRadius + bot2.radius)) * .4f;
                        int2 newPos = GetPositionOnGrid(human.position + moveVector);
                        if (!walkable[newPos.x + newPos.y * mapSizeX])
                        {
                            moveVector.x = 0;
                            moveVector.y = 0;
                        }
                        direction += moveVector;
                    }
                }
            }
            return math.length(direction) > 0 ? math.normalize(direction) : direction;
        }

        int2 GetPositionOnGrid(float2 pos)
        {
            return new int2((int)math.floor(pos.x / blockSize + mapSizeX / 2), (int)math.floor(pos.y / blockSize + mapSizeY / 2));
        }

    }



    [BurstCompile]
    struct ZombieDecision : IJobParallelFor
    {
        [ReadOnly] public float deltaTime;
        [ReadOnly] public NativeArray<float4> mapPheromones;
        [ReadOnly] public NativeArray<bool> walkable;
        //[ReadOnly] public NativeArray<int> crowd;
        public NativeArray<Zombie> zombies;
        [ReadOnly] public NativeArray<Human> humans;
        public float blockSize;
        public int mapSizeX;
        public int mapSizeY;
        

        public void Execute(int index)
        {
            Zombie zombie = zombies[index];

            int2 pos = GetPositionOnGrid(zombie.position);
            float4 myPheromones = mapPheromones[pos.x + pos.y * mapSizeX];

            if (myPheromones.z > 0.2 * zombie.senseStrength) zombie.state = Zombie.ZombieState.Attacing;
            else if (myPheromones.y > 0.2 * zombie.senseStrength) zombie.state = Zombie.ZombieState.Allert;
            else zombie.state = Zombie.ZombieState.Iddle;

            float2 desirePos = zombie.position;

            if (zombie.state == Zombie.ZombieState.Iddle)
            {
                desirePos = zombie.position + GetDirectionToWalk(index) * deltaTime * zombie.speed;
            }
            else if(zombie.state == Zombie.ZombieState.Allert)
            {
                float2 pheromones = new float2();
                if (pos.x + 1 < mapSizeX) pheromones.x += mapPheromones[pos.x + 1 + pos.y * mapSizeX].y; 
                if (pos.x - 1 > 0) pheromones.x -= mapPheromones[pos.x - 1 + pos.y * mapSizeX].y;
                if (pos.y + 1 < mapSizeY) pheromones.y += mapPheromones[pos.x + (pos.y + 1) * mapSizeX].y;
                if (pos.y - 1 > 0) pheromones.y -= mapPheromones[pos.x + (pos.y - 1) * mapSizeX].y;


                if (pheromones.x != 0 || pheromones.y != 0) pheromones = math.normalize(pheromones);
                desirePos = zombie.position + pheromones * deltaTime * zombie.speed;
            }
            else if(zombie.state == Zombie.ZombieState.Attacing)
            {
                float2 pheromones = new float2();
                float up = mapPheromones[pos.x + (pos.y + 1) * mapSizeX].z, down = mapPheromones[pos.x + (pos.y - 1) * mapSizeX].z, left = mapPheromones[pos.x - 1 + pos.y * mapSizeX].z, right = mapPheromones[pos.x + 1 + pos.y * mapSizeX].z;
                float current = mapPheromones[pos.x + pos.y * mapSizeX].z;
                if (up < current && down < current && left < current && right < current)
                {
                    for(int i = 0; i < humans.Length; i++)
                    {
                        if(humans[i].active )
                        {
                            int2 humanPos = GetPositionOnGrid(humans[i].position);
                            int2 zombiePos = GetPositionOnGrid(zombie.position);
                            if (humanPos.x == zombiePos.x && humanPos.y == zombiePos.y)
                            {
                                float2 dirToWalk = math.normalize(humans[i].position - zombie.position);
                                desirePos = zombie.position + dirToWalk * deltaTime * zombie.speed;
                            }
                        }
                    }
                }
                else
                {
                    if (pos.x + 1 < mapSizeX) pheromones.x += right;
                    if (pos.x - 1 > 0) pheromones.x -= left;
                    if (pos.y + 1 < mapSizeY) pheromones.y += up;
                    if (pos.y - 1 > 0) pheromones.y -= down;


                    if (pheromones.x != 0 || pheromones.y != 0) pheromones = math.normalize(pheromones);
                    desirePos = zombie.position + pheromones * deltaTime * zombie.speed;
                }
            }

            int2 tile = GetPositionOnGrid(desirePos);

            float3 lookAt;

            lookAt.x = desirePos.x - zombie.position.x;
            lookAt.y = desirePos.y - zombie.position.y;

            zombie.rotation = (math.atan2(lookAt.y, lookAt.x) * 180) / math.PI;

            if (walkable[tile.x + tile.y * mapSizeX])
            {
                if(zombie.positionOnGrid != tile.x + tile.y * mapSizeX)
                {
                    zombie.cameFromPositionOnGrid = zombie.positionOnGrid;
                }
                zombie.positionOnGrid = tile.x + tile.y * mapSizeX;
                zombie.position = desirePos;
            }
            else
            {
                //zombie.positionOnGrid = pos.x + pos.y * mapSizeX;
            }
            zombies[index] = zombie;
        }

        private float2 GetDirectionToWalk(int index)
        {
            int2 pos = GetPositionOnGrid(zombies[index].position);
            int indexInGrid = pos.x + pos.y * mapSizeX;
            int oldPosIndes = zombies[index].cameFromPositionOnGrid;
            int2 forbiddenDir = new int2(oldPosIndes % mapSizeX, oldPosIndes / mapSizeX) - pos;


            float2 toGo = new float2();
            float dislikeLeft, dislikeRight, dislikeUp, dislikeDown;

            if (pos.x + 1 < mapSizeX && walkable[indexInGrid + 1]) dislikeRight = mapPheromones[indexInGrid + 1].x + (forbiddenDir.x != -1 ? 0.4f : 0);
            else dislikeRight = -1;

            if (pos.x - 1 > 0 && walkable[indexInGrid - 1]) dislikeLeft = mapPheromones[indexInGrid - 1].x + (forbiddenDir.x != 1 ? 0.4f : 0);
            else dislikeLeft = -1;


            if (pos.y - 1 > 0 && walkable[indexInGrid - mapSizeX]) dislikeUp = mapPheromones[indexInGrid + mapSizeX].x + (forbiddenDir.y != -1 ? 0.4f : 0);
            else dislikeUp = -1;

            if (pos.y + 1 < mapSizeY && walkable[indexInGrid + mapSizeX]) dislikeDown = mapPheromones[indexInGrid - mapSizeX].x + (forbiddenDir.y != 1 ? 0.4f : 0);
            else dislikeDown = -1;

            if (dislikeRight < 0 && dislikeLeft < 0) toGo.x = 0;

            if(dislikeLeft > 0 && dislikeRight > 0)
            {
                toGo.x = -(dislikeRight - dislikeLeft);
            }
            else if (dislikeLeft > 0)
            {
                toGo.x = -0.1f;
            }
            else if (dislikeRight > 0)
            {
                toGo.x = 0.1f;
            }

            if (dislikeUp < 0 && dislikeDown< 0) toGo.y = 0;

            if (dislikeUp > 0 && dislikeDown > 0)
            {
                toGo.y = -(dislikeUp - dislikeDown);
            }
            else if (dislikeUp > 0)
            {
                toGo.y = -0.1f;
            }
            else if (dislikeDown > 0)
            {
                toGo.y = 0.1f;
            }


            return math.length(toGo) > 0 ? math.normalize(toGo) : toGo;
        }
        int2 GetPositionOnGrid(float2 pos)
        {
            return new int2((int)math.floor(pos.x / blockSize + mapSizeX / 2), (int)math.floor(pos.y / blockSize + mapSizeY / 2));
        }


    }

    [BurstCompile]
    struct ZombiePheromones : IJob
    {
        public NativeArray<float4> mapPheromones; // x - crowdness, y - alertness, z - human smell, w - blood
        public NativeArray<Zombie> zombies;
        [ReadOnly] public float deltaTime;
        public float blockSize;
        public int mapSizeX;
        public int mapSizeY;

        public void Execute()
        {
            for (int i = 0; i < zombies.Length; i++)
            {
                Zombie zombie = zombies[i];
                float4 pheromonOnTile = mapPheromones[zombie.positionOnGrid];
                switch (zombies[i].state)
                {
                    case Zombie.ZombieState.Iddle: // iddle
                        pheromonOnTile.x += 0.2f * deltaTime;
                        break;
                    case Zombie.ZombieState.Allert: // allert
                        pheromonOnTile.x += 0.2f * deltaTime;
                        break;
                    case Zombie.ZombieState.Attacing: // attacking
                        pheromonOnTile.x += 0.2f * deltaTime;
                        pheromonOnTile.y += 0.2f * deltaTime;
                        if (zombies[i].sporeBurst)
                        {
                            zombie.sporeBurst = false;
                            pheromonOnTile.y += 20;
                        }
                        break;
                }
                zombies[i] = zombie;
                mapPheromones[zombie.positionOnGrid] = pheromonOnTile;
            }
        }
    }


    [BurstCompile]
    struct HumanPheromones : IJob
    {
        public NativeArray<float4> mapPheromones;
        [ReadOnly] public NativeArray<Human> humans;
        [ReadOnly] public float deltaTime;
        public float blockSize;
        public int mapSizeX;
        public int mapSizeY;

        public void Execute()
        {
            for (int i = 0; i < humans.Length; i++)
            {
                if (!humans[i].active) continue;
                Human human = humans[i];
                float4 pheromonOnTile = mapPheromones[human.positionOnGrid];
                switch (human.state)
                {
                    case Human.HumanState.Walking: // walking
                        pheromonOnTile.z += 0.75f * deltaTime;
                        break;
                    case Human.HumanState.Running: // running
                        pheromonOnTile.z += 1f * deltaTime;
                        //pheromonOnTile.y += 0.01f;
                        break;
                }

                if(human.hp < human.maxHp * 0.5f)
                {
                    pheromonOnTile.w += 0.3f * deltaTime;
                }

                mapPheromones[human.positionOnGrid] = pheromonOnTile;
            }
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


    [BurstCompile]
    struct HumanColision : IJob
    {
        public NativeArray<Human> humans;
        public NativeArray<Zombie> zombies;
        [ReadOnly] public NativeArray<bool> walkable;
        [ReadOnly] public float timeDelta;
        public float blockSize;
        public int mapSizeX;
        public int mapSizeY;
        public void Execute()
        {
            int i, j;
            for (i = 0; i < humans.Length; i++)
            {
                var bot1 = humans[i];
                if (!bot1.active) continue;
                for (j = i + 1; j < humans.Length; j++)
                {
                    var bot2 = humans[j];
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
                            if (!walkable[newPos.x + newPos.y * mapSizeX])
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
                            humans[i] = bot1;
                            bot2.position += moveVector2;
                            humans[j] = bot2;
                        }
                    }
                }
            }

            for (i = 0; i < humans.Length; i++)
            {
                var bot1 = humans[i];
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
                            if (!walkable[newPos.x + newPos.y * mapSizeX])
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
                            bot1.hp -= bot2.damage * timeDelta;
                            bot2.position += moveVector2;

                            zombies[j] = bot2;
                            humans[i] = bot1;
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
