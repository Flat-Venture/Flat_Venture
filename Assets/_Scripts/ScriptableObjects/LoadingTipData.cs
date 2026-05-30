using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LoadingTipData", menuName = "Scriptable Objects/LoadingTipData")]
public class LoadingTipData : ScriptableObject
{
    [TextArea]
    public List<string> tips;
}
