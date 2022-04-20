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
    private string nextAction = "decide";
    private string currentTask;

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
        Debug.Log($"Task : {currentTask}");
        Debug.Log($"Action : {nextAction}");
    }

    private void OnTriggerEnter(Collider other) {
        if (other.tag == "Facility") {
            currentFacility = other.GetComponent<Facility>();
            Debug.Log($"I'm in {currentFacility.GetName()}");
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

    public bool isBusy() {
        return nextAction != "decide";
    }

    public void assignItem(string _itemName) {
        nextItem = GameObject.Find(_itemName).GetComponent<Item>();
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
                    switch (nextItem.typeOfProcess) {
                        case "retrieve container":
                            // make a plan
                            GoToObject(nextItem.storeObject);
                            break;
                        case "collect":
                            GoToObject(nextItem.gameObject);
                            break;
                        default:
                            throw new InvalidOperationException($"Tasks can either begin with retrival or collection, not {nextItem.typeOfProcess}");
                    }
                    nextAction = nextItem.typeOfProcess;
                }
                else {
                    if ((destination - position).magnitude <= grabDistance) {
                        // get random point in random facility?
                        destination = GetRandomPoint(position, wanderDistance);
                        Debug.Log($"New location is {destination}");
                        nmAgent.SetDestination(destination);
                    }
                }
                break;

            case "retrieve container":
                if ((destination - position).magnitude <= grabDistance) {
                    Item containerItem = nextItem.storeObject.GetComponent<Store>().Remove();
                    CollectItem(containerItem);
                    GoToObject(nextItem.gameObject);
                    nextAction = "contain";
                }
                break;

            case "contain":
                if ((destination - position).magnitude <= grabDistance) {
                    heldItem.AddToInventory(nextItem);
                    nextItem.complete();
                    nextItem = null;

                    DeliverItem(heldItem);
                    heldItem.complete();

                    nextAction = "decide";
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

                        Debug.Log($"Next task! {nextItem.typeOfProcess} with {nextItem.processObject.name} at {nextItem.GetProcessPosition()}");
                        // Set the task to complete this process
                        switch (nextItem.typeOfProcess) {
                            case "deliver":
                                GoToObject(nextItem.processObject);
                                break;
                            case "store":
                                //GoToObject(nextItem.storeObject);
                                GoToObject(nextItem.processObject);
                                break;
                            default:    
                                throw new InvalidOperationException($"Collected items can either be delivered or stored, not {nextItem.typeOfProcess}");
                        }
                        currentTask = nextItem.typeOfProcess;
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
                                nextItem.complete();
                                nextItem = null;
                            }
                            else {
                                nextItem.RemoveFromInventory();
                            }
                            break;
                        case "store":
                            DeliverItem(nextItem);
                            StoreItem(nextItem);
                            nextItem.complete();
                            Debug.Log($"{nextItem.itemName} stored!");
                            nextItem = null;
                            break;
                    }
                }
                if (nextItem == null) {
                    currentTask = null;
                    nextAction = "decide";
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
        _item.gameObject.transform.position = gameObject.transform.position + new Vector3(0, 1, 0);
        _item.gameObject.SetActiveRecursively(true);

        _item.setBeingCarried(false);
        heldItem = null;
        _item.gameObject.tag = "Item";
    }

    void StoreItem(Item _item)
    {
        Store store = _item.storeObject.GetComponent<Store>();
        store.Add(_item);
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
