using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartGame : MonoBehaviour
{

    [SerializeField]
    Slider zombieCount;
    [SerializeField]
    Slider zombieSpeed;
    [SerializeField]
    Slider zombieDamage;
    [SerializeField]
    Slider zombieSensitivity;
    [SerializeField]
    Slider zombieChance;
    [SerializeField]
    Slider zombieStrength;


    [SerializeField]
    Slider humanCount;
    [SerializeField]
    Slider humanSpeed;
    [SerializeField]
    Slider humanHp;
    [SerializeField]
    Slider humanZombie;
    [SerializeField]
    Slider humanSmell;
    [SerializeField]
    Slider humanWalls;

    [SerializeField]
    Button zombieSpawnRandom;
    [SerializeField]
    Button zombieSpawnIn;
    [SerializeField]
    Button zombieSpawnOut;


    [SerializeField]
    Button humanSpawnRandom;
    [SerializeField]
    Button humanSpawnIn;
    [SerializeField]
    Button humanSpawnOut;

    int zombieSpawnType;

    int humanSpawnType;

    public void ChangeZombieSpawn(int id)
    {
        zombieSpawnType = id;
        zombieSpawnRandom.interactable = true;
        zombieSpawnIn.interactable = true;
        zombieSpawnOut.interactable = true;
        switch(id)
        {
            case 0:
                zombieSpawnRandom.interactable = false;
                break;
            case 1:
                zombieSpawnIn.interactable = false;
                break;
            case 2:
                zombieSpawnOut.interactable = false;
                break;
        }

    }

    public void ChangeHumanSpawn(int id)
    {
        humanSpawnType = id;
        humanSpawnRandom.interactable = true;
        humanSpawnIn.interactable = true;
        humanSpawnOut.interactable = true;
        switch (id)
        {
            case 0:
                humanSpawnRandom.interactable = false;
                break;
            case 1:
                humanSpawnIn.interactable = false;
                break;
            case 2:
                humanSpawnOut.interactable = false;
                break;
        }
    }

    public void SetUpAndStartGame()
    {
        AIManager ai = AIManager.aIManager;
        MapManager.mapManager.PreGenerateMap();

        ai.zombieCount = zombieCount.value;
        ai.zombieSpeed = zombieSpeed.value;
        ai.zombieDamage = zombieDamage.value;
        ai.zombieSensitivity = zombieSensitivity.value;
        ai.zombieChance = zombieChance.value;
        ai.zombieStrength = zombieStrength.value;

        ai.zombieSpawnType = zombieSpawnType;


        ai.humanCount = humanCount.value;
        ai.humanSpeed = humanSpeed.value;
        ai.humanHp = humanHp.value;
        ai.humanZombie = humanZombie.value;
        ai.humanSmell = humanSmell.value;
        ai.humanWalls = humanWalls.value;

        ai.humanSpawnType = humanSpawnType;

        ai.Populate();
    }
}   
