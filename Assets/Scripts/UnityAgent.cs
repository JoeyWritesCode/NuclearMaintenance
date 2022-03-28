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

    private List<string> actions;

    //public BDIAgent _bdi;


    public override void Setup()
    {
        Console.WriteLine($"Starting {Name}");
        List<WorldAction> actionList = new List<WorldAction>();

        actions = new List<string>(){"decide", "collect", "deliver"};
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
                // When the message from the BDI is not a BDI Sensing WorldAction, add it to the actionTasks
                default:
                    actionList.Add(new WorldAction(actions.First(), null, "INITIATED"));
                    actions.Remove(actions.First());
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            //Console.WriteLine(ex.ToString()); // for debugging
        }

        Debug.Log("I'm alive!");

        actionList.Add(new WorldAction(actions.First(), null, "INITIATED"));
        actions.Remove(actions.First());

        foreach (WorldAction action in actionList) {
            switch (action.GetState()) {
                case "INITIATE":
                    // read the parameters. This will set the location and/or the next item.
                    worker.Act(action);
                    action.SetState("RUNNING");
                    break;
                case "DROPPED":
                    actionList.Remove(action);
                    break;
                case "RUNNING":
                    //worker.nextAction = action;
                    Debug.Log(action);
                    break;
                default:
                    break;
            }
        }
    }
}


