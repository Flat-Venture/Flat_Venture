using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DescriptionSynergyUI : MonoBehaviour
{
    public Image synergyIcon;
    public TextMeshProUGUI synergyName;

    public void SetUp(Sprite synergyIcon, string synergyName)
    {
        this.synergyIcon.sprite = synergyIcon;
        this.synergyName.text = synergyName;
    }
}
