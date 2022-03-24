using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    private static  Dictionary<string, float> itemMaintences;
    
    public int time_spent_waiting;
    private float remaining_time = 5;
    private float days_per_step = 0.02f;
    public float distance_threshold = 0.5f;

    public static Vector3 processPosition;

    private Dictionary<string, Vector3> all_process_positions = new Dictionary<string, Vector3>{
        {"Item", new Vector3(0f, 2.5f, 0f)},
        {"AlsoAnItem", new Vector3(0f, 2.5f, 0f)}
        };

    // Start is called before the first frame update
    void Start()
    {
        processPosition = all_process_positions[gameObject.name];
    }

    void Update()
    {
        time_spent_waiting++;
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
    public int GetTimeSpentWaiting()
    {
        return time_spent_waiting;
    }

    public Vector3 GetPosition()
    {
        return gameObject.transform.position;
    }
    public Vector3 GetProcessPosition()
    {
        return processPosition;
    }
    public bool inProcessPosition()
    {
        float distance = (processPosition - gameObject.transform.position).magnitude;
        Debug.Log($"The distance between {gameObject.name} and it's process position {processPosition} is {distance}. In order to be delivered, it must be less than {distance_threshold}");
        return (processPosition - gameObject.transform.position).magnitude <= distance_threshold;
    }

    public string GetName()
    {
        return gameObject.name;
    }

    public void complete()
    {
        Debug.Log($"Destroying {gameObject.name}");
        Destroy(gameObject);
    }
}
