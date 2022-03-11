using ActressMas;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

public class BDIAgent : Agent
{ 
    private Dictionary<string, Vector3> _beliefs;
    private HashSet<string> _desires;
    private string _intention; // only 1 intention active in this model
    private List<string> _plan;
    private bool _needToReplan;
    private int _size;

    private string _unity;
    private GameObject next_item;

    public BDIAgent(string unityName)
    {
        _beliefs = new Dictionary<string, Vector3>();
        _desires = new HashSet<string>();
        _intention = "";
        _plan = new List<string>();

        _unity = unityName;
    }

    public override void Setup()
    {
        Debug.Log($"Starting {Name}");

        _size = Environment.Memory["Size"];

        _beliefs["position"] = GameObject.Find(_unity).transform.position;
        _beliefs["destination"] = _beliefs["position"];
        // What else should we add?
        // track where items are... 

        // This orchestrates the environment to inform which agent to do which action
        // perhaps we should start with percepts...
        Send(_unity, "percepts");
    }
    
    // This message will be what informs this agent about the actions carried out.
    // Inevitably, we're going to need two agents.
    public override void Act(Message message)
    {
        try
        {
            Debug.Log($"\t{message.Format()}");
            message.Parse(out string action, out List<string> parameters);

            switch (action)
            {
                case "percepts":
                    BeliefRevision(parameters, _memory);
                    GenerateOptions();
                    FilterDesires();
                    if (_needToReplan) // if the environment is very dynamic, one can replan after each perception act
                        MakePlan();
                    ExecuteAction();
                    break;

                case "task-completed":
                    // for now, let's say if Vector.Zero then no current objective
                    _beliefs["destination"] = Vector.Zero;
                    _beliefs.Remove(next_item.name);
                    GenerateOptions();
                    FilterDesires();
                    if (_needToReplan) // if the environment is very dynamic, one can replan after each perception act
                        MakePlan();
                    ExecuteAction();
                    break;

                case "processing":
                    // could be cool to add something here
                    GenerateOptions();
                    FilterDesires();
                    if (_needToReplan) // if the environment is very dynamic, one can replan after each perception act
                        MakePlan();
                    ExecuteAction();
                    break;

                case "arrived":
                    // check if the item is at the location
                    Send(_unity, "look-around");
                    break;

                /* case "request-help":
                    // load in relevant information
                    _beliefs[parameters[0]] = parameters[1]; */

                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
            //Debug.Log((ex.ToString()); // for debugging
        }
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

    // What are the parameters for percepts? Information about the visual field?
    // In that case, this will have to be a vector for the agent's position, and then the unique
    // identifiers of the objects it sees.
    private async void BeliefRevision(List<string> parameters)
    {
        _beliefs["position"] = StringToVector3(parameters[0]);

        var visualFieldSize = parameters.Count() - 1;
        for (int i = 1; i < visualFieldSize; i++) {
            GameObject item = GameObject.Find(parameters[i]);
            _beliefs[item.name] = item.transform.position;
/* 
            Vector3 distance = item.processPosition - _beliefs["position"];
            if (distance < smallest_distance) {
                next_item = item;
            } */
        }
    }

    private void GenerateOptions()
    {
        if (_desires.Count == 0)
            _desires.Add("new-task");

        if (_intention == "complete-task" && _plan.Count > 0) // plan in progress
            return;


        if (_intention == "look-around") {
            _desires.Remove("look-around");
            if (_beliefs[next_item.name] == _beliefs["destination"]) {
                _desires.Add("complete-task");
            }
            else {
                _desires.Add("new-task");
            }
        }

        if (_beliefs["position"] == _beliefs["destination"])
        {
            // Reached the destination
            _desires.Remove("go-to");
            _desires.Add("look-around");
        }
    }

    private void FilterDesires()
    {
        string newIntention = "";

        if (_desires.Contains("new-task"))
            newIntention = "new-task";
        else if (_desires.Contains("complete-task"))
            newIntention = "complete-task";
        else if (_desires.Contains("go-to"))
            newIntention = "go-to";

        if (newIntention != _intention)
        {
            _intention = newIntention;
            _needToReplan = true;

            Debug.Log("Adopting new intention: {_intention}");
        }
    }

    private void MakePlan()
    {
        _plan.Clear();
        _needToReplan = false;

        // the only way a plan is interrupted is if the belief revision informs the agent that there is a better
        // sequence of events. 

        switch (_intention)
        {
            case "complete-task":
                _plan.Add("process");
                break;

            case "new-task":
                if (orderedTasks.Count == 0) {
                        orderedTasks = from entry in _beliefs orderby entry.Value - _beliefs["position"] ascending select entry;
                }
                else {
                    orderedTasks.Remove(orderedTasks.Last());
                }
                GameObject nextItem = GameObject.Find(orderedTasks.Last());
                _beliefs["destination"] = _beliefs[orderedTasks.Last()];
                _plan.Add($"go-to {_beliefs["destination"]}");
                _plan.Add($"pick-up {nextItem.name}");
                _plan.Add($"go-to {nextItem.processPosition}");
                _plan.Add($"drop {nextItem.name}");
                _plan.Add($"process {nextItem.name}");
                break;

            case "go-to":
                _plan.Add($"go-to {_beliefs["destination"]}");
                break;

            default:
                break;
        }
    }

    private void ExecuteAction()
    {
        if (_plan.Count == 0) // plan finished
            _intention = "";

        string action = _plan[0];
        _plan.RemoveAt(0);

        Send(_unity, action);
    }
}

