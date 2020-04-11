﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheepBehavior : MonoBehaviour
{
    public enum SheepPathingType
    {
        Stationary,
        Patrolling,
        ToSweater,
        ToPlayer,
        Fleeing
    }

    public enum SheepDirection
    {
        North,
        East,
        South,
        West
    }

    public float movementSpeed = 2.0f;

    Vector2Int pos;
    Vector2Int nextPos;
    bool pathFound;
    bool movingToNextTile;
    SheepPathingType pathingType;
    List<Vector2Int> travelPath;

    // Start is called before the first frame update
    void Start()
    {
        pathFound = false;
        movingToNextTile = false;
        travelPath = new List<Vector2Int>();

        pathingType = SheepPathingType.ToPlayer;

        pos = Vector2Int.RoundToInt(new Vector2(transform.position.x, transform.position.y));
    }

    // Update is called once per frame
    void Update()
    {
        switch (pathingType)
        {
            case SheepPathingType.Stationary:
                //don't move, but make sure not sheared and if so, keep an eye out for a sweater. also be alert for the wolf
                break;

            case SheepPathingType.Patrolling:
                //move along designated path, but make sure not sheared and if so, keep an eye out for a sweater. also be alert for the wolf
                break;

            case SheepPathingType.ToSweater:
                //check if a path has already been made and follow that path if so, otherwise make a path to the sweater first
                break;

            case SheepPathingType.ToPlayer:
                if (!movingToNextTile)
                {
                    if (!pathFound || travelPath.Count == 0 || SameRoomAsTarget() || PlayerChangedRooms())
                    {
                        pos = GetSheepPos();
                        travelPath = Pathing.AStar(GetSheepPos(), Wolf.GetWolfPos());
                        pathFound = true;

                        for (int i = 0; i < travelPath.Count; i++)
                        {
                            Debug.Log("checking path to player (" + travelPath.Count + " tiles): " + travelPath[i].x + "," + travelPath[i].y);
                        }
                    }
                }

                //if a path has already been made, and the player is in a different room than the sheep and the player is in the same room as when the path was made, go along that path
                //otherwise, make a new path first
                break;

            case SheepPathingType.Fleeing:
                //flee away from source?
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

        if (!movingToNextTile)
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
            if (NextPosUp())
            {
                transform.Translate(new Vector3(0, movementSpeed, 0) * Time.deltaTime);
            }
            else if (NextPosRight())
            {
                transform.Translate(new Vector3(movementSpeed, 0, 0) * Time.deltaTime);
            }
            else if (NextPosDown())
            {
                transform.Translate(new Vector3(0, -movementSpeed, 0) * Time.deltaTime);
            }
            else if (NextPosLeft())
            {
                transform.Translate(new Vector3(-movementSpeed, 0, 0) * Time.deltaTime);
            }
            else
            {
                movingToNextTile = false;
                pathFound = false;
                Debug.LogError("Next tile isn't adjacent! Current: (" + pos.x + "," + pos.y + ") & Next: (" + nextPos.x + "," + nextPos.y + ")");
            }
            //figure out where the next tile is from current and continue in that direction
        }
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

    //Quick functions to reduce rewriting
    Vector2Int GetSheepPos() { return Vector2Int.RoundToInt(new Vector2(transform.position.x, transform.position.y)); }
    bool NextPosUp() { return (pos.x == nextPos.x && pos.y + 1 == nextPos.y); }
    bool NextPosRight() { return (pos.x + 1 == nextPos.x && pos.y == nextPos.y); }
    bool NextPosDown() { return (pos.x == nextPos.x && pos.y - 1 == nextPos.y); }
    bool NextPosLeft() { return (pos.x - 1 == nextPos.x && pos.y == nextPos.y); }
}
