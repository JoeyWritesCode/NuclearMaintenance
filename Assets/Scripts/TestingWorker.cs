using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestingWorker : MonoBehaviour
{
    /* ---------------------------------- Task management variables --------------------------------- */
    private Item nextItem;
    private Item heldItem;
    public string nextAction = "decide";
    public string currentTask = null;
    private bool taskRecorded;

    /* ----------------------------- Facility parameters for global flow ---------------------------- */
    public Facility currentFacility;

    private Vector3 destination;


    void Start()
    {
    /* --------------------------------- Set up internal structures --------------------------------- */
        nextItem = null;
        heldItem = null;
    }


    private void OnTriggerEnter(Collider other) {
        if (other.tag == "Facility" && nextAction == "decide") {
            currentFacility = other.GetComponent<Facility>();
        }
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
        switch (nextAction) 
        {
            case "decide":
                switch (DecideOnTask())
                {
                    case true:
                        nextItem.selectForTask();
                        SwitchAction(nextItem.GetProcessType());
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
        if (true) {
            _store.Pop();
            CollectItem(nextItem);
            return true;
        }
        return false;
    }

    bool DeliverItem()
    {
        // make sure that the destination is set to the nextItem, so ensures that tasks can be specified from facility
        if (true) {
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
        if (true) {
            heldItem.AddToInventory(nextItem);
            nextItem.complete();
            return true;
        }
        return false;
    }

    bool StoreItem(Store _store)
    {
        if (true) {
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


    void GoToObject(GameObject objectToTravelTo) {
        currentFacility = objectToTravelTo.GetComponent<Facility>();
        //destination = objectToTravelTo.transform.position;
    }
}
