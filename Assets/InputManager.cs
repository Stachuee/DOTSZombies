using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    AIManager aIManager;


    private void Start()
    {
        aIManager = transform.GetComponent<AIManager>();    
    }


}
