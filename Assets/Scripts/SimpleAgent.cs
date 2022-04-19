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
                /* case "StoreWarhead":
                    worker.transitionStore = "StoreWarhead";
                    worker.transitionDestination = "Disassembly";
                    worker.SetDestination(GameObject.Find("StoreWarhead").transform.position);
                    worker.SetNextAction("retrieve");
                    break;

                case "RecycleA":
                    worker.SetDestination(GameObject.Find("StoreContainersMaterialA").transform.position);
                    worker.transitionStore = "StoreContainersMaterialA";
                    
                    worker.transitionDestination = parameters;
                    worker.objectToContain = parameters;

                    worker.SetNextAction("retrieve");
                    break; */
                
                // When the message from the BDI is not a BDI Sensing WorldAction, add it to the actionTasks
                default:    
                    if (worker.isBusy()) {
                        Send(message.Sender, $"reject {name}");
                    }
                    else {
                        worker.assignItem(itemName);
                        Send(message.Sender, $"accept {name}");
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            //Console.WriteLine(ex.ToString()); // for debugging
        }

        worker.Act();
    }

    public override void ActDefault()
    {
        worker.Act();
    }
}