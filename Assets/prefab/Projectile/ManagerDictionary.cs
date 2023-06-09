using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ManagerDictionary : ScriptableObject
{
    public List<ProjectileManager> projList;

    public ProjectileManager loadProperties(string projType)
    {
        foreach (ProjectileManager Settings in projList)
        {
            Debug.Log(Settings.TypeName);
            if(projType.Equals(Settings.TypeName))
                return Settings;
        }
        Debug.Log("none");
        return null;
    }
}
