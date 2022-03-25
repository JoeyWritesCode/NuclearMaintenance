using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.AI;

using ActressMas;

public class Worker : MonoBehaviour
{
    /* ----------------------------- The GameObject used for picking up ----------------------------- */
    // - Could we just use the Worker as the parent object, with a small offset?
    public GameObject Hands;

    /* ------------------------------------ The heads up display ------------------------------------ */
    public TMP_Text TextBox;
    private string textPrompt;
    private string lastPrompt;

    /* -------------------------------------- NavMesh variables ------------------------------------- */
    private NavMeshAgent nmAgent;
    private Vector3 destination;
    private Vector3 processPosition;
    private float wanderDistance = 7.5f;

    /* --------------------- Associated agent within the ActressMas environment --------------------- */
    private Agent agent;

    /* ------------------------------------- The BDI structures ------------------------------------- */
    // - Right now, not used
    private Dictionary<string, Vector3> _beliefs;
    private HashSet<string> _desires;
    private string _intention; // only 1 intention active in this model
    private string newIntention;
    private List<string> _plan;
    private bool _needToReplan;

    /* ------------------------------------ The Worker parameters ----------------------------------- */
    private float visionDistance = 10.0f;
    private float grabDistance = 1.0f;
    private float travelSpeed = 5.0f;
    private float processSpeed = 1.0f;

    /* ---------------------------------- Task management variables --------------------------------- */
    private Item nextItem;
    private bool isCarrying;
    private bool delivered;
    private string nextAction;

    /* ------------------------------------ Simulation parameters ----------------------------------- */
    private int stepsBetweenObservations = 5;
    private int steps = 0;

    void Start()
    {
    /* --------------------------------- Set up internal structures --------------------------------- */
        _beliefs = new Dictionary<string, Vector3>();

        isCarrying = false;
        delivered = false;

        nmAgent = gameObject.GetComponent<NavMeshAgent>();
        
        nextItem = null;
        destination = gameObject.transform.position;

        nextAction = "decide";
        textPrompt = "decide";
    }

    // Update is called once per frame
    void Update()
    {
        if (nextItem == null) {
            UpdateTextBox("deciding");
        }
        else {
            UpdateTextBox($"{nextAction} {nextItem.GetName()} {destination}");
        }
        steps++;
        if (steps == stepsBetweenObservations) {
            _beliefs = GetObjectsInRange(gameObject.transform.position);
            steps = 0;
        }
        Act();
    }

    void UpdateTextBox(string _textPrompt) {
        TextBox.text = _textPrompt;
    }

