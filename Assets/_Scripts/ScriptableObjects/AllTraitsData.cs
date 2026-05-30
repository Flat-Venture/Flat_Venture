using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AllTraitsData", menuName = "Scriptable Objects/AllTraitsData")]
public class AllTraitsData : ScriptableObject
{
    public List<TraitData> allTraits;
}
