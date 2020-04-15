﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SheepBehavior : MonoBehaviour
{
    // In order to access the Sheep's class
    private Sheep sheep;
    public enum SheepPathingType
    {
        Stationary,
        Patrolling,
        ToSweater,
        ToPlayer,
        Fleeing,
        Returning
    }

    public enum SheepDirection
    {
        North,
        East,
        South,
        West
    }
    // public data members
    [Header("Steering")]
    public float movementSpeed = 2.0f;

    [Header("Pathing")]
    public bool pathFound;
    public SheepPathingType pathingType;
    public SheepPathingType oldPathingType;
    public List<Vector2> travelPath;

    [Header("Behaviours")]
    public bool movingToNextTile;
    public bool fleeing;
    public float howlTimer;
    public Vector2Int sweaterPos;

    // private data members
    Transform slot;
    Vector2Int pos;
    Vector2 nextPos;
    Vector2 oldPos;
    Vector3 oldScale;
    Quaternion oldRot;

    Wolf wolf;

    // Patrol AI used for single sheep patrolling
    private int patrolSpotsIndex;
    public PatrolSpot[] aiPatrolSpots;

    // Start is called before the first frame update
    void Start()
    {
        pathFound = false;
        movingToNextTile = false;
        travelPath = new List<Vector2>();
        pathingType = SheepPathingType.Stationary; // stationary, unless they have patrol spots
        sheep = GetComponent<Sheep>();
        pos = Vector2Int.RoundToInt(new Vector2(transform.position.x, transform.position.y));

        wolf = GameObject.Find("Wolf").GetComponent<Wolf>();
        howlTimer = 0;

        // AI has a patrol spot
        if (aiPatrolSpots.Length > 0)
        {
            pathingType = SheepPathingType.Patrolling;
            Vector3 min = aiPatrolSpots[patrolSpotsIndex].GetPosition;

            for (int i = 0; i < aiPatrolSpots.Length; i++)
            {
                if (Vector3.Distance(transform.position, min) > Vector3.Distance(transform.position, aiPatrolSpots[i].GetPosition))
                {
                    min = aiPatrolSpots[i].GetPosition;
                    patrolSpotsIndex = i;
                }
            }
        }
        oldPathingType = pathingType;
    }

    // Update is called once per frame
    void Update()
    {   
        switch (pathingType)
        {
            case SheepPathingType.Stationary:
                //sheep doesn't move, keep information on its position, FoV rotation and facing direction

                oldPos = GetSheepPos();
                oldRot = transform.GetChild(3).rotation;
                oldScale = transform.localScale;

                break;

            case SheepPathingType.Patrolling:
                //move along designated path, keep information on its current position, FoV rotation and facing direction
                //make sure the patrol points are close to each other for more natural behaviour

                oldPos = GetSheepPos();
                oldRot = transform.GetChild(3).rotation;
                oldScale = transform.localScale;

                if (!movingToNextTile && aiPatrolSpots.Length > 0)
                {
                    if (!sheep.IsSheared)
                    {
                        if (slot != null)
                        {
                            travelPath = GeneratePath(PositionToWorldVector2Int(slot.position));
                            if (travelPath != null)
                            {
                                pathFound = true;
                                float dist = Vector3.Distance(transform.position, slot.position);
                                if (dist < 1.8f)
                                {
                                    pathFound = false;
                                    travelPath = null;
                                }
                            }
                        }
                        else
                        {
                            Patrol();
                        }
                    }
                }
                break;

            case SheepPathingType.ToSweater:
                //make naked sheep go to sweater, then return to its original path
                if (!movingToNextTile)
                {
                    if (this.tag == "Clothed")
                    {
                        pathingType = SheepPathingType.Returning;
                    }
                    else if (!pathFound || travelPath.Count == 0)
                    {
                        pos = GetSheepPos();
                        travelPath = GeneratePath(sweaterPos);
                        if (travelPath != null)
                        {
                            pathFound = true;
                        }
                    }
                }
                break; 

            case SheepPathingType.ToPlayer:
                if (!movingToNextTile)
                {
                    if (!pathFound || travelPath.Count == 0 || SameRoomAsTarget() || PlayerChangedRooms())
                    {
                        pos = GetSheepPos();
                        travelPath = GeneratePath(Wolf.GetWolfPos());
                        if (travelPath != null)
                        {
                            pathFound = true;

                            /*for (int i = 0; i < travelPath.Count; i++)
                            {
                                Debug.Log("checking path to player (" + travelPath.Count + " tiles): " + travelPath[i].x + "," + travelPath[i].y);
                            }*/
                        }
                    }
                    if (wolf.escaped) // Return to old position if wolf escapes sheep's FoV
                    {
                        pathingType = SheepPathingType.Returning;
                    }
                    else
                    {
                        // increment detection meter
                    }
                }

                //if a path has already been made, and the player is in a different room than the sheep and the player is in the same room as when the path was made, go along that path
                //otherwise, make a new path first
                break;

            case SheepPathingType.Fleeing:
                //only occurs if near a wolf howl
                if (howlTimer > 0)
                {
                    howlTimer -= Time.deltaTime;
                    transform.Translate((this.transform.position - wolf.transform.position).normalized * movementSpeed * Time.deltaTime);
                    if (transform.localScale == new Vector3(1, 1, 1))
                    {
                        transform.localScale = new Vector3(-1, 1, 1);
                    }
                }
                else
                {
                    pathingType = SheepPathingType.Returning; // change to ToPlayer if flee will only be used if all sheep chase the player
                }
                break;

            case SheepPathingType.Returning:
                if (!movingToNextTile)
                {
                    if (pos == oldPos)
                    {
                        pathingType = oldPathingType;
                        if (oldPathingType == SheepPathingType.Stationary)
                        {
                            transform.localScale = oldScale;
                            transform.GetChild(3).rotation = oldRot;
                        }
                    }
                    else if (!pathFound || travelPath.Count == 0)
                    {
                        travelPath = GeneratePath(oldPos);
                        if (travelPath != null)
                        {
                            pathFound = true;
                            movingToNextTile = true;
                        }
                    }
                }
                break;
        }

        Move();
    }

    void Move()
    {
        if (movingToNextTile)
        {
            Vector2Int checkPos = GetSheepPos();
            if (checkPos != pos)
            {
                movingToNextTile = false;
                pos = checkPos;
            }
        }

        if (!movingToNextTile && pathFound)
        {
            if (pathingType == SheepPathingType.Stationary) return;

            if (travelPath.Count > 0)
            {
                nextPos = travelPath[0];
                travelPath.RemoveAt(0);
                movingToNextTile = true;
            }
            else
            {
                movingToNextTile = false;
                pathFound = false;
            }
        }

        if (movingToNextTile)
        {
            // move towards using seek or arrive
            // Arrive();
        }
    }

    private void Patrol()
    {
        pos = GetSheepPos();

        travelPath = GeneratePath(PositionToWorldVector2Int(aiPatrolSpots[patrolSpotsIndex].GetPosition));

        if (travelPath != null)
        {
            pathFound = true;
            float dist = Vector3.Distance(transform.position, aiPatrolSpots[patrolSpotsIndex].GetPosition);
            if (dist < 1.8f)
            {
                pathFound = false;
                travelPath = null;
                patrolSpotsIndex = (patrolSpotsIndex + 1) % aiPatrolSpots.Length;
            }

        }
    }

    void Arrive()
    {
        Vector2 direction = nextPos - (Vector2)transform.position;


        if(direction.magnitude < 1.8f)
        {
            movingToNextTile = false;
            pathFound = false;
            return;
        }

        transform.position = (Vector2)transform.position + movementSpeed * direction * Time.deltaTime;
    }

    List<Vector2> GeneratePath(Vector2 destination)
    {
        List<Vector2> path = new List<Vector2>();
        Vector2 direction = destination - (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, direction.magnitude, LayerMask.NameToLayer("Obstacle"));
        if(hit)
        {
            Debug.Log("Sheep raycasted this: " + hit.transform.tag);

            
            pos = GetSheepPos();
            List<Vector2Int> pathInt = Pathing.AStar(pos, PositionToWorldVector2Int(destination));
            
            foreach(Vector2Int node in pathInt)
            {
                path.Add(node);
            }            
        }
        else
        {
            path.Add(destination);
        }

        return path;
    }

    bool SameRoomAsTarget()
    {
        //FIXIT actually do checking, once rooms are coded in
        switch (pathingType)
        {
            case SheepPathingType.Patrolling:
                break;
            case SheepPathingType.ToSweater:
                break;
            case SheepPathingType.ToPlayer:
                break;
        }

        return false;
    }

    bool PlayerChangedRooms()
    {
        //FIXIT actually do checking, once rooms are coded in
        return false;
    }

    // Group formation sets target slot for each sheep
    public void SetSlot(Transform target)
    {
        slot = target;
    }

    //Quick functions to reduce rewriting
    public Vector2Int PositionToWorldVector2Int(Vector2 position) { return (Vector2Int)GridManager.Instance.walkableTilemap.WorldToCell(new Vector3(position.x, position.y, 0)); }
    Vector2Int GetSheepPos() { return (Vector2Int)GridManager.Instance.walkableTilemap.WorldToCell(new Vector3(transform.position.x, transform.position.y, 0)); }
    bool NextPosUp() { return (pos.x == nextPos.x && pos.y + 1 == nextPos.y); }
    bool NextPosRight() { return (pos.x + 1 == nextPos.x && pos.y == nextPos.y); }
    bool NextPosDown() { return (pos.x == nextPos.x && pos.y - 1 == nextPos.y); }
    bool NextPosLeft() { return (pos.x - 1 == nextPos.x && pos.y == nextPos.y); }
}
