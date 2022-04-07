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

    public GameObject facility;

    public override void Setup()
    {
        Debug.Log($"{facility.gameObject.name} is ready to assign work!");
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
                    // reply to begin next phase
                case "begin":
                    break;
                
                // When the message from the BDI is not a BDI Sensing WorldAction, add it to the actionTasks
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            //Console.WriteLine(ex.ToString()); // for debugging
        }
    }
}