using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    private static Dictionary<string, float> itemMaintences;
    
    public int time_spent_waiting;
    private float total_time = 50.0f;
    private float remaining_time;
    private float days_per_step = 0.2f;
    public float distance_threshold = 0.25f;

    private bool beingCarried = false;

    private Renderer renderer;

    public static Vector3 processPosition;
    private static GameObject processObject;

    public bool isEmpty = false;
    public string itemName;

    private Dictionary<string, List<string>> objectsBelongWith = new Dictionary<string, List<string>> {
        {"WarheadTransitContainer", new List<string>{"StoreContainersWarheadTransit", "10.0"}},
        {"CompletedWarhead", new List<string>{"StoreContainersWarheadTransit", "delivery"}},
        {"WarheadContainer", new List<string>{"Disassembly", "delivery"}},
        {"Warhead", new List<string>{"Disassembly", "delivery"}},
        {"MaterialA", new List<string>{"StoreContainersMaterialA", "10.0"}}
        };

    private Dictionary<string, List<string>> objectComponents = new Dictionary<string, List<string>> {
        {"WarheadContainer", new List<string>{"Warhead"}},
        {"Warhead", new List<string>{"MaterialA", "MaterialB", "NonFissle"}}
        };

    public List<string> inventory = new List<string>();

    // Start is called before the first frame update
    void Start()
    {
        renderer = gameObject.GetComponent<Renderer>();
        List<string> processInformation = objectsBelongWith[itemName];
        processObject = GameObject.Find(processInformation[0]);

        if (processInformation[1] == "delivery") {
            total_time = float.MinValue;
        }
        else {
            total_time = float.Parse(processInformation[1]);
        }

        processPosition = processObject.transform.position;
        remaining_time = total_time;
    }

    void Update()
    {
        time_spent_waiting++;
    }

    public void decrementProcessTime()
    {
        remaining_time -= days_per_step;
        renderer.material.SetColor("_Color", new Color(remaining_time / total_time, 255, remaining_time / total_time));
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

    void EmptyContents()
    {
        foreach (string component in objectComponents[itemName]) {
            var item = Resources.Load(component);
            Instantiate(item);
            inventory.Remove(component);
        }
    }

    public void complete()
    {
        // Items that are just being delivered have a process time of float.MinValue
        // therefore if the remaining time is not that, the item needs to be stored somewhere
        if (remaining_time != float.MinValue) {
            if (processObject.tag == "Store") {
                processObject.GetComponent<Store>().Add(itemName);
                Destroy(gameObject);
            }
        }
        else {
        // in this case, a container is taken from the store and the current item is place in it
            if (processObject.tag == "Store") {
                Transform old_transform = gameObject.transform;
                var container = Resources.Load(processObject.GetComponent<Store>().Pop());
                Item containerItem = (Item) Instantiate(container, old_transform);
                containerItem.inventory.Add(itemName);
            }
            else {
                if (objectComponents.ContainsKey(itemName))
                    EmptyContents();
                else
                    Destroy(gameObject);
            }
        }
    }

    public bool isUnavailable()
    {
        return beingCarried || remaining_time < total_time;
    }
    public void setBeingCarried(bool _beingCarried) 
    {
        beingCarried = _beingCarried;
    }
}
