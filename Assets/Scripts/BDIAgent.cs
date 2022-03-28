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
    private string newIntention;
    private List<string> _plan;
    private bool _needToReplan;
    private int _size;

    private float travel_speed;
    private float process_speed;

    public string _abm;
    private Item next_item;

    private List<string> availableTasks;

    public Vector3 destination;
    public Vector3 position;
    public string action;

    private float distance_threshold = 1.0f;

    public BDIAgent(string unityName)
    {
        _beliefs = new Dictionary<string, Vector3>();
        _desires = new HashSet<string>();
        _intention = "";
        _plan = new List<string>();

        _abm = unityName;

        travel_speed = 5.0f;
        process_speed = 5.0f;

        availableTasks = new List<string>();
    }

    public override void Setup()
    {
        Debug.Log($"Starting {Name}");

        _size = Environment.Memory["Size"];

        position = GameObject.Find(_abm).transform.position;
        destination = Vector3.zero;

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
                case "start":
                    Debug.Log("let's have a looksie");
                    BeliefRevision(parameters);
                    GenerateOptions();
                    FilterDesires();
                    if (_needToReplan) // if the environment is very dynamic, one can replan after each perception act
                        MakePlan();
                    ExecuteAction();
                    break;

                case "travelling":
                    Send(message.Sender, "waiting");
                    break;

                case "task-completed":
                    _beliefs.Remove(next_item.GetName());
                    next_item = null;
                    BeliefRevision(parameters);
                    GenerateOptions();
                    FilterDesires();
                    if (_needToReplan) // if the environment is very dynamic, one can replan after each perception act
                        MakePlan();
                    ExecuteAction();
                    break;

                case "processing":
                    // could be cool to add something here
                    _plan.Add($"process {next_item.GetName()}");
                    ExecuteAction();
                    break;

                case "arrived":
                    // check if the item is at the location
                    // if it is, complete the task
                    // otherwise, generate options
                    BeliefRevision(parameters);
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

        List<string> seenObjects = parameters.GetRange(3, parameters.Count - 3);
        if (seenObjects.Count > 0) {
            foreach (string name in seenObjects) {
                //Debug.Log(name);
                GameObject item = GameObject.Find(name);
                _beliefs[item.name] = item.transform.position;
            }
        }
    }

    private void GenerateOptions()
    {
        //if (availableTasks.First() != next_item.GetName()) {
        if (_intention == "go-to" && (destination - position).magnitude > distance_threshold)
            return;

        if (next_item == null) {
            //_desires.Remove("complete-task");
            //_desires.Remove("process");

            List<string> availableTasks = new List<string>(_beliefs.Keys);
            availableTasks.OrderBy(name => TravelEffort(name));

            try {
                Debug.Log($"let's go get that {availableTasks.Last()}");
                next_item = GameObject.Find(availableTasks.Last()).GetComponent<Item>();

                destination = next_item.GetPosition();
            }
            catch (Exception ex) {
                destination = GetRandomPoint(position, 5.0f);
            }
            newIntention = "go-to";

        }
        else {
            if (next_item.inProcessPosition())
                newIntention = "process";
            else {
                //_desires.Remove("go-to");
                Debug.Log("Let's get this to it's process position!");
                //_desires.Add("complete-task");
                newIntention = "complete-task";
            }
        }
    }

    private void FilterDesires()
    {
        // This function determines whether desire(X, D) and belief(X, Z) hold, if so return intention(X, Y)
        if (newIntention != _intention)
        {
            _intention = newIntention;
            _needToReplan = true;

            Debug.Log($"Adopting new intention: {_intention}");
        }
        else {
            _plan.Add(_plan.First());
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
                break;

            case "go-to":
                _plan.Add($"go-to {destination}");
                break;

            case "process":
                _plan.Add($"drop {next_item.GetName()}");
                _plan.Add($"process {next_item.GetName()}");
                break;

            default:
                break;
        }
    }

    private void ExecuteAction()
    {
        if (_plan.Count == 1) { // plan finished
            Debug.Log("plan is finished!");
            _plan.Add(_plan.First());
           /*  if (_intention == "go-to")
                Send(_abm, "waiting");
            else
                Send(_abm, "start"); */
        }
        action = _plan[0];
        _plan.RemoveAt(0);
        Debug.Log($"Hello! I will now {action}, because I want to {_intention}");
        Send(_abm, action);
    }

    public Vector3 GetRandomPoint(Vector3 center, float maxDistance) {
        // Get Random Point inside Sphere which position is center, radius is maxDistance
        Vector3 randomPos = UnityEngine.Random.insideUnitSphere * maxDistance + center;

        NavMeshHit hit; // NavMesh Sampling Info Container

        // from randomPos find a nearest point on NavMesh surface in range of maxDistance
        NavMesh.SamplePosition(randomPos, out hit, maxDistance, NavMesh.AllAreas);

        return hit.position;
    }
}

