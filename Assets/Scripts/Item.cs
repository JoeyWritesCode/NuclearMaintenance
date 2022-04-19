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

    /* -------------------------- These describe HOW the item is processed -------------------------- */

    public GameObject storeObject;
    private GameObject processObject;
    public List<(GameObject, string)> tasks;
    private int amountOfCompletedTasks = 0;
    public string typeOfProcess;

    private Facility nextFacility;
    /* public Dictionary<string, List<string>> processInformation; */

    public bool isEmpty = false;
    public bool isTransitioning = false;
    public string itemName;
    private bool hasBeenSelected = false;

    public List<Item> inventory;

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

        remaining_time = total_time;

        // No longer using colliders, so this is a little redundant. 
        Debug.Log($"{gameObject.name} will be aware of the floor goddamit!");
        Physics.IgnoreCollision(this.GetComponent<Collider>(), GameObject.Find("Floor").GetComponent<Collider>(), false);   

        inventory = new List<Item>();
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

    public bool requiresProcessing()
    {
        return remaining_time > 0 || inventory.Count > 0;
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

    private (GameObject, string) GetTask(int taskIndex)
    {
        if (taskIndex >= tasks.Count) {
            return (gameObject, "complete");
        }
        else {
            return tasks[taskIndex];
        }
    }

    public Vector3 GetProcessPosition()
    {
        return processObject.transform.position;
    }

    public bool inProcessPosition()
    {
        return (GetProcessPosition() - gameObject.transform.position).magnitude <= distance_threshold;
    }


    public string GetName()
    {
        return gameObject.name;
    }

    void UpdateTask(int taskIndex)
    {
        var (processObject, typeOfProcess) = GetTask(taskIndex);
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
                if (inventory.Count > 0) {
                    foreach (Item item in inventory) {
                        RemoveFromInventory(item);
                    }
                }
                UpdateTask(amountOfCompletedTasks++);
                break;

            case "empty contents":
                // Send the empty item to to it's container store
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

    public void RemoveFromInventory(Item _item)
    {
        float spawnRadius = 1.0f;
        Vector3 spawnPoint = gameObject.transform.position + Random.insideUnitSphere * spawnRadius;
        
        _item.gameObject.transform.position = spawnPoint;
        _item.gameObject.SetActiveRecursively(true);
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
}
