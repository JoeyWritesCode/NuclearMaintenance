using ActressMas;

using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.AI;

using ActressMas;

public class SimpleWorker : MonoBehaviour
{

    /* ------------------------------------ The heads up display ------------------------------------ */
    private TextMeshPro TextBox;

    /* -------------------------------------- NavMesh variables ------------------------------------- */
    private NavMeshAgent nmAgent;
    private Vector3 destination;
    private Vector3 position;
    private float wanderDistance = 30.0f;
    Vector3 infinity = new Vector3(Single.PositiveInfinity, Single.PositiveInfinity, Single.PositiveInfinity);

    /* ------------------------------------ The Worker parameters ----------------------------------- */
    private float grabDistance = 1.0f;

    /* ---------------------------------- Task management variables --------------------------------- */
    private Item nextItem;
    private Item heldItem;
    public string nextAction = "decide";
    public string currentTask = null;
    private bool taskRecorded;

    /* ----------------------------- Facility parameters for global flow ---------------------------- */
    public Facility currentFacility;


    void Start()
    {
    /* --------------------------------- Set up internal structures --------------------------------- */
        nmAgent = gameObject.GetComponent<NavMeshAgent>();
        
        nextItem = null;
        heldItem = null;
        destination = gameObject.transform.position;

        TextBox = GetComponent<TextMeshPro>();
    }

    // Update is called once per frame
    void Update()
    {
        /* if (nextItem == null) {            
            UpdateTextBox("On the hunt!");
        }
        else{
            UpdateTextBox($"{nextAction} {nextItem.GetName()} {destination}");
        } */
    }

    private void OnTriggerEnter(Collider other) {
        if (other.tag == "Facility" && nextAction == "decide") {
            currentFacility = other.GetComponent<Facility>();
        }
    }

    void UpdateTextBox(string _textPrompt) {
        TextBox.text = _textPrompt;
    }

