using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Facility : MonoBehaviour
{
    public string inputItemName; 
    public string watchedItemName;
    public GameObject targetObject;
    public GameObject localMaterialStore;
    public Facility nextFacility;

    public int amountOfComponents;
    public string nextPhase;
    private int amountOfTasksInProgress;
    private int lastAmountOfTasks;

    private int transitionTasks = 0;

    public FacilityAgent agent;
    private List<string> localFreeAgents;

    bool m_Started;
    public LayerMask m_LayerMask;

    void Start()
    {
        //Use this to ensure that the Gizmos are being drawn when in Play Mode.
        m_Started = true;
        localFreeAgents = new List<string>();
    }

    public string GetName()
    {
        return gameObject.name;
    }

    void FixedUpdate()
    {
        MyCollisions();
    }

    async void MyCollisions()
    {
        //Use the OverlapBox to detect if there are any other colliders within this box area.
        //Use the GameObject's centre, half the size (as a radius) and rotation. This creates an invisible box around your GameObject.
        Collider[] hitColliders = Physics.OverlapBox(gameObject.transform.position, transform.localScale / 2, Quaternion.identity, m_LayerMask);
        localFreeAgents = new List<string>();
        foreach (Collider hit in hitColliders) {            
            // May as well keep track of agents in here too
            if (hit.tag == "Agent") {
                hit.GetComponent<SimpleWorker>().currentFacility = this;
                localFreeAgents.Add(hit.name);
            }

            if (hit.tag == "Item")
                hit.GetComponent<Item>().storeObject = localMaterialStore;
        }

        /* if (i > 0) {
            amountOfTasksInProgress = Convert.ToInt32(Math.Ceiling( (double) i / amountOfComponents));
        }
        else 
            amountOfTasksInProgress = 0;

        Debug.Log($"We have {i} interesting objects, and {amountOfTasksInProgress} tasks going on!"); */

        // Kinda a weird way of organizing it...
        /* if (amountOfTasksInProgress < lastAmountOfTasks) {
            if (localFreeAgents.Count > 0) {
                Debug.Log($"Ready for {nextPhase}!"); 
                agent.InformAgents(localFreeAgents, nextPhase, targetObject);
                lastAmountOfTasks = amountOfTasksInProgress;
            }
        }
        else {
            lastAmountOfTasks = amountOfTasksInProgress;
        } */
        
    }

    //Draw the Box Overlap as a gizmo to show where it currently is testing. Click the Gizmos button to see this
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        //Check that it is being run in Play Mode, so it doesn't try to draw this in Editor mode
        if (m_Started)
            //Draw a cube where the OverlapBox is (positioned where your GameObject is as well as a size)
            Gizmos.DrawWireCube(transform.position, transform.localScale);
    }

    public void RecordCompletion(Item _item) {
        if (_item.itemName == watchedItemName && _item.inventory.Count != 0) {
            Debug.Log($"Nice! Let's tell everyone. There's {localFreeAgents.Count} to pick from");
            nextFacility.SetupNewTask(localMaterialStore.name);
        }
    }

    public void SetupNewTask(string lastMaterialStore) {
        agent.InformAgents(localFreeAgents, lastMaterialStore, gameObject.name);
    }
}
