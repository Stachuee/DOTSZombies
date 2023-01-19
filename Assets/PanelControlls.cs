using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelControlls : MonoBehaviour
{
    [SerializeField]
    Button hideUi;

    bool hidden = true;

    [SerializeField]
    Sprite hide;
    [SerializeField]
    Sprite show;

    [SerializeField]
    Vector2 pos;
    [SerializeField]
    Vector2 hidePos;

    [SerializeField]
    Button zombieSmell;
    [SerializeField] Sprite zombieSmellpressed;
    [SerializeField] Sprite zombieSmellnotPressed;

    [SerializeField]
    Button zombieAletness;
    [SerializeField] Sprite zombieAletnesspressed;
    [SerializeField] Sprite zombieAletnessnotPressed;

    [SerializeField]
    Button humanSmell;
    [SerializeField] Sprite humanSmellpressed;
    [SerializeField] Sprite humanSmellnotPressed;

    [SerializeField]
    Button humanBlood;
    [SerializeField] Sprite humanBloodpressed;
    [SerializeField] Sprite humanBloodnotPressed;

    public void HideUi()
    {
        if(hidden)
        {
            hideUi.image.sprite = show;
            transform.position = pos;
            hidden = false;
        }
        else
        {
            hideUi.image.sprite = hide;
            transform.position = hidePos;
            hidden = true;
        }
    }

    public void UpdateButtonPressed(int id)
    {
        MapManager map = MapManager.mapManager;
        switch(id)
        {
            case 0:
                if(map.drawCrowdness)
                {
                    zombieSmell.image.sprite = zombieSmellnotPressed;
                }
                else
                {
                    zombieSmell.image.sprite = zombieSmellpressed;
                    zombieAletness.image.sprite = zombieAletnessnotPressed;
                    humanSmell.image.sprite = humanSmellnotPressed;
                    humanBlood.image.sprite = humanBloodnotPressed;
                }
                map.drawCrowdness = !map.drawCrowdness;
                map.drawAlertness = false;
                map.drawHumanSmell = false;
                map.drawBlood = false;
                break;
            case 1:
                if (map.drawAlertness)
                {
                    zombieAletness.image.sprite = zombieAletnessnotPressed;
                }
                else
                {
                    zombieSmell.image.sprite = zombieSmellnotPressed;
                    zombieAletness.image.sprite = zombieAletnesspressed;
                    humanSmell.image.sprite = humanSmellnotPressed;
                    humanBlood.image.sprite = humanBloodnotPressed;
                }
                map.drawCrowdness = false;
                map.drawAlertness = !map.drawAlertness;
                map.drawHumanSmell = false;
                map.drawBlood = false;
                break;
            case 2:
                if (map.drawHumanSmell)
                {
                    humanSmell.image.sprite = humanSmellnotPressed;
                }
                else
                {
                    zombieSmell.image.sprite = zombieSmellnotPressed;
                    zombieAletness.image.sprite = zombieAletnessnotPressed;
                    humanSmell.image.sprite = humanSmellpressed;
                    humanBlood.image.sprite = humanBloodnotPressed;
                }
                map.drawCrowdness = false;
                map.drawAlertness = false;
                map.drawHumanSmell = !map.drawHumanSmell;
                map.drawBlood = false;
                break;
            case 3:
                if (map.drawBlood)
                {
                    humanBlood.image.sprite = humanBloodnotPressed;
                }
                else
                {
                    zombieSmell.image.sprite = zombieSmellnotPressed;
                    zombieAletness.image.sprite = zombieAletnessnotPressed;
                    humanSmell.image.sprite = humanSmellnotPressed;
                    humanBlood.image.sprite = humanBloodpressed;
                }
                map.drawCrowdness = false;
                map.drawAlertness = false;
                map.drawHumanSmell = false;
                map.drawBlood = !map.drawBlood;
                break;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(pos, hidePos);
    }
}
