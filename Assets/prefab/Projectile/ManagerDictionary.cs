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
            if(projType.Equals(Settings.TypeName))
                return Settings;
        }
        return null;
    }
}
