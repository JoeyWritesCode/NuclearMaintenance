using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    private static  Dictionary<string, float> itemMaintences;
    private static float days_per_step = 0.2F;
    
    private float remaining_time;

    public static string name;
    public static Vector3 processPosition;

    private Dictionary<string, Vector3> all_process_positions = new Dictionary<string, Vector3>{{"Item", Vector3.zero}};

    // Start is called before the first frame update
    void Start()
    {
        name = gameObject.name;
        processPosition = all_process_positions[name];
    }

    public void decrementProcessTime()
    {
        remaining_time -= days_per_step;
    }

    public bool isProcessed()
    {
        return remaining_time <= 0;
    }

    public float GetRemainingTime()
    {
        return remaining_time;
    }

    public Vector3 GetPosition()
    {
        return gameObject.transform.position;
    }
    public Vector3 GetProcessPosition()
    {
        return processPosition;
    }

    public string GetName()
    {
        return name;
    }

    public void complete()
    {
        Destroy(gameObject);
    }
}
