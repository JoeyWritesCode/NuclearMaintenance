using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

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

    /* ----------------------------------- a fancy Task class ----------------------------------- */
    [System.Serializable]
    public class Task
    {
        public GameObject thisTasksObject;
        public string thisTasksProcessType;
    }

    /* -------------------------- These describe HOW the item is processed -------------------------- */

    public GameObject storeObject;
    public GameObject processObject;
    public List<Task> tasks;
    private int amountOfCompletedTasks = 0;
    public string typeOfProcess;

    private Facility nextFacility;
    /* public Dictionary<string, List<string>> processInformation; */

    public string itemName;
    private bool hasBeenSelected = false;

    public string maintenanceType;

    public List<Item> inventory;

    // Start is called before the first frame update
    void Start()
    {
        renderer = gameObject.GetComponent<Renderer>();

        UpdateTask(amountOfCompletedTasks);

        switch (maintenanceType) {
            case "minor":
                total_time = 5;
                break;
            case "major":
                total_time = 15;
                break;
            default:
                total_time = 0;
                break;
        }
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

    /* ------------------------------------ Processing functions ------------------------------------ */
    public bool requiresMaintenance()
    {
        return remaining_time > 0;
    }
    public Vector3 GetProcessPosition()
    {
        return processObject.transform.position;
    }
    public bool inProcessPosition()
    {
        return (GetProcessPosition() - gameObject.transform.position).magnitude <= distance_threshold;
    }
    public void decrementProcessTime()
    {
        remaining_time -= days_per_step;
        renderer.material.SetColor("_Color", new Color(remaining_time / total_time, 255, remaining_time / total_time));
    }

    private bool needsMaintenance()
    {
        return remaining_time > 0;
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

    private Task GetTask(int taskIndex)
    {
        return tasks[taskIndex];
    }

    public string GetName()
    {
        return gameObject.name;
    }

    void UpdateTask(int taskIndex)
    {
        if (taskIndex >= tasks.Count) {
            Debug.Log($"{gameObject.name} is done");
            processObject = null;
            typeOfProcess = "complete";
        }
        else {
            Task newTask = GetTask(taskIndex);
            processObject = newTask.thisTasksObject;
            typeOfProcess = newTask.thisTasksProcessType;
        }
    }

    /* public void PerformOneStepOfProcess()
    {
        if (inventory.Count > 0) {
            RemoveFromInventory();
        }
        else if (remaining_time > 0) {
            decrementProcessTime();
        }
    } */


    public void complete()
    {        
        // Increment this objects progression. 
        // Could write to the program at this point too.
        
        // Bit of a weird temporary fix... Will have to think about this later
        /* if (isTransitioning)
            typeOfProcess = "delivery"; */

        switch (typeOfProcess) {
            case "contain":
                // When a material is placed in a container, it either:
                    // prepares itself for it's final task
                    // has been fully processed
                /* if (amountOfCompletedTasks++ < tasks.Count) {
                    UpdateTask(amountOfCompletedTasks);
                }
                else {
                    typeOfProcess = "completed";
                } */
                UpdateTask(amountOfCompletedTasks++);
                break;

            case "collect":
                UpdateTask(amountOfCompletedTasks++);
                break;
            
            case "deliver":
                UpdateTask(amountOfCompletedTasks++);
                break;

            // Increment the store's occupancy
            case "store":
                UpdateTask(amountOfCompletedTasks++);
                break;

            case "maintenance":
                UpdateTask(amountOfCompletedTasks);
                break;
        }
    }

    public void RemoveFromInventory()
    {
        float spawnRadius = 1.0f;
        Vector3 spawnPoint = gameObject.transform.position + Random.insideUnitSphere * spawnRadius;
        
        Item _item = inventory[0];
        // if an unspawned object
        if (GameObject.Find(_item.gameObject.name) != null) {
            _item.transform.position = spawnPoint;
            _item.gameObject.SetActiveRecursively(true); 
        }
        else {
            Debug.Log($"Spawning {_item.itemName}!");
            GameObject itemObject = Instantiate(Resources.Load(_item.itemName)) as GameObject;
            itemObject.transform.position = spawnPoint;
        }
        inventory.Remove(_item);
    }

    public void AddToInventory(Item _item)
    {
        inventory.Add(_item);
        _item.gameObject.SetActiveRecursively(false);
    }

    public bool isAvailable()
    {
        return gameObject.tag == "Item";
    }

    public void setBeingCarried(bool _beingCarried) 
    {
        beingCarried = _beingCarried;
    }

    public void selectForTask() {
        gameObject.tag = "ActiveItem";
    }

    public void deselectForTask() {
        gameObject.tag = "Item";
    }

    public bool isEmpty() {
        Debug.Log($"I'm off! But this inventory has {inventory.Count} items. Have a good break!");
        return inventory.Count == 0;
    }
}
