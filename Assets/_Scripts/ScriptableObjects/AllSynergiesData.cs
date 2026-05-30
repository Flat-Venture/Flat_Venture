using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AllSynergiesData", menuName = "Scriptable Objects/AllSynergiesData")]
public class AllSynergiesData : ScriptableObject
{
    public List<SynergyData> allSyneries;
}
