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
    private Dictionary<string, Vector3> _beliefs;
    private HashSet<string> _desires;
    private string _intention; // only 1 intention active in this model
    private List<string> _plan;
    private bool _needToReplan;
    private int _size;

    private float travel_speed;
    private float process_speed;

    private string _unity;
    private Item next_item;

    private List<string> unfinishedTasks;

    public BDIAgent(string unityName)
    {
        _beliefs = new Dictionary<string, Vector3>();
        _desires = new HashSet<string>();
        _intention = "";
        _plan = new List<string>();

        _unity = unityName;

        travel_speed = 5.0f;
        process_speed = 5.0f;

        next_item = GameObject.Find("Item").GetComponent<Item>();
        unfinishedTasks = new List<string>();
    }

    public override void Setup()
    {
        Debug.Log($"Starting {Name}");

        _size = Environment.Memory["Size"];

        _beliefs["position"] = GameObject.Find(_unity).transform.position;
        _beliefs["destination"] = new Vector3(-1.89f, 2.47f, -2.43f);

        // This orchestrates the environment to inform which agent to do which action
        // perhaps we should start with percepts...
        Send(_unity, $"go-to {_beliefs["destination"]}");
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

    public float TravelEffort(string key) {
        Debug.Log($"found {key}");
        if (key != "position" && key != "destination") {
            Item item = GameObject.Find(key).GetComponent<Item>();
            float distance = (item.GetPosition() - _beliefs["position"]).magnitude;
            distance += (item.GetProcessPosition() - item.GetPosition()).magnitude;

            float time = distance / travel_speed + item.GetRemainingTime() / process_speed;
            return time;
        }
        return float.MaxValue;
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
                    BeliefRevision(parameters);
                    GenerateOptions();
                    FilterDesires();
                    if (_needToReplan) // if the environment is very dynamic, one can replan after each perception act
                        MakePlan();
                    ExecuteAction();
                    break;

                case "task-completed":
                    // for now, let's say if Vector.Zero then no current objective
                    _beliefs["destination"] = Vector3.zero;
                    _beliefs.Remove(next_item.GetName());
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
            Debug.Log((ex.ToString())); // for debugging
        }
    }

    // What are the parameters for percepts? Information about the visual field?
    // In that case, this will have to be a vector for the agent's position, and then the unique
    // identifiers of the objects it sees.
    private async void BeliefRevision(List<string> parameters)
    {
        Debug.Log(parameters.Count);
        
        _beliefs["position"] = StringToVector3(parameters[0] + parameters[1] + parameters[2]);

        for (int i = 0; i < parameters.Count - 3; i++) {
            GameObject item = GameObject.Find(parameters[i + 3]);
            _beliefs[item.name] = item.transform.position;
            Debug.Log($"new belief! {item.name} is at {_beliefs[item.name]}");
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
            if (_beliefs[next_item.GetName()] == _beliefs["destination"]) {
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

        if (_desires.Contains("complete-task"))
            newIntention = "complete-task";
        else if (_desires.Contains("go-to"))
            newIntention = "go-to";
        else if (_desires.Contains("look-around"))
            newIntention = "look-around";

        if (newIntention != _intention)
        {
            _intention = newIntention;
            _needToReplan = true;

            Debug.Log($"Adopting new intention: {_intention}");
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

            case "go-to":
                _plan.Add($"go-to {_beliefs["destination"]}");
                break;

            case "look-around":
                if (unfinishedTasks.Count == 0) {
                    foreach (KeyValuePair<string, Vector3> seenItem in _beliefs.OrderBy(pair => TravelEffort(pair.Key)))  
                    {  
                        unfinishedTasks.Add(seenItem.Key);
                    } 
                }
                Debug.Log("loop done!");
                next_item = GameObject.Find(unfinishedTasks[unfinishedTasks.Count - 1]).GetComponent<Item>();
                unfinishedTasks.Remove(unfinishedTasks[unfinishedTasks.Count - 1]);
                _beliefs["destination"] = next_item.GetPosition();
                Debug.Log("next item found!");

                _plan.Add($"go-to {_beliefs["destination"]}");
                _plan.Add($"pick-up {next_item.GetName()}");
                _plan.Add($"go-to {next_item.GetProcessPosition()}");
                _plan.Add($"drop {next_item.GetName()}");
                _plan.Add($"process {next_item.GetName()}");
                break;

            default:
                break;
        }
    }

    private void ExecuteAction()
    {
        if (_plan.Count == 0) // plan finished
            _intention = "";

        Debug.Log("Now we execute!");

        string action = _plan[0];
        _plan.RemoveAt(0);

        Send(_unity, action);
    }
}

