using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    private static  Dictionary<string, float> itemMaintences;
    
    public int time_spent_waiting;
    private float total_time = 50.0f;
    private float remaining_time;
    private float days_per_step = 0.2f;
    public float distance_threshold = 0.25f;

    private Renderer renderer;

    public static Vector3 processPosition;

    private Dictionary<string, Vector3> all_process_positions = new Dictionary<string, Vector3>{
        {"Item", new Vector3(0f, 2.5f, 0f)},
        {"AlsoAnItem", new Vector3(0f, 2.5f, 0f)}
        };

    // Start is called before the first frame update
    void Start()
    {
        renderer = gameObject.GetComponent<Renderer>();
        if (!all_process_positions.TryGetValue(gameObject.name, out processPosition)) {
            
            Color Red = new Color(255, 0, 0);
            Color Blue = new Color(0, 0, 255);

            if (renderer.material.GetColor("_Color") == Red)
                processPosition = new Vector3(-10.0f, 2.5f, -1.5f);
            else if (renderer.material.GetColor("_Color") == Blue)
                processPosition = new Vector3(9.0f, 2.5f, -1.5f);
            else
                processPosition = new Vector3(0f, 2.5f, 0f);
        }
        remaining_time = total_time;
    }

    void Update()
    {
        time_spent_waiting++;
    }

    public void decrementProcessTime()
    {
        remaining_time -= days_per_step;
        renderer.material.SetColor("_Color", new Color(remaining_time / total_time, 1, remaining_time / total_time));
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