    /* ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ */
    /*                                       Auxiliary functions                                      */
    /* ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ */
    private Dictionary<string, Vector3> GetObjectsInRange(Vector3 position)
    {
        Dictionary<string, Vector3> seenObjects = new Dictionary<string, Vector3>();

        Collider[] hitColliders = Physics.OverlapSphere(position, visionDistance);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.tag == "Item") {
                seenObjects[hitCollider.gameObject.name] = hitCollider.gameObject.transform.position;
            }
        }
        return seenObjects;
    }

    float TravelEffort(string key) {
        Item item = GameObject.Find(key).GetComponent<Item>();
        float time_to_item = (item.GetPosition() - gameObject.transform.position).magnitude / travelSpeed;
        float time_to_carry = (item.GetProcessPosition() - item.GetPosition()).magnitude / travelSpeed;
        float time_to_process = item.GetRemainingTime() / processSpeed;
        float score = time_to_item + time_to_carry + time_to_process - item.GetTimeSpentWaiting();
        Debug.Log($"{key} has a score of {score}");
        return score;
    }

    public Vector3 GetRandomPoint(Vector3 center, float maxDistance) {
        // Get Random Point inside Sphere which position is center, radius is maxDistance
        Vector3 randomPos = UnityEngine.Random.insideUnitSphere * maxDistance + center;

        NavMeshHit hit; // NavMesh Sampling Info Container

        // from randomPos find a nearest point on NavMesh surface in range of maxDistance
        NavMesh.SamplePosition(randomPos, out hit, maxDistance, NavMesh.AllAreas);

        return hit.position;
    }

    bool isCloseTo(Vector3 locationOne, Vector3 locationTwo, float minimumDistance) {
        return (locationTwo - locationOne).magnitude <= minimumDistance;
    }

    /* ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ */
    /*                                GameObject interaction functions                                */
    /* ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ */
    private void toggleCarry() {
        if (isCarrying) {
            nextItem.gameObject.transform.parent = null;
            nextItem.gameObject.GetComponent<Rigidbody>().useGravity = true;
        }
        else {
            nextItem.gameObject.GetComponent<Rigidbody>().useGravity = false;
            nextItem.gameObject.transform.position = gameObject.transform.position + new Vector3(0, 1, 1);
            nextItem.gameObject.transform.parent = gameObject.transform;
            isCarrying = true;
        }
    }

    /* ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ */
    /*                                        The Act function                                        */
    /* ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ */
    public void Act()
    {
        switch (nextAction) 
        {
            // Let's step this out for now...
            case "decide":
                DecideOnTask();
                if (nextItem != null) {
                    /* if ((nextItem.transform.position - gameObject.transform.position).magnitude > wanderDistance * 5) {
                        nextItem = null;
                        break;
                    } */

                    if (nextItem.inProcessPosition()) {
                        destination = nextItem.GetProcessPosition();
                        nmAgent.SetDestination(destination);

                        nextAction = "process";
                        break;
                    }
                    else {
                        destination = nextItem.GetPosition();
                        processPosition = nextItem.GetProcessPosition();
                        nmAgent.SetDestination(destination);

                        nextAction = "collect"; 
                        break;
                    }
                }
                else {
                    if ((destination - gameObject.transform.position).magnitude <= grabDistance) {
                        destination = GetRandomPoint(gameObject.transform.position, wanderDistance);
                        Debug.Log($"New location is {destination}");
                        nmAgent.SetDestination(destination);
                    }
                    break;
                }


            case "collect":
                if (nextItem.isUnavailable()) {
                    Debug.Log("Someone picked that up!");
                    nextItem = null;

                    destination = gameObject.transform.position;
                    nmAgent.SetDestination(destination);

                    nextAction = "decide";
                    break;
                }
                else {
                    if ((nextItem.GetPosition() - gameObject.transform.position).magnitude <= grabDistance) {
                        CollectItem();
                        nextItem.gameObject.tag = "HeldItem";

                        //destination = nextItem.GetProcessPosition();
                        nmAgent.SetDestination(processPosition);

                        nextAction = "deliver";
                        break;
                    }
                    else {
                        Debug.Log($"We're on our way to {destination} to collect {nextItem.GetName()}");
                        break;
                    }
                }

            case "deliver":
                Debug.Log($"We're on our way to {processPosition} to deliver {nextItem.GetName()}");

                if ((processPosition - gameObject.transform.position).magnitude <= grabDistance) {
                    DeliverItem();
                    
                    nmAgent.SetDestination(gameObject.transform.position);
                    
                    nextItem.gameObject.tag = "ActiveItem";
                    nextAction = "process";
                }
                break;

            case "process":
                if (nextItem == null) {
                    nextAction = "decide";
                    break;
                }
                else {
                    if (nextItem.GetRemainingTime() <= 0) {
                        nextItem.complete();
                        _beliefs.Remove(nextItem.GetName());

                        nextItem = null;

                        destination = GetRandomPoint(gameObject.transform.position, wanderDistance);
                        nmAgent.SetDestination(destination);

                        nextAction = "decide";
                        break;
                    }
                    else {
                        //Debug.Log($"Remaining time: {nextItem.GetRemainingTime()}");
                        nextItem.decrementProcessTime();
                        break;
                    }
                }

            default:
                break;
        }
    }

    void DecideOnTask()
    {
        // List<string> availableTasks = new List<string>(_beliefs.Keys);
        List<string> availableTasks = new List<string>(GetObjectsInRange(gameObject.transform.position).Keys);
        availableTasks.OrderBy(name => TravelEffort(name));

        // We may get to the point where there are no more beliefs held.
        foreach (string item in availableTasks) {
            nextItem = GameObject.Find(item).GetComponent<Item>();
        
            // This is a temporary fix (ish) for when objects fall through the floor. As long as we try to make sure 
            // not to do that.
            if ((nextItem.GetPosition() - gameObject.transform.position).magnitude <= 5 * wanderDistance && !nextItem.isUnavailable()) {
                Debug.Log($"Found {nextItem.GetName()}! Going to {nextItem.GetPosition()}");
                break;
            }
            else {
                nextItem = null;
            }
        }
    }

    void CollectItem()
    {
        // check if distance to item is less than distance threshold
            Debug.Log($"Let's pick up this {nextItem.GetName()}");
            nextItem.gameObject.GetComponent<Rigidbody>().useGravity = false;
            nextItem.gameObject.transform.position = gameObject.transform.position + new Vector3(0, 1, 1);
            nextItem.gameObject.transform.parent = gameObject.transform;

            nextItem.setBeingCarried(true);
            nextItem.gameObject.tag = "HeldItem";
    }

    void DeliverItem()
    {
        Debug.Log($"Let's drop this {nextItem.GetName()}");
        nextItem.gameObject.transform.parent = null;
        nextItem.gameObject.GetComponent<Rigidbody>().useGravity = true;

        nextItem.setBeingCarried(false);
        nextItem.gameObject.tag = "Item";
    }

    void ProcessItem()
    {
        nextItem.decrementProcessTime();
        if (nextItem.isProcessed()) {
            nextItem.complete();
            _beliefs.Remove(nextItem.GetName());
            nextItem = null;
            destination = Vector3.zero;
        }
    }
}
