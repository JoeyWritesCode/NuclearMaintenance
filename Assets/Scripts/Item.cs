using System.Linq;
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

        public Task(GameObject _thisTasksObject, string _thisTasksProcessType) {
            thisTasksObject = _thisTasksObject;
            thisTasksProcessType = _thisTasksProcessType;
        }
    }

    /* -------------------------- These describe HOW the item is processed -------------------------- */

    public List<Task> tasks;
    private int amountOfCompletedTasks = 0;
    private int taskIndex = 0;
    public string typeOfProcess;

    private Facility nextFacility;
    /* public Dictionary<string, List<string>> processInformation; */

    public string itemName;
    private bool hasBeenSelected = false;

    public string maintenanceType;

    public List<Item> inventory;
    public Store store;
    public Item container;

    // Start is called before the first frame update
    void Start()
    {
        renderer = gameObject.GetComponent<Renderer>();

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
        // Debug.Log($"{gameObject.name} will be aware of the floor goddamit!");
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
        return GetProcessObject().transform.position;
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

    public void ResetTaskList()
    {
        tasks = new List<Task>();
        ResetTaskIndex();
    }
    public void ResetTaskList(GameObject _thisTasksObject, string _thisTasksProcessType)
    {
        Task task = new Task(_thisTasksObject, _thisTasksProcessType);
        tasks = new List<Task>() {task};
        Debug.Log($"{gameObject.name}'s task is now just {_thisTasksObject}, {_thisTasksProcessType}");
        ResetTaskIndex();
    }

    public void ResetTaskIndex() 
    {
        taskIndex = 0;
    }

    public void AmmendTaskList(GameObject newTaskObject, string newProcessType)
    {
        Task newTask = new Task(newTaskObject, newProcessType);
        tasks.Insert(taskIndex, newTask);
        Debug.Log($"Adding {newTaskObject}, {newProcessType} to {gameObject.name}");
        taskIndex++;
    }

    private Task GetTask()
    {
        if (taskIndex >= tasks.Count) {
            return new Task(null, "complete");
        }
        else {
            return tasks[taskIndex];
        }
    }

    public List<Task> GetTaskSpecification()
    {
        return tasks;
    }

    public string GetName()
    {
        return gameObject.name;
    }

    public GameObject GetProcessObject()
    {
        return GetTask().thisTasksObject;
    }
    public string GetProcessType()
    {
        return GetTask().thisTasksProcessType;
    }
    public string GetLastAction()
    {
        Task lastTask = (Task) tasks.Last();
        Debug.Log(lastTask.thisTasksProcessType);
        return lastTask.thisTasksProcessType;
    }


    public void complete()
    {        
        if (GetProcessType() != "maintenance") {
            taskIndex++;
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
        gameObject.SetActiveRecursively(!_beingCarried);
        gameObject.tag = _beingCarried ? "HeldItem" : "Item";
        beingCarried = _beingCarried;
    }

    public void selectForTask() {
        gameObject.tag = "ActiveItem";
    }

    public void deselectForTask() {
        gameObject.tag = "Item";
    }

    public bool isEmpty() {
        return inventory.Count == 0;
    }

    public bool isContainer() {
        return itemName.EndsWith("Container");
    }

    public List<Item> EmptyContents() {
        List<Item> contents = new List<Item>(inventory);
        inventory.Clear();
        return contents;
    }
}
