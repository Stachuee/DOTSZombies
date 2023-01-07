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

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                aIManager.SpawnHuman(new float2(hit.point.x, hit.point.z));
            }
        }
    }
}
