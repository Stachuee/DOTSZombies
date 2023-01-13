using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiPanels : MonoBehaviour
{
    [SerializeField]
    GameObject mapPanel;
    [SerializeField]
    GameObject zombiePanel;
    [SerializeField]
    GameObject humanPanel;


    public void ChangePanel(int id)
    {
        switch(id)
        {
            case 0:
                mapPanel.SetActive(true);
                zombiePanel.SetActive(false);
                humanPanel.SetActive(false);
                break;
            case 1:
                mapPanel.SetActive(false);
                zombiePanel.SetActive(true);
                humanPanel.SetActive(false);
                break;
            case 2:
                mapPanel.SetActive(false);
                zombiePanel.SetActive(false);
                humanPanel.SetActive(true);
                break;
        }
    }

}
