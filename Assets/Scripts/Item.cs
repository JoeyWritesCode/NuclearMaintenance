using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    private static Dictionary<string, float> itemMaintences;
    
    /* ----------------------- Variables detailing how this item is processed ----------------------- */
    public int time_spent_waiting;
    private float total_time = 50.0f;
    private float remaining_time;
    private float days_per_step = 0.2f;
    private float distance_threshold = 0.3f;

    private bool beingCarried = false;

    private Renderer renderer;

    /* -------------------------- These describe HOW the item is processed -------------------------- */

    public Vector3 processPosition;

    public string storeObjectName;
    public GameObject storeObject;
    public string processObjectName;
    private GameObject processObject;
    public string typeOfProcess;

    private bool needsMaintenance;
    private Facility nextFacility;
    /* public Dictionary<string, List<string>> processInformation; */

    public bool isEmpty = false;
    public bool isTransitioning = false;
    public string itemName;


    public List<GameObject> inventory;

    // Start is called before the first frame update
    void Start()
    {
        renderer = gameObject.GetComponent<Renderer>();
        /* processInformation = objectsBelongWith[itemName];
        if (inventory.Count == 0)
            processObject = itemStore;
        else
            processObject = GameObject.Find(processInformation[0]);

        typeOfProcess = processInformation[1]; */
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

        processObject = GameObject.Find(processObjectName);

        processPosition = processObject.transform.position;
        remaining_time = total_time;

        // No longer using colliders, so this is a little redundant. 
        Debug.Log($"{gameObject.name} will be aware of the floor goddamit!");
        Physics.IgnoreCollision(this.GetComponent<Collider>(), GameObject.Find("Floor").GetComponent<Collider>(), false);   
    }

    void Update()
    {
        time_spent_waiting++;
    }

    /* ----------------------- This is to track which facility the item is in ----------------------- */
    /* private void OnTriggerEnter(Collider other) {
        if (other.tag == "Facility") {
            storeObject = other.GetComponent<Facility>().localMaterialStore;
        }
    } */

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
    public void SetProcessType(string _type)
    {
        typeOfProcess = _type;
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
        remaining_time = total_time;
    }


    public string GetName()
    {
        return gameObject.name;
    }


    public void complete()
    {        
        gameObject.tag = "Item";
        // Increment this objects progression. 
        // Could write to the program at this point too.
        
        // Bit of a weird temporary fix... Will have to think about this later
        /* if (isTransitioning)
            typeOfProcess = "delivery"; */

        switch (typeOfProcess) {
            // Only singleton items can be stored. Therefore if task is to deliver the item, you
            // must empty it's contents
            case "delivery":
                if (inventory.Count > 0)
                    EmptyContents();
                break;

            // Spawn a new container from this store, and add the current item to it's inventory
            /* case "merge":
                var container = Resources.Load(itemName + "Container");
                Instantiate(container, gameObject.transform.position, Quaternion.identity);
                Debug.Log(containerName);
                var container = Resources.Load(containerName);
                GameObject containerItem = (GameObject) Instantiate(container, gameObject.transform.position, Quaternion.identity);
                containerItem.GetComponent<Item>().inventory.Add(itemName);
                Destroy(gameObject);
                break; */

            // Increment the store's occupancy
            case "store":
                processObject.GetComponent<Store>().Add(gameObject);
                //nextFacility.targetObject = gameObject;
                gameObject.SetActiveRecursively(false);
                break;

            // Change this name! Used with fissle material
            case "scoop":
                inventory.Add(processObject);
                processObject.SetActiveRecursively(false);
                break;

            case "maintenance":
                needsMaintenance = false;
                break;
        }
    }

    void EmptyContents()
    {

        float spawnRadius = 1.0f;
        foreach (GameObject component in inventory) {
            Debug.Log(component.name);
            var item = Resources.Load(component.GetComponent<Item>().itemName);

            Vector3 spawnPoint = gameObject.transform.position + Random.insideUnitSphere * spawnRadius;
            Instantiate(item, spawnPoint, Quaternion.identity);
        }
        inventory = new List<GameObject>();

        // Bring this to where the empty one's go!
        SetEmptyDestinations();

        // For now, destroy Warheads after disassembling them.
        // We're going to need to announce this later - and potentially pass it to maintenance
        /* if (itemName != "Warhead") {
            var emptyItem = Resources.Load("Empty" + itemName);
            Instantiate(emptyItem, gameObject.transform.position, Quaternion.identity);
        }
        Destroy(gameObject); */
    }

    void SetEmptyDestinations()
    {
        processObjectName = storeObjectName;
        processObject = GameObject.Find(processObjectName);
        processPosition = processObject.transform.position;
        typeOfProcess = "store";
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
