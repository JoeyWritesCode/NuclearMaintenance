using ActressMas;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.AI;


public class BDIAgent : Agent
{ 
    public UnityAgent _abm;
    public List<WorldAction> actionList;

    private string currentPlan;
    private string goal;


    public override void Setup()
    {
        Debug.Log($"Starting {Name}");

        // add act to actionList
        // send message to UnityAgent

    }

    void UpdateAction()
    {
        // if there is a new act suggested:
            // if act.state == PASSED, then goal was achieved
            // if act.state == FAIL or DROPPED, then goal was not acheived
        // remove act from actionList
        Debug.Log("pass");
    }

    public static Vector3 StringToVector3(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith ("(") && sVector.EndsWith (")")) {
            sVector = sVector.Substring(1, sVector.Length-2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
    }
    
    // This message will be what informs this agent about the actions carried out.
    // Inevitably, we're going to need two agents.
    public override void Act(Message message)
    {
        try
        {
            Debug.Log($"\t{message.Format()}");
            message.Parse(out string action, out List<string> parameters);

            // Can take 'percepts' or updates from the world, and sends Actions to the Worker object
            switch (action)
            {
                case "percepts":
                    Debug.Log("let's have a looksie");
                    UpdateAction();
                    break;

                case "nearest agents":
                    break;

                case "objects":
                    break;

                default:
                    // By default, we determine whether a change in plan is needed
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.Log((ex.ToString())); // for debugging
        }
    }
}

