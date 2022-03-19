using ActressMas;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

public class UnityAgent : Agent
{
    /* private int _turns = 0; */
    private int _size;

    private GameObject _self;
    private NavMeshAgent nmAgent;

    private HashSet<string> memory;

    public GameObject Hands;

    public Vector3 destination;

    public string _bdi;

    private float distanceThreshold = 0.1f;
    private float visualFieldDistance = 10f;


    public override void Setup()
    {
        Console.WriteLine($"Starting {Name}");

        _self = GameObject.Find(Name);
        memory = new HashSet<string>();

        _size = Environment.Memory["Size"];
        nmAgent = _self.GetComponent<NavMeshAgent>();

        Send(_bdi, $"start {_self.transform.position} {string.Join(" ", GetObjectNamesInRange(_self.transform.position, visualFieldDistance, "Item"))}");
    }

    void Update()
    {
        Debug.Log(destination);
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

    public override void Act(Message message)
    {
        try
        {
            Console.WriteLine($"\t{message.Format()}");
            message.Parse(out string action, out string parameters);

            switch (action)
            {
                case "go-to":
                    destination = StringToVector3(parameters);
                    nmAgent.SetDestination(destination);
                    break;

                case "pick-up":
                    pickUp(parameters);
                    break;

                case "drop":
                    drop(parameters);
                    break;

                case "process":
                    executeTask(parameters, message.Sender);
                    break;

                case "look-around":
                    if (nmAgent.transform.position == destination) {
                        Send(message.Sender, "arrived");
                    }
                    else;
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
        item.transform.position = _self.transform.Find("Hands").position;
        item.transform.parent = _self.transform.Find("Hands");
    }

    private void drop(string itemName) {
        GameObject item = GameObject.Find(itemName);
        item.transform.parent = null;
        item.GetComponent<Rigidbody>().useGravity = true;
    }

    private void executeTask(string itemName, string sender) {
        // check how long the item requires (will fluctuate later with collaboration)
        Item item = GameObject.Find(itemName).GetComponent<Item>();
        if (item.isProcessed()) {
            item.complete();
            Send(sender, $"task-completed {itemName}");
        }
        else {
            item.decrementProcessTime();
            Send(sender, $"processing {itemName} {item.GetRemainingTime()}");
        }
    }

    private List<string> GetObjectNamesInRange(Vector3 position, float radius, string tag)
    {
        List<string> seenObjects = new List<string>();

        Collider[] hitColliders = Physics.OverlapSphere(position, radius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.tag == tag) {
                seenObjects.Add(hitCollider.gameObject.name);
            }
        }
        return seenObjects;
    }

    private void UpdateVisualField(string sender)
    {   
        Send(sender, $"travelling {_self.transform.position} {string.Join(" ", GetObjectNamesInRange(_self.transform.position, visualFieldDistance, "Item"))}");
    }  
}


