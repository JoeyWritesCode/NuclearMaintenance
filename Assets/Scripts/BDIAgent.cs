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
    private WorldAction act;

    private string currentPlan;
    private string goal;
    private string plan;

    private string name;

    private Dictionary<string, List<string>> beliefs;


    public override void Setup()
    {
        Debug.Log($"Starting {Name}");
        name = Name;

        beliefs = new Dictionary<string, List<string>>();
        act = new WorldAction("ask-agent", null, "INITIATE");

        Send(_abm.name, "Hello! decide");

    }

    void UpdatePercepts(List<Percept> percepts)
    {
        foreach (Percept percept in percepts) {
            beliefs[percept.identifier] = percept.value;
        }
    }

    void MakePlan()
    {
        switch (plan) {
            case "complete-task":
                break;

            case "ask-someone":
                // tell abm agent to determine it's nearest agents, and ask each of them for their best task
                break;
        }
    }


    void GenerateAct()
    {
        switch (goal) {
            case "minimize-time":
                break;

            case "find-task":
                // determine next best task that it's observed
                // if there are agents nearby, ask someone
                break;

            case "process-location":
                break;

            case "complete-task":
                break;
        }
    }

    void UpdateAct()
    {
        // This is where we encode all of the domain-specific rules that would lead to a change in action. 
        // As a small recap, that's when the observations of the world:
        /* ----------------------- - inform the agent that the action is completed ---------------------- */
        /* ----------------- - lead the agent to believe it should switch GoalTree paths ---------------- */
        /* ------------------- - inform the agent that the action can NOT be completed ------------------ */

        // if there is a new act suggested:
            
            // if act.state == FAIL or DROPPED, then goal was not acheived
        // remove act from actionList

        WorldAction nextAct = act;

        switch (act.GetIdentifier()) {
            case "ask-agent":
                if (beliefs.ContainsKey("nearest-agents") && beliefs["nearest-agents"].Count > 0) {
                    foreach (string agentName in beliefs["nearest-agents"]) {
                        Debug.Log($"hello {agentName}!");
                    }
                    act.SetState("PASSED");
                    nextAct = new WorldAction("decide", null, null);
                    break;
                }
                else {
                    Send(_abm.name, "find-agents");
                    break;
                }

            case "decide":
                Send(_abm.name, "Hello! decide");
                break;
        }

        if (nextAct != act) {
            switch (act.GetState()) {
                case "PASSED":
                    // if act.state == PASSED, then goal was achieved
                    Debug.Log($"Successfuly completed {act.GetIdentifier()}");
                    break;
                case "FAIL":
                    // if act.state == FAIL or DROPPED, then goal was not acheived
                    break;
                case "DROPPED":
                    break;
            }
            actionList.Remove(act);
            act = nextAct;
        }
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
                    // need to be able to send Percepts instead of strings
                    //UpdatePercepts(parameters);
                    UpdateAct();
                    break;

                case "nearest-agents":
                    beliefs["nearest-agents"] = parameters;
                    UpdateAct();
                    break;

                default:
                    UpdateAct();
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.Log((ex.ToString())); // for debugging
        }
    }
}

