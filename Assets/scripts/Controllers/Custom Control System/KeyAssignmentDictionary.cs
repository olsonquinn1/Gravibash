using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ControllHandler
{
    public static Dictionary<string, KeyCode> KeyDict;

    static string[] keyMaps = new string[6]
    {
        "Attack",
        "Block",
        "Forward",
        "Backward",
        "Left",
        "Right"
    };
    static KeyCode[] defaultKeys = new KeyCode[6]
    {
        KeyCode.Q,
        KeyCode.E,
        KeyCode.W,
        KeyCode.S,
        KeyCode.A,
        KeyCode.D
    };

    static ControllHandler()
    {
        InitializeDictionary();
    }
 
    public static void InitializeDictionary()
    {
        KeyDict = new Dictionary<string, KeyCode>();
        for(int i=0;i<keyMaps.Length;++i)
        {
            KeyDict.Add(keyMaps[i], defaultKeys[i]);
        }
    }

    public static void StartControllSys()
    {    }
}

