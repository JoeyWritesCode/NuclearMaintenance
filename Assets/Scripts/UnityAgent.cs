using ActressMas;

using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

public class UnityAgent : Agent
{
    /* private int _turns = 0; */

    public Worker worker;
    public List<WorldAction> actionList;

    public string name;

    //public BDIAgent _bdi;


    public override void Setup()
    {
        Debug.Log($"Starting {name}");
        actionList = new List<WorldAction>();
        //actions = new List<string>(){"decide", "collect", "deliver"};
    }

    // This is only used for when receiving messages from the agent's BDI model. 
    // Could be target percept requests, or a new action to add to the actionList
    public override void Act(Message message)
    {
        try
        {
            Console.WriteLine($"\t{message.Format()}");
            message.Parse(out string action, out string parameters);

            switch (action)
            {
                case "find-agents":
                    //actionList.Add(new WorldAction(action, parameters, "INITIATE"));
                    Send(message.Sender, "nearest-agents " + String.Join(" ", worker.FindNearestAgents()));
                    EvaluateActionList();
                    break;

                case "Hello!":
                    actionList.Add(new WorldAction(parameters, null, "INITIATE"));
                    Send(message.Sender, "Hello to you too!");
                    EvaluateActionList();
                    break;
                
                // When the message from the BDI is not a BDI Sensing WorldAction, add it to the actionTasks
                default:
                    actionList.Add(new WorldAction(action, null, "INITIATE"));
                    EvaluateActionList();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            //Console.WriteLine(ex.ToString()); // for debugging
        }
    }

    void EvaluateActionList()
    {
        foreach (WorldAction action in actionList) {
            switch (action.GetState()) {
                case "INITIATE":
                    // read the parameters. This will set the location and/or the next item.
                    Debug.Log($"I have {action.GetIdentifier()} to get to!");
                    worker.Act(action);
                    action.SetState("RUNNING");
                    break;
                case "DROPPED":
                    actionList.Remove(action);
                    break;
                case "RUNNING":
                    //worker.nextAction = action;
                    Debug.Log($"Continuing with {action.GetIdentifier()}");
                    worker.Act(action);
                    break;
                default:
                    break;
            }
        }
    }
}


