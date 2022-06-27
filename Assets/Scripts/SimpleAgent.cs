using ActressMas;

using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

public class SimpleAgent : Agent
{

    public SimpleWorker worker;

    public string name;

    private string lastTask;

    public override void Setup()
    {
        Send("program", $"Starting {name}");
    }
 
    // This is only used for when receiving messages from the agent's BDI model. 
    // Could be target percept requests, or a new itemName to add to the actionList
    public override void Act(Message message)
    {
        try
        {
            Console.WriteLine($"\t{message.Format()}");
            message.Parse(out string itemName, out string parameters);

            switch (itemName)
            {
                // When the message from the BDI is not a BDI Sensing WorldAction, add it to the actionTasks
                default:    
                    if (worker.nextAction == "decide") {
                        worker.assignItem(itemName);
                        Send(message.Sender, $"accept {name}");
                    }
                    else {
                        Send(message.Sender, $"reject {name}");
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            //Console.WriteLine(ex.ToString()); // for debugging
        }

        PerformAction();
    }

    public override void ActDefault()
    {
        PerformAction();
    }

    void PerformAction() {
        worker.Act();
        if (worker.nextAction == "record task") {
            Send(worker.currentFacility.name, $"finished {worker.getTaskDetails()}");
            worker.resetPlanTree();
        }
    }
}