using ActressMas;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

public class UnityAgent : Agent
{
    private Vector3 position;
    /* private int _turns = 0; */
    private int _size;

    private GameObject _self;

    private HashSet<string> memory;

    public UnityAgent()
    {
        _self = GameObject.Find(Name);
        position = _self.transform.position;
        memory = new HashSet<string>();
    }

    public override void Setup()
    {
        Console.WriteLine($"Starting {Name}");

        _size = Environment.Memory["Size"];

/*         States = new TerrainState[_size];
        States[0] = TerrainState.Water;

        for (int i = 1; i < _size; i++)
            States[i] = TerrainState.Normal;

        position = 0; */
    }

    public override void Act(Message message)
    {
        try
        {
            Console.WriteLine($"\t{message.Format()}");
            message.Parse(out string action, out string parameters);

            /* for (int i = 0; i < States.Length; i++)
                if (States[i] == TerrainState.GettingWater)
                    States[i] = TerrainState.Water; */

            switch (action)
            {
                case "go-to":
                    NavMeshAgent nmAgent = _self.GetComponent<NavMeshAgent>();
                    nmAgent.SetDestination(parameters[0]);
                    UpdateVisualField(message.Sender);
                    break;

/*                 case "enlist":
                    var worker_counter = 0;
                    while (worker_counter < parameters[1]) {
                        foreach (string name in GetObjectsInRange(position, 10.0f, "Agent")) {
                            string manager = GameObject.Find(name)._manager;
                            Send(manager, "request-help");
                            worker_counter++;
                        }
                        worker_counter = parameters[1];
                    }
                    UpdateVisualField(message.Sender);
                    break; */

                case "pick-up":
                    pickUp(parameters[0]);
                    break;

                case "drop":
                    drop(parameters[0]);
                    break;

                case "process":
                    executeTask(parameters[0]);
                    break;

                case "look-around":
                    UpdateVisualField(message.Sender);
                    break;

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

    private void pickUp(string itemName) {
        GameObject item = GameObject.Find(itemName);
        item.GetComponent<Rigidbody>().useGravity = false;
        item.transform.position = _self.Hands.position;
        item.transform.parent = _self.Hands.transform;
    }

    private void drop(string itemName) {
        GameObject item = GameObject.Find(itemName);
        item.transform.parent = null;
        item.GetComponent<Rigidbody>().useGravity = true;
    }

    private void executeTask(string itemName, string sender) {
        // check how long the item requires (will fluctuate later with collaboration)
        GameObject item = GameObject.Find(itemName);
        if (item.isProcessed()) {
            Destroy(item);
            Send(sender, $"task-completed {itemName}");
        }
        else {
            item.decrementProcessTime();
            Send(sender, $"processing {itemName} {item.GetRemainingTime()}");
        }
    }

    private HashSet<string> GetObjectsInRange(Vector3 position, float radius, string tag)
    {
        List<string> seenObjects = new List<string>();

        Collider[] hitColliders = Physics.OverlapSphere(position, radius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.GameObject.tag == tag && !memory.Contains(hitCollider.GameObject.name)) {
                seenObjects.Add(hitCollider.GameObject.name);
            }
        }
        
        return seenObjects;
    }

    private void UpdateVisualField(string sender)
    {
        Send(sender, $"percepts {position} {GetObjectsInRange(position, 10.0f, "Item")}");
    }
}


