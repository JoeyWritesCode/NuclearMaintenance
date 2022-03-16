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

    private List<string> availableTasks;

    public Vector3 destination;
    public Vector3 position;
    public string action;

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
        availableTasks = new List<string>();
    }

    public override void Setup()
    {
        Debug.Log($"Starting {Name}");

        _size = Environment.Memory["Size"];

        position = GameObject.Find(_unity).transform.position;
        destination = new Vector3(-1.89f, 2.47f, -2.43f);

        // This orchestrates the environment to inform which agent to do which action
        // perhaps we should start with percepts...
        Send(_unity, "look-around");
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
        // determine distances to each object
        // determine distances from object to process location 
        // determine time to process 
        // sum up
        // pick best
        Item item = GameObject.Find(key).GetComponent<Item>();
        float time_to_item = (item.GetPosition() - position).magnitude / travel_speed;
        float time_to_carry = (item.GetProcessPosition() - item.GetPosition()).magnitude / travel_speed;
        float time_to_process = item.GetRemainingTime() / process_speed;
        return time_to_item + time_to_carry + time_to_process - item.GetTimeSpentWaiting();
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
                case "travelling":
                    BeliefRevision(parameters);
                    Send(message.Sender, "look-around");
                    break;

                case "task-completed":
                    _beliefs.Remove(next_item.GetName());
                    next_item = null;
                    GenerateOptions();
                    FilterDesires();
                    if (_needToReplan) // if the environment is very dynamic, one can replan after each perception act
                        MakePlan();
                    ExecuteAction();
                    break;

                case "processing":
                    // could be cool to add something here
                    Send(message.Sender, $"process {next_item.GetName()}");
                    break;

                case "arrived":
                    // check if the item is at the location
                    // if it is, complete the task
                    // otherwise, generate options
                    GenerateOptions();
                    FilterDesires();
                    if (_needToReplan) // if the environment is very dynamic, one can replan after each perception act
                        MakePlan();
                    ExecuteAction();
                    break;

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
        position = StringToVector3(parameters[0] + parameters[1] + parameters[2]);

        for (int i = 0; i < parameters.Count - 3; i++) {
            GameObject item = GameObject.Find(parameters[i + 3]);
            _beliefs[item.name] = item.transform.position;
        }
    }

    private void GenerateOptions()
    {
        List<string> availableTasks = new List<string>(_beliefs.Keys);
        availableTasks.OrderBy(name => TravelEffort(name));

        if (availableTasks.First() != next_item.GetName()) {
            next_item = GameObject.Find(availableTasks.First()).GetComponent<Item>();
            availableTasks.RemoveAt(0);
            _desires.Add("go-to");
            if (next_item.inProcessPosition())
                destination = next_item.GetProcessPosition();
            else
                destination = next_item.GetPosition();
        }
        else {
            _desires.Remove("go-to");
            _desires.Add("complete-task");
        }
    }

    private void FilterDesires()
    {
        string newIntention = "";

        if (_desires.Contains("go-to"))
            newIntention = "go-to";
        else if (_desires.Contains("complete-task"))
            newIntention = "complete-task";

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
                _plan.Add($"pick-up {next_item.GetName()}");
                _plan.Add($"go-to {next_item.GetProcessPosition()}");
                _plan.Add($"drop {next_item.GetName()}");
                _plan.Add($"process {next_item.GetName()}");
                break;

            case "go-to":
                _plan.Add($"go-to {destination}");
                break;

            default:
                break;
        }
    }

    private void ExecuteAction()
    {
        if (_plan.Count == 0) { // plan finished
            _intention = "";
            Send(_unity, "look-around");
        }
        else {
            action = _plan[0];
            _plan.RemoveAt(0);
            Send(_unity, action);
        }
    }
}

