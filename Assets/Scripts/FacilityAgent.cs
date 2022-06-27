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
                // reply to begin next phase
                case "start":
                    // uses the relevant store from the previous facility
                    // peeks into the store and finds the item
                    // add "remove from store" to task list
                    // add "deliver to this facility" to task list
                    /* Debug.Log($"Let's grab {parameters[1]} from {parameters[0]}");
                    Store nextStore = GameObject.Find(parameters[0]).GetComponent<Store>();
                    //Item nextItem = nextStore.GetItem(parameters[1]);
                    Item nextItem = nextStore.Pop();
                    Debug.Log($"Next item is {nextItem.gameObject.name}");
                    nextItem.AmmendTaskList(nextStore.gameObject, "remove");
                    nextItem.AmmendTaskList(nextStore.gameObject, "collect");
                    nextItem.AmmendTaskList(facility.gameObject, "deliver");
                    Debug.Log("Let's find someone to...");
                    InformAgent(agentCounter++); */
                    break;

                case "accept":
                    Debug.Log("Thank you, " + message.Sender);
                    agentCounter = 0;
                    break;

                case "reject":
                    Debug.Log("No worries, " + message.Sender);
                    InformAgent(agentCounter++);
                    break;
                
                case "finished":
                    string itemObjectName = taskInfo[0];
                    string itemName = taskInfo[1];
                    string typeOfProcess = taskInfo[2];

                    Debug.Log($"{message.Sender} has just {typeOfProcess}'d {itemObjectName}");
                    if (facility.watchedTask.thisTasksObject.name == itemName && facility.watchedTask.thisTasksProcessType == typeOfProcess) {
                        Debug.Log(facility.nextFacility.name);
                        //Send(facility.nextFacility.name, $"start {facility.GetOutputStoreName()} {taskInfo[0]}");
                        Send(facility.nextFacility.name, $"accept");
                    }
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
}