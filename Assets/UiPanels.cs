using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiPanels : MonoBehaviour
{
    [SerializeField]
    GameObject zombiePanel;
    [SerializeField]
    GameObject humanPanel;

    [SerializeField]
    Button zombieButton;

    [SerializeField]
    Button humanButton;


    public void ChangePanel(int id)
    {
        switch(id)
        {
            case 0:
                zombiePanel.SetActive(true);
                humanPanel.SetActive(false);
                zombieButton.interactable = false;
                humanButton.interactable = true;
                break;
            case 1:
                zombiePanel.SetActive(false);
                humanPanel.SetActive(true);
                humanButton.interactable = false;
                zombieButton.interactable = true;
                break;
        }
    }

}
