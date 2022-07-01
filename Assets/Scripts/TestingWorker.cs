using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestingWorker : MonoBehaviour
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

        TextBox = gameObject.transform.Find("ActionText").GetComponent<TextMeshPro>();
    }

    // Update is called once per frame
    void Update()
    {
        try {
            UpdateTextBox($"Task: {nextAction} {nextItem.GetName()} Destination: {destination} Hands: {heldItem}");
        }
        catch {
            UpdateTextBox($"...");
        }
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
        nextItem.selectForTask();
        SwitchAction(nextItem.GetProcessType());
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
                switch (DecideOnTask())
                {
                    case true:
                        nextItem.selectForTask();
                        SwitchAction(nextItem.GetProcessType());
                        break;

                    case false:
                        nmAgent.SetDestination(GetRandomPoint(position, wanderDistance));
                        break;

                    default:
                        break;
                }
                break;

            case "collect":
                CollectItem(nextItem);
                SwitchAction(heldItem.GetProcessType());
                break;

            case "retrieve":
                switch (TakeOutFrom(nextItem.store)) {
                    case true:
                        SwitchAction(heldItem.GetProcessType());
                        break;

                    default:
                        break;     
                };
                break;

            case "deliver":
                switch (DeliverItem()) {
                    case true:
                        SwitchAction(nextItem.GetProcessType());
                        break;
                    
                    default:
                        break;    
                };
                break;
                
            case "contain":
                switch (ContainItem()) {
                    case true:
                        SwitchAction(nextItem.GetProcessType());
                        break;
                    
                    default:
                        break;    
                };
                break;

            case "store":
                switch (StoreItem(heldItem.store)) {
                    case true:
                        SwitchAction(nextItem.GetProcessType());
                        break;

                    default:
                        break;
                };
                break;

            case "process":
                switch (Process(nextItem)) {
                    case true:
                        SwitchAction(nextItem.GetProcessType());
                        break;
                    
                    case false:
                        break;
                }
                break;

            case "perform action":
            // if main required, go for it. May destroy object
                if (nextItem.requiresMaintenance()) {
                    nextItem.decrementProcessTime();
                }
                else {
                    nextAction = "collect";
                }
                break;

            case "complete":
                nextAction = "record task";
                break;
            
            case "record task":
                // Terminal state.
                // Only the Agent can reset a plan tree.
                break;

            default:
                break;
        }
    }

    // sets the action and applies the implict traversal
    void SwitchAction(string action)
    {
        nextAction = action;

        switch (nextAction) {
            case "decide":
                GoToObject(gameObject);
                break;
            
            case "collect":
                break;

            case "process":
                break;

            case "retrieve":
                GoToObject(nextItem.store.gameObject);
                break;

            case "deliver":
                GoToObject(heldItem.GetProcessObject());
                break;

            case "store":
                GoToObject(heldItem.store.gameObject);
                break;


        }
    }


    bool? DecideOnTask()
    {
        // Final null check in case a facility has interjected with a transition task
        if ((destination - position).magnitude <= grabDistance) {
            foreach (GameObject itemObject in GetObjectsInRange(gameObject.transform.position, "Item")) {
                Item potentialNextItem = itemObject.GetComponent<Item>();
                if (potentialNextItem.isAvailable()) {
                    nextItem = potentialNextItem;
                    return true;
                }
            }
            return false;
        }
        return new bool?();
    }

    void CollectItem(Item _nextItem)
    {
        heldItem = _nextItem;
        heldItem.setBeingCarried(true);
        heldItem.complete();
    }

    bool TakeOutFrom(Store _store)
    {
        if ((destination - position).magnitude <= grabDistance) {
            _store.Pop();
            CollectItem(nextItem);
            return true;
        }
        return false;
    }

    bool DeliverItem()
    {
        // make sure that the destination is set to the nextItem, so ensures that tasks can be specified from facility
        if ((destination - position).magnitude <= grabDistance) {
            heldItem.gameObject.transform.position = gameObject.transform.position + new Vector3(0, 0.5f, 0);
            //_item.gameObject.transform.localScale = new Vector3(1, 1, 1);

            heldItem.setBeingCarried(false);

            nextItem = heldItem;
            nextItem.complete();

            heldItem = null;
            return true;
        }
        return false;
        
    }

    bool ContainItem() {
        // puts the nextItem into the currently held item
        // NOT DONE
        if ((destination - position).magnitude <= grabDistance) {
            heldItem.AddToInventory(nextItem);
            nextItem.complete();
            return true;
        }
        return false;
    }

    bool StoreItem(Store _store)
    {
        if ((destination - position).magnitude <= grabDistance) {
            _store.Add(heldItem);
            heldItem.setBeingCarried(false);

            nextItem = heldItem;
            heldItem = null;

            nextItem.complete();
            return true;
        }
        return false;
    }

    bool Process(Item _item)
    {
        if (!_item.isEmpty()) {
            Disassemble(_item);
            return false;
        }

        if (_item.requiresMaintenance()) {
            _item.decrementProcessTime();
            return false;
        }
        else {
            _item.complete();
            return true;
        }
    }


/* --------------------- empty the item's contents, and process each of them -------------------- */
    void Disassemble(Item _nextItem)
    {
        foreach(Item _item in _nextItem.EmptyContents()) {
            Debug.Log(_item.name);
            _item.ResetTaskList(_item.gameObject, "process");
            Instantiate(_item);
        };
    }


    public List<GameObject> FindNearestWorkers() {
        List<GameObject> workers = GetObjectsInRange(position, "Agent");
        workers.Remove(gameObject);
        return workers;
    }

    public void SetDestination(Vector3 _destination) {
        destination = _destination;
        nmAgent.SetDestination(destination);
    }

    void GoToObject(GameObject objectToTravelTo) {
        destination = objectToTravelTo.transform.position;
        nmAgent.SetDestination(destination);
    }
}
