using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class CamScript : MonoBehaviour
{


    MapManager mapManager;
    Camera cam;

    Vector2 mousePos;
    [SerializeField]
    Vector2 zoom;
    [SerializeField]
    Vector2 minMaxPanSpeed;
    Vector2 leftBottomCorner;
    Vector2 rightTopCorner;
        
    float camSpeed;

    [SerializeField][Range(0f, 0.5f)]
    float scrollScreenPercent;
    [SerializeField]
    private float sensitivity;


    private void Awake()
    {
        cam = Camera.main;
    }
    private void Start()
    {
        mapManager = MapManager.mapManager;
        leftBottomCorner = new Vector2(-mapManager.mapSize.x * mapManager.blockSize, -mapManager.mapSize.y * mapManager.blockSize) / 2;
        rightTopCorner = new Vector2(leftBottomCorner.x, leftBottomCorner.y) * -1;
    }

    private void Update()
    {
        mousePos = Input.mousePosition;

        camSpeed = Mathf.Lerp(minMaxPanSpeed.x, minMaxPanSpeed.y, (transform.position.y - zoom.x) / (zoom.y - zoom.x));

        if (mousePos.x < Screen.width * scrollScreenPercent)
        {
            transform.position += Vector3.left * camSpeed * Time.deltaTime ;
        }
        else if(mousePos.x > Screen.width * (1 - scrollScreenPercent))
        {
            transform.position += -Vector3.left * camSpeed * Time.deltaTime;
        }
        if (mousePos.y < Screen.height * scrollScreenPercent)
        {
            transform.position += -Vector3.forward * camSpeed * Time.deltaTime;
        }
        else if(mousePos.y > Screen.height * (1 - scrollScreenPercent))
        {
            transform.position += Vector3.forward * camSpeed * Time.deltaTime;
        }

        transform.position += Vector3.up * Input.GetAxis("Mouse ScrollWheel") * sensitivity ;

        transform.position = new Vector3(Mathf.Clamp(transform.position.x, leftBottomCorner.x, rightTopCorner.x), Mathf.Clamp(transform.position.y, zoom.x, zoom.y), Mathf.Clamp(transform.position.z, leftBottomCorner.y, rightTopCorner.y));
        
        
    }
}
