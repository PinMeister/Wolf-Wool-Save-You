﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroupBehaviour : MonoBehaviour
{
    [Header("Formation Data")]
    [SerializeField]
    public List<Transform> slots;
    [SerializeField]
    public List<GameObject> SheepGroup;
    [SerializeField]
    GameObject PatrolPlot;

    [Header("Steering behaviour")]
    [SerializeField]
    float max_velocity = 1.8f;
    [SerializeField]
    float max_acceleration = 100f;
    [SerializeField]
    float slowdown_radius = 0.5f;
    [SerializeField]
    float arrival_radius = 0.2f;
    [SerializeField]
    float slowdown_velocity = 0.5f;
    [SerializeField]
    float formation_slowdown_range = 5f;
    [HideInInspector]
    public Transform anchor;

    private List<Transform> slots_walkable = new List<Transform>();

    private bool isInitialized = false;
    // the forward boolean is to know whether we are going back or forth on the patrol route (forward means forth, else back)
    private bool forward = true;
    private List<Transform> patrol_nodes = new List<Transform>();
    private int target_node_index = 0;
    private Vector2 current_velocity = Vector2.zero;
    private float formation_velocity;

    void Start()
    {
        formation_velocity = max_velocity;
        // make sure your slots and npcs match
        if (SheepGroup.Count != slots.Count)
        {
            throw new System.Exception("The amount of sheep in a group must equal the amount of slots in that group!");
        }

        // make sure slots are connected to anchor, and that each sheep object has a reference to its slot
        anchor = transform;
        for(int i = 0; i < slots.Count; i++)
        {
            slots[i].parent = anchor;
            Transform walkable = new GameObject().transform;
            walkable.position = slots[i].position;
            slots_walkable.Add(walkable);

            SheepBehavior sb = SheepGroup[i].GetComponent<SheepBehavior>();
            sb.SetSlot(slots_walkable[i]);
            sb.pathingType = SheepBehavior.SheepPathingType.Patrolling;
        }

        // populate the patrol nodes list
        InitializePatrolNodes();
        InvokeRepeating("CheckFormationIntegrity", 3, 5);
    }

    void Update()
    {
        if (isInitialized)
        {
            PlaceWalkableSlots();
            Arrive();
        }   
    }
    
    void Arrive()
    {
        // this is steering with acceleration

        //Debug.Log(patrol_nodes);

         Vector2 direction = patrol_nodes[target_node_index].position - transform.position;
        // Vector2 acceleration = max_acceleration * direction.normalized;
        // Vector2 velocity = current_velocity + acceleration * Time.deltaTime;

        // This is exactly the same as the above commented out code, except compacted into 1 line to save some memory
        Vector2 velocity = current_velocity + (max_acceleration * direction.normalized) * Time.deltaTime;

        if (velocity.magnitude > formation_velocity)
        {
            velocity = velocity.normalized * formation_velocity;
        }

        float distance = direction.magnitude;
        if (distance < slowdown_radius && distance > arrival_radius)
        {
            velocity = velocity.normalized * (distance / slowdown_radius);
        }

        // when you reach a node, go to next node. If you're at the edge of a patrol path, change direction
        if(distance <= arrival_radius)
        {
            if (target_node_index == patrol_nodes.Count - 1 && forward == true)
            {
                forward = false;
            }
            else if(target_node_index == 0 && forward == false)
            {
                forward = true;
            }

            if (forward)
            {
                target_node_index++;
            }
            else
            {
                target_node_index--;
            }
        }

        transform.position = (Vector2)transform.position + velocity * Time.deltaTime;
    }

    void PlaceWalkableSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            // if slot is walkable, sheep can go to it
            Vector2Int slotPos = (Vector2Int)GridManager.Instance.walkableTilemap.WorldToCell(slots[i].position);
            if (GridManager.Instance.isWalkableTile(slotPos))
            {
                // Debug.Log("Assigning walkable slot");
                slots_walkable[i] = slots[i];
            }
            // if slot is not walkable, find a walkable spot 0.2f from the obstacle
            else
            {
                var rayDirection = slots[i].position - transform.position;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, rayDirection.magnitude, LayerMask.NameToLayer("Obstacle"));
                if (hit)
                {
                    // if the slot is unwalkable, find nearest spot before that point from the anchor
                    float distanceFromAnchor = (hit.point - (Vector2)transform.position).magnitude;
                    distanceFromAnchor -= 0.2f;
                    slots_walkable[i].position = transform.position + rayDirection.normalized * distanceFromAnchor;
                }
            }
            SheepGroup[i].GetComponent<SheepBehavior>().SetSlot(slots_walkable[i]);
        }
        
    }

    void CheckFormationIntegrity()
    {
        bool slowdown = false;
        // Debug.Log("Checking if formation is A-ok");
        // if any sheep is further than some certain range, slow down the formation so sheep can catch up
        for (int i = 0; i < slots.Count; i++)
        {
            if((SheepGroup[i].transform.position - slots[i].position).magnitude > formation_slowdown_range)
            {
                formation_velocity = slowdown_velocity;
                SheepGroup[i].GetComponent<SheepBehavior>().travelPath.Clear();
                slowdown = true;
            }
        }
        // otherwise make sure it stays at its max velocity
        if (!slowdown)
        {
            formation_velocity = max_velocity;
        }
    }


    // populates the patrol nodes list
    void InitializePatrolNodes()
    {
        if (PatrolPlot == null)
        {
            throw new System.Exception("No patrol plot exists for this group! " + gameObject.GetInstanceID());
        }
        else
        {
            foreach(var sheep in SheepGroup)
            {
                sheep.GetComponent<SheepBehavior>().pathingType = SheepBehavior.SheepPathingType.Patrolling;
            }
        }

        foreach (Transform child in PatrolPlot.transform)
        {
            if (child.tag == "PatrolNode")
            {
                patrol_nodes.Add(child);
            }
        }
        target_node_index = 0;

        isInitialized = true;
    }
}
