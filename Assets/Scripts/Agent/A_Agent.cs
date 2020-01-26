﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO: change this name to AgentPathfinding
public class A_Agent : MonoBehaviour
{
    List<Vector3> path = new List<Vector3>();
    Pathfinder pathFinder;

    public float moveSpeed = 1.0f;

    // Start is called before the first frame update
    void Awake()
    {
        pathFinder = GameObject.FindGameObjectWithTag("PathFinder").GetComponent<Pathfinder>();
    }

    // Update is called once per frame
    void Update()
    {
        if(path.Count != 0)
        {
            transform.position = Vector3.MoveTowards(transform.position, path[0], moveSpeed * Time.deltaTime);
            
            if(Vector3.Distance(transform.position, path[0]) < 0.05f)
            {
                path.RemoveAt(0);
            }
        }
    }

    public void setPath(Vector3 target)
    {
        Debug.Log("pathfinding");
        nullifyPath();

        path = pathFinder.requestPath(transform.position, target);

        if (path == null)
        {
            path = new List<Vector3>();
            return;
        }
    }

    public void setSingleNodePath(Vector3 target)
    {
        nullifyPath();
        path.Add(target);

    }

    public void nullifyPath()
    {
        if(path.Count != 0)
            path.Clear();
    }
}
