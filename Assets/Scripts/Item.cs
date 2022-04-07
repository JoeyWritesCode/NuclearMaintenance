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
    private float distance_threshold = 0.3f;

    private bool beingCarried = false;

    private Renderer renderer;

    public Vector3 processPosition;
    private GameObject processObject;
    private List<string> processInformation;

    public bool isEmpty = false;
    public string itemName;

    private string typeOfProcess;

    private Dictionary<string, List<string>> objectsBelongWith = new Dictionary<string, List<string>> {
        {"WarheadTransitContainer", new List<string>{"GoodsInOut", "delivery"}},
        {"EmptyWarheadTransitContainer", new List<string>{"StoreContainersWarheadTransit", "store"}},
        {"WarheadContainer", new List<string>{"StoreWarhead", "store"}},
        {"EmptyWarheadContainer", new List<string>{"StoreContainersWarhead", "store"}},
        {"Warhead", new List<string>{"Disassembly", "delivery"}},
        {"MaterialA", new List<string>{"StoreContainersMaterialA", "merge"}},
        {"MaterialAContainer", new List<string>{"StoreMaterialA", "store"}},
        {"MaterialB", new List<string>{"StoreContainersMaterialB", "merge"}},
        {"MaterialBContainer", new List<string>{"StoreMaterialB", "store"}},
        {"NonFissle", new List<string>{"StoreContainersNonFissle", "merge"}},
        {"NonFissleContainer", new List<string>{"StoreNonFissle", "store"}},
        {"EmptyWarhead", new List<string>{"StoreNonFissle", "store"}},
        {"CompletedWarhead", new List<string>{"StoreContainersWarheadTransit", "merge"}},
        };

    private Dictionary<string, List<string>> objectComponents = new Dictionary<string, List<string>> {
        {"WarheadTransitContainer", new List<string>{"WarheadContainer"}},
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

        typeOfProcess = processInformation[1];
        switch (typeOfProcess) {
            case "delivery":
                total_time = float.MinValue;
                break;
            case "store":
                total_time = 5.0f;
                break;
            case "merge":
                total_time = 10.0f;
                break;
        }

        processPosition = processObject.transform.position;
        remaining_time = total_time;

        if (itemName.StartsWith("Empty"))
            isEmpty = true;
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
            Instantiate(item, gameObject.transform.position, Quaternion.identity);
            inventory.Remove(component);
        }

        // For now, destroy Warheads after disassembling them.
        // We're going to need to announce this later - and potentially pass it to maintenance
        if (itemName != "Warhead") {
            var emptyItem = Resources.Load("Empty" + itemName);
            Instantiate(emptyItem, gameObject.transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    public void complete()
    {        
        gameObject.tag = "Item";
        switch (typeOfProcess) {
            // Only singleton items can be stored. Therefore if task is to deliver the item, you
            // must empty it's contents
            case "delivery":
                if (!isEmpty)
                    EmptyContents();
                break;

            // Spawn a new container from this store, and add the current item to it's inventory
            case "merge":
                var container = Resources.Load(itemName + "Container");
                Instantiate(container, gameObject.transform.position, Quaternion.identity);
                /* Debug.Log(containerName);
                var container = Resources.Load(containerName);
                GameObject containerItem = (GameObject) Instantiate(container, gameObject.transform.position, Quaternion.identity);
                containerItem.GetComponent<Item>().inventory.Add(itemName); */
                Destroy(gameObject);
                break;

            // Increment the store's occupancy
            case "store":
                processObject.GetComponent<Store>().Add();

                Destroy(gameObject);
                break;
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