    /* ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ */
    /*                                       Auxiliary functions                                      */
    /* ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ */
    private List<GameObject> GetObjectsInRange(Vector3 position, string tag)
    {
        List<GameObject> seenObjects = new List<GameObject>();
        // totally arbitrary
        float visionDistance = (gameObject.GetComponent<Renderer>().bounds.size).magnitude * 5;

        Collider[] hitColliders = Physics.OverlapSphere(position, visionDistance);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.tag == tag) {
                seenObjects.Add(hitCollider.gameObject);
            }
        }        
        seenObjects.OrderBy(obj => (obj.transform.position - position).magnitude);
        return seenObjects;
    }

    public Vector3 GetRandomPoint(Vector3 center, float maxDistance) {
        // Get Random Point inside Sphere which position is center, radius is maxDistance
        Vector3 randomPos = UnityEngine.Random.insideUnitSphere * maxDistance + center;

        NavMeshHit hit = new NavMeshHit(); // NavMesh Sampling Info Container

        // from randomPos find a nearest point on NavMesh surface in range of maxDistance
        NavMesh.SamplePosition(randomPos, out hit, maxDistance, NavMesh.AllAreas);
        if (hit.position != infinity)
            return hit.position;
        else
            return GetRandomPoint(center, maxDistance);
    }

    bool isCloseTo(Vector3 locationOne, Vector3 locationTwo, float minimumDistance) {
        return (locationTwo - locationOne).magnitude <= minimumDistance;
    }

    public void assignItem(string _itemName) {
        nextItem = GameObject.Find(_itemName).GetComponent<Item>();
    }

    public string getTaskDetails() {
        return $"{nextItem.gameObject.name} {nextItem.itemName} {nextItem.GetLastAction()}";
    }

    public Item getCurrentItem() {
        return nextItem;
    }

    public void resetPlanTree() {
        nextItem.complete();
        nextItem = null;
        currentTask = null;
        nextAction = "decide";
    }

    /* ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ */
    /*                                        The Act function                                        */
    /* ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ */
    public void Act()
    {
        position = gameObject.transform.position;
        switch (nextAction) 
        {
            case "decide":
                DecideOnTask();
                if (nextItem != null) {
                    nextItem.selectForTask();

                    // Set the relevant destination to begin this process

                    switch (nextItem.GetProcessType()) {
                        case "collect":
                            GoToObject(nextItem.gameObject);
                            break;
                        default:
                            GoToObject(nextItem.GetProcessObject());
                            break;
                        /* default:
                            throw new InvalidOperationException($"Tasks can either begin with retrival or collection, not {nextItem.typeOfProcess}"); */
                    }
                    nextAction = nextItem.GetProcessType();
                }
                else {
                    if ((destination - position).magnitude <= grabDistance) {
                        // get random point in random facility?
                        destination = GetRandomPoint(position, wanderDistance);
                        nmAgent.SetDestination(destination);
                    }
                }
                break;

            case "retrieve container":
                if ((destination - position).magnitude <= grabDistance) {
                    Item containerItem = nextItem.GetProcessObject().GetComponent<Store>().Pop();
                    CollectItem(containerItem);
                    GoToObject(nextItem.gameObject);
                    nextAction = "contain";
                }
                break;

            case "remove":
                if ((destination - position).magnitude <= grabDistance) {
                    nextItem.GetProcessObject().GetComponent<Store>().Remove(nextItem);
                    CollectItem(nextItem);
                    GoToObject(currentFacility.gameObject);
                    nextAction = "finish task";
                }
                break;


            case "contain":
                if ((destination - position).magnitude <= grabDistance) {
                    heldItem.AddToInventory(nextItem);
                    nextItem.complete();
                    nextItem = null;

                    DeliverItem(heldItem);
                    /* heldItem.complete();
                    nextAction = "decide"; */
                    taskRecorded = false;
                    nextAction = "record task";
                }
                break;

            case "collect":
                if ((destination - position).magnitude <= grabDistance) {
                    // Maintenance and disassembly tasks are item dependent
                    // therefore they interupt the plan tree, but maintain the task
                    if (nextItem.requiresMaintenance()) {
                        GoToObject(gameObject);
                        nextAction = "perform action";
                    }
                    else {
                        CollectItem(nextItem);
                        nextItem.complete();

                        // Debug.Log($"Next task! {nextItem.typeOfProcess} with {nextItem.processObject.name} at {nextItem.GetProcessPosition()}");
                        // Set the task to complete this process
                        switch (nextItem.GetProcessType()) {
                            case "deliver":
                                GoToObject(nextItem.GetProcessObject());
                                break;
                            case "store":
                                //GoToObject(nextItem.storeObject);
                                GoToObject(nextItem.GetProcessObject());
                                break;
                            case "collect":
                                throw new InvalidOperationException($"Sync error. Attempting to collect the collected item {nextItem.name}");
                            default:    
                                throw new InvalidOperationException($"Collected items can either be delivered or stored, not {nextItem.typeOfProcess}");
                        }
                        currentTask = nextItem.GetProcessType();
                        nextAction = "finish task";
                    }
                }
                break;

            // Finish the task
            case "finish task":
                if ((destination - position).magnitude <= grabDistance) {                    
                    switch (currentTask) {
                        case "deliver":
                            DeliverItem(nextItem);
                            if (nextItem.isEmpty()) {
                                nextAction = "record task";
                            }
                            else {
                                nextItem.RemoveFromInventory();
                            }
                            break;
                        case "store":
                            DeliverItem(nextItem);
                            StoreItem(nextItem);
                            nextAction = "record task";
                            break;
                    }
                }
                break;

            case "perform action":
                if (nextItem.requiresMaintenance()) {
                    nextItem.decrementProcessTime();
                }
                else {
                    nextAction = "collect";
                }
                break;

            case "record task":
                // Terminal state.
                // Only the Agent can reset a plan tree.
                break;

            default:
                break;
        }
    }


    void DecideOnTask()
    {
        // Final null check in case a facility has interjected with a transition task
        foreach (GameObject itemObject in GetObjectsInRange(gameObject.transform.position, "Item")) {
            Item potentialNextItem = itemObject.GetComponent<Item>();
            if (potentialNextItem.isAvailable()) {
                nextItem = potentialNextItem;
                break;
            }
        }
    }

    void CollectItem(Item _nextItem)
    {
        _nextItem.gameObject.SetActiveRecursively(false);

        heldItem = _nextItem;
        _nextItem.gameObject.tag = "HeldItem";
    }

    void DeliverItem(Item _item)
    {
        _item.gameObject.transform.position = gameObject.transform.position + new Vector3(0, 0.5f, 0);
        _item.gameObject.SetActiveRecursively(true);

        _item.setBeingCarried(false);
        heldItem = null;
        _item.gameObject.tag = "Item";
    }

    void StoreItem(Item _item)
    {
        Store store = _item.GetProcessObject().GetComponent<Store>();
        store.Add(_item);
        _item.gameObject.transform.localScale = new Vector3(0, 0, 0);
    }


    public List<GameObject> FindNearestWorkers() {
        List<GameObject> workers = GetObjectsInRange(position, "Agent");
        workers.Remove(gameObject);
        return workers;
    }

    public void SetDestination(Vector3 _destination) {
        destination = _destination;
        nmAgent.SetDestination(_destination);
    }

    void GoToObject(GameObject objectToTravelTo) {
        destination = objectToTravelTo.transform.position;
        nmAgent.SetDestination(destination);
    }
}
