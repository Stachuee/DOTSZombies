using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpdateUI : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI text;

    [SerializeField]
    Jednostka jedn;

    enum Jednostka { None, Percent, Vec2Szie}

    private void Start()
    {
        ValueChange(transform.GetComponent<Slider>().value);
    }

    public void ValueChange(float value)
    {
        if (jedn == Jednostka.Vec2Szie) text.text = (Mathf.Round(value * 100) / 100).ToString() + "x" + (Mathf.Round(value * 100) / 100).ToString();
        else text.text = (Mathf.Round(value * 100) / 100).ToString() + (jedn == Jednostka.Percent ? "%" : "");
    }
}
