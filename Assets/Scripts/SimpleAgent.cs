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
        Debug.Log($"Starting {name}");
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
                case "Item disassembled":
                    break;
                
                // When the message from the BDI is not a BDI Sensing WorldAction, add it to the actionTasks
                default:
                    DetermineMessage();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            //Console.WriteLine(ex.ToString()); // for debugging
        }
    }

    void DetermineMessage()
    {
        switch (worker.taskToStart) {
            case null:
                break;
            
            case "disassembly":
                Send("Disassembly", "begin");
                worker.taskToStart = null;
                break;

            default:
                break;
        }
    }
}