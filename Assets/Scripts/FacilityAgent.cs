using ActressMas;

using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

public class FacilityAgent : Agent
{

    public Facility facility;
    
    private List<string> localAgents;
    private int agentCounter = 0;
    private List<string> transitionActions;

    private string lastMaterialStore;
    private string nameOfDestination;

    private string auctionedItemName;

    private string phase;

    public override void Setup()
    {
        Debug.Log($"{Name} is ready to assign work!");
    }

    private Item ObjectNameToItem(string name) {
        return GameObject.Find(name).GetComponent<Item>();
    }

    // This is only used for when receiving messages from the agent's BDI model. 
    // Could be target percept requests, or a new action to add to the actionList
    public override void Act(Message message)
    {
        try
        {
            Console.WriteLine($"\t{message.Format()}");
            message.Parse(out string action, out List<String> taskInfo);

            switch (action)
            {
/* ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ */
/*                                    A task has been completed                                   */
/* ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ */
                case "finished":
                    string itemObjectName = taskInfo[0];
                    string itemName = taskInfo[1];
                    string typeOfProcess = taskInfo[2];

                    Debug.Log($"{message.Sender} has just {typeOfProcess}'d {itemObjectName}");
                   
/* ------------------------------ Check the output task conditions ------------------------------ */

                    if (facility.watchedTask.thisTasksObject.name == itemName && facility.watchedTask.thisTasksProcessType == typeOfProcess) {
                        Debug.Log(facility.nextFacility.name);
                        Send(facility.nextFacility.name, $"start {facility.GetOutputStoreName()} {itemObjectName}");
                    }
                    else {
                        RegenerateItemSpec(ObjectNameToItem(itemObjectName));
                    }
                    break;

                case "accept":
                    Debug.Log("Thank you, " + message.Sender);
                    agentCounter = 0;
                    break;

                case "reject":
                    Debug.Log("No worries, " + message.Sender);
                    InformAgent(agentCounter++);
                    break;

/* ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ */
/*                               A previous facility has sent a task                              */
/* ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ */
                case "start":
                    // uses the relevant store from the previous facility
                    // peeks into the store and finds the item
                    // add "remove from store" to task list
                    // add "deliver to this facility" to task list
                    string storeObjectName = taskInfo[0];
                    string collectedObjectName = taskInfo[1];


                    Debug.Log($"Let's grab {collectedObjectName} from {storeObjectName}");
                    //Item nextItem = nextStore.GetItem(parameters[1]);
                    Item nextItem = GameObject.Find(collectedObjectName).GetComponent<Item>();

/* ------------------------------------- retrieve -> deliver ------------------------------------ */
                    nextItem.ResetTaskList();
                    nextItem.AmmendTaskList(nextItem.store.gameObject, "retrieve");
                    //nextItem.AmmendTaskList(nextItem.store.gameObject, "collect");
                    nextItem.AmmendTaskList(facility.gameObject, "deliver");
                    nextItem.ResetTaskIndex();
                    
/* ------------------------------ Inform an agent to take this task ----------------------------- */
                    Debug.Log("Let's find someone to...");
                    auctionedItemName = nextItem.gameObject.name;

                    InformAgent(agentCounter++);
                    break;

                // When the message from the BDI is not a BDI Sensing WorldAction, add it to the actionTasks
                default:
                    Send("program", $"{message.Sender} : {action}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            //Console.WriteLine(ex.ToString()); // for debugging
        }
    }

    public void InformAgent(int _agentCounter)
    {
        Send("Agent_" + _agentCounter, auctionedItemName);
    }

    private void RegenerateItemSpec(Item item) 
    {
        switch (item.GetProcessType())
        {
            case "complete":
                item.ResetTaskList();
                item.AmmendTaskList(item.gameObject, "process");
                item.ResetTaskIndex();
                break;
            
            case "deliver":
                item.ResetTaskList();
                item.AmmendTaskList(item.gameObject, "empty");
                item.AmmendTaskList(item.gameObject, "process");
                item.ResetTaskIndex();
                break;

            case "process":
                if (item.isContainer()) {
                    item.ResetTaskList();
                    item.AmmendTaskList(item.store.gameObject, "deliver");
                    item.AmmendTaskList(item.store.gameObject, "store");
                    item.ResetTaskIndex();
                }
                else {
                    item.ResetTaskList();
                    item.AmmendTaskList(item.container.gameObject, "retrieve");
                    item.AmmendTaskList(item.container.gameObject, "contain");
                    item.ResetTaskIndex();
                }
                break;
        }

    }
}