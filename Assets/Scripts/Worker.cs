using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

using ActressMas;

public class Worker : MonoBehaviour
{
    /* ----------------------------- The GameObject used for picking up ----------------------------- */
    // - Could we just use the Worker as the parent object, with a small offset?
    public GameObject Hands;

    /* -------------------------------------- NavMesh variables ------------------------------------- */
    private NavMeshAgent nmAgent;
    private Vector3 destination;
    private float wanderDistance = 5.0f;

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
    private float grabDistance = 2.0f;
    private float travelSpeed = 5.0f;
    private float processSpeed = 1.0f;

    /* ---------------------------------- The next associated item ---------------------------------- */
    private Item nextItem;
    private bool isCarrying;
    private bool delivered;
    private string nextAction;

    /* ------------------------------------ Simulation parameters ----------------------------------- */
    private int stepsBetweenObservations = 10;
    private int steps = 0;

    void Start()
    {
    /* --------------------------------- Set up internal structures --------------------------------- */
        _beliefs = new Dictionary<string, Vector3>();

        isCarrying = false;
        delivered = false;

        nmAgent = gameObject.GetComponent<NavMeshAgent>();
        
        nextItem = null;
        destination = Vector3.zero;

        nextAction = "decide";
    }

    // Update is called once per frame
    void Update()
    {
        steps++;
        if (steps == stepsBetweenObservations) {
            _beliefs = GetObjectsInRange(gameObject.transform.position);
            steps = 0;
        }
        Debug.Log(nextAction);
        Act();
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
                if (nextItem != null) {
                    if (nextItem.inProcessPosition()) {
                        nextAction = "process";
                        destination = nextItem.GetProcessPosition();
                        nmAgent.SetDestination(destination);
                        break;
                    }
                    else {
                        nextAction = "collect"; 
                        destination = nextItem.GetPosition();
                        nmAgent.SetDestination(destination);
                        break;
                    }
                }
                else 
                    DecideOnTask();
                    break;

            case "collect":
                Debug.Log($"We're on our way to {nextItem.GetPosition()} to {nextAction} {nextItem.GetName()}");
                
                if (isCarrying) {
                    nextAction = "deliver";
                    break;
                }
                else {
                    CollectItem();
                    break;
                }

            case "deliver":
                Debug.Log($"We're on our way to {nextItem.GetProcessPosition()} to deliver {nextItem.GetName()}");

                if (delivered) {
                    Debug.Log($"We have delivered {nextItem.GetName()}");
                    
                    delivered = false;
                    nextAction = "process";
                    destination = gameObject.transform.position;
                    nmAgent.SetDestination(destination);
                    break;
                }
                else
                    DeliverItem();
                    break;

            case "process":
                if (nextItem == null) {
                    nextAction = "decide";
                    break;
                }
                else {
                    nextItem.decrementProcessTime();

                    if (nextItem.GetRemainingTime() <= 0) {
                        nextItem.complete();
                        _beliefs.Remove(nextItem.GetName());
                        nextItem = null;
                        break;
                    }
                    else {
                        //Debug.Log($"Remaining time: {nextItem.GetRemainingTime()}");
                        break;
                    }
                }
                

            default:
                break;
        }
    }

    void DecideOnTask()
    {
        if (destination == Vector3.zero) {
            destination = GetRandomPoint(gameObject.transform.position, wanderDistance);
            Debug.Log($"New location is {destination}");
            nmAgent.SetDestination(destination);
        }
        else {
            if ((destination - gameObject.transform.position).magnitude <= grabDistance) {
                List<string> availableTasks = new List<string>(_beliefs.Keys);
                availableTasks.OrderBy(name => TravelEffort(name));

                // We may get to the point where there are no more beliefs held.
                if (availableTasks.Count() >= 1) {
                    nextItem = GameObject.Find(availableTasks[0]).GetComponent<Item>();

                    Debug.Log($"Found {nextItem.GetName()}! Going to {nextItem.GetPosition()}");
                    destination = nextItem.GetPosition();
                    nmAgent.SetDestination(destination);
                }
                else {
                    destination = Vector3.zero;
                }
            }
            else {
                Debug.Log($"Current position {gameObject.transform.position} is more than {grabDistance} from {destination}, so continue");
            }
        }
    }

    void CollectItem()
    {
        // check if distance to item is less than distance threshold
        if ((nextItem.GetPosition() - gameObject.transform.position).magnitude > grabDistance)
            Debug.Log((nextItem.GetPosition() - gameObject.transform.position).magnitude);
        else {
            Debug.Log($"Let's pick up this {nextItem.GetName()}");
            nextItem.gameObject.GetComponent<Rigidbody>().useGravity = false;
            nextItem.gameObject.transform.position = gameObject.transform.position + new Vector3(0, 1, 1);
            nextItem.gameObject.transform.parent = gameObject.transform;
            nextItem.gameObject.tag = "HeldItem";

            destination = nextItem.GetProcessPosition();
            nmAgent.SetDestination(destination);
            nextAction = "deliver";
        }
    }

    void DeliverItem()
    {
        // check if distance to item is less than distance threshold
        if ((nextItem.GetProcessPosition() - gameObject.transform.position).magnitude <= nextItem.distance_threshold) {
            Debug.Log($"Let's drop this {nextItem.GetName()}");
            nextItem.gameObject.transform.parent = null;
            nextItem.gameObject.GetComponent<Rigidbody>().useGravity = true;

            nextItem.gameObject.tag = "Item";
            delivered = true;
        }
        else {
            float distance = (nextItem.GetProcessPosition() - gameObject.transform.position).magnitude;
            Debug.Log($"This agent is currently {distance} away from it's process position {nextItem.GetProcessPosition()}. We have to be at least {nextItem.distance_threshold} in order to drop this item.");
        }
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
