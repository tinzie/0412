﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Aim of this class:
// Describe the trilateral movement of an agent across a 3D grid, with obstacle avoidance.

// Initial implementation only works for uniform size colliders (cube, sphere etc)
public class Pathfinder : MonoBehaviour
{
    /// <summary>
    ///  Grid Bits
    /// </summary>
    class Node
    {
        public struct intVector3
        {
            public int x, y, z;
        }

        public intVector3 intVec3;

        public Vector3 pos;
        public bool occupied;

        // booleans replace open and closed lists, as checking lists was too underperformant.
        public bool open;
        public bool visited;

        public float f, g, h;
        public Node parent;

        public void clear()
        {
            f = g = h = 0;
            parent = null;
            open = false;
            visited = false;
        }
    }


    //public static int gridSize = 10;
    public static float nodeSize = 1.0f;
    public static Vector3 centrePos = new Vector3(0.0f, 0.0f, 0.0f);
    public Vector3 gridBottomCorner;

    List<GameObject> obstacles;

    Node[,,] grid;

    /// <summary>
    /// A_Star bits
    /// </summary>


    public List<Vector3> requestPath(Vector3 from, Vector3 to)
    {
        Node fromNode = ReturnNodeFromVector3(from);

        int X = GameManager.Instance.mapX;
        int Y = GameManager.Instance.mapY;
        int Z = GameManager.Instance.mapZ;


        if (to.x > X || to.x < .0f
            || to.y > Y || to.y < .0f
            || to.z > Z || to.z < .0f)
        {
            Debug.Log("Demanded an out of range vector destination!");
            return null;
        }

        Node toNode = ReturnNodeFromVector3(to);


        List<Node> nodePath = FindPath(fromNode, toNode);

        if (nodePath == null)
            return null;

        List<Vector3> vector3Path = new List<Vector3>();

        //invert path and convert to Vector3
        // i > 0 as we don't want to add last one, see below comment
        for(int i = nodePath.Count - 1; i > 0; i--)
        {
            vector3Path.Add(nodePath[i].pos);
        }

        //last one should be exact position, not node position.
        vector3Path.Add(to);

        return vector3Path;
    }
    
    List<Node> FindPath(Node from, Node to)
    {
        Debug.Log("Starting to time");
        float timeatstart = Time.realtimeSinceStartup;
        List<Node> open = new List<Node>();
        Node current;

        current = from;
        open.Add(current);

        float clearNodeTime = Time.realtimeSinceStartup;
        foreach(Node node in grid)
        {
            node.clear();
        }
        Debug.Log("clearing node list took: " + (Time.realtimeSinceStartup - clearNodeTime));


        float visitedContainsCheckTime = 0.0f;
        float openContainsCheckTime = 0.0f;
        float openAddCheckTime = 0.0f;


        while (open.Count > 0)
        {
            current.f = current.g = current.h = 0;
            if (current.intVec3.Equals(to.intVec3))
            {
                List<Node> returnNodes = new List<Node>();
                // check this
                while (current.parent != null)
                {
                    returnNodes.Add(current);
                    current = current.parent;
                }
                
                Debug.Log("checking visited list took: " + (visitedContainsCheckTime));
                Debug.Log("checking open contains list took: " + (openContainsCheckTime));
                Debug.Log("adding node to Open took: " + (openAddCheckTime));

                float timeatend = Time.realtimeSinceStartup;
                Debug.Log("Pathfinding took: " + (timeatend - timeatstart));
                return returnNodes;
            }

            current = open[0];
            open.Remove(current);
            current.open = false;
            current.visited = true;
            
            List<Node> neighbours = GetValidNeighbours(current);
           
            for (int i = 0; i < neighbours.Count; i++)
            {
                float a = Time.realtimeSinceStartup;
                // skip if aleady ruled out.
                if (neighbours[i].visited)
                    continue;
                visitedContainsCheckTime += Time.realtimeSinceStartup - a;


                float g = neighbours[i].g + nodeSize;
                float h = manhattanHeuristic(neighbours[i], to);
                float f = g + h;

                float b = Time.realtimeSinceStartup;
                bool inOpen = neighbours[i].open;
                openContainsCheckTime += Time.realtimeSinceStartup - b;

                float c = Time.realtimeSinceStartup;
                if(!inOpen || (inOpen && (neighbours[i].f > f)))
                {
                    if (!inOpen)
                    {
                        neighbours[i].open = true;
                        open.Add(neighbours[i]);
                    }

                    neighbours[i].f = f;
                    neighbours[i].g = g;
                    neighbours[i].h = h;
                    neighbours[i].parent = current;
                }
                openAddCheckTime += Time.realtimeSinceStartup - c;

            }
        }


        Debug.Log("WARNING: Destination could not be found for path find!!");
        return null;
    }

    float manhattanHeuristic(Node from, Node to)
    {
        return Vector3.Distance(from.pos, to.pos);
    }

    List<Node> GetValidNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        
        for (int x = -1; x < 2; x++)
        {
            for (int y = -1; y < 2; y++)
            {
                for (int z = -1; z < 2; z++)
                {
                    // skip current node
                    if (x == 0 && y == 0 && z == 0)
                        continue;

                    int newNodeX = node.intVec3.x + x;
                    int newNodeY = node.intVec3.y + y;
                    int newNodeZ = node.intVec3.z + z;

                    // don't add off-screen nodes
                    if (newNodeX < 0 || newNodeY < 0 || newNodeZ < 0
                            || newNodeX >= GameManager.Instance.mapX || newNodeY >= GameManager.Instance.mapY || newNodeZ >= GameManager.Instance.mapZ)
                        continue;

                    // don't add occupied nodes
                    if (grid[newNodeX, newNodeY, newNodeZ].occupied)
                        continue;

                    neighbours.Add(grid[node.intVec3.x + x, node.intVec3.y + y, node.intVec3.z + z]);
                }
            }
        }

        return neighbours;
    }


    void Awake()
    {
        grid = new Node[GameManager.Instance.mapX, GameManager.Instance.mapY, GameManager.Instance.mapZ];

        transform.position = centrePos;

        //inital population of lists.
        obstacles = new List<GameObject>(GameObject.FindGameObjectsWithTag("Obstacle"));

        //gridBottomCorner = centrePos - (new Vector3(gridSize / 2.0f, gridSize / 2.0f, gridSize / 2.0f) * nodeSize);
        gridBottomCorner = Vector3.zero;

        //initiate grid
        InitiateGrid();

        //populate grid with inital objects.
        UpdateGridWithObstacles();
    }


    // Start is called before the first frame update
    void Start()
    {
        

        //JUST FOR TESTING
        //drawGrid();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void InitiateGrid()
    {
        for(int x = 0; x < GameManager.Instance.mapX; x++)
        {
            for (int y = 0; y < GameManager.Instance.mapY; y++)
            {
                for (int z = 0; z < GameManager.Instance.mapZ; z++)
                {

                    grid[x, y, z] = new Node();
                    // positions


                    float halfNodeSize = nodeSize / 2;

                    grid[x, y, z].pos.x = gridBottomCorner.x + (x * nodeSize) + halfNodeSize;
                    grid[x, y, z].pos.y = gridBottomCorner.y + (y * nodeSize) + halfNodeSize;
                    grid[x, y, z].pos.z = gridBottomCorner.z + (z * nodeSize) + halfNodeSize;

                    // indices
                    grid[x, y, z].intVec3.x = x;
                    grid[x, y, z].intVec3.y = y;
                    grid[x, y, z].intVec3.z = z;
                }
            }
        }

    }

    void UpdateGridWithObstacles()
    {
        foreach (GameObject obj in obstacles)
        {
            // transform 
            Vector3 pos = obj.transform.position;

            // get bottom corner of collider
            Vector3 colSize = obj.GetComponent<colliderManager>().size;

            //CORRECT
            Vector3 colBotCorner = pos - (colSize / 2.0f);
            Vector3 colTopCorner = pos + (colSize / 2.0f);

            //TEST: pos = 6.6, blah, blah
            //TEST: nodeSize = 1.0f

            //find bottom corner node
            Vector3 nodeBotCorner = colBotCorner - new Vector3(colBotCorner.x % nodeSize, colBotCorner.y % nodeSize, colBotCorner.z % nodeSize);
            Vector3 nodeTopCorner = colTopCorner - new Vector3(colTopCorner.x % nodeSize, colTopCorner.y % nodeSize, colTopCorner.z % nodeSize);

            //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //cube.transform.position = nodeBotCorner;
            //cube.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            //
            //GameObject cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //cube2.transform.position = nodeTopCorner;
            //cube2.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

            //TEST: nodeBotCorner = 6.6 - 0.6 = 6.0
            int botNodeX = (int)(nodeBotCorner.x / nodeSize);
            int botNodeY = (int)(nodeBotCorner.y / nodeSize);
            int botNodeZ = (int)(nodeBotCorner.z / nodeSize);

            int TopNodeX = (int)(nodeTopCorner.x / nodeSize);
            int TopNodeY = (int)(nodeTopCorner.y / nodeSize);
            int TopNodeZ = (int)(nodeTopCorner.z / nodeSize);

            //TEST: nodeX = (int)6.0 / 1.0f = 6
            //Node bottomNode = grid[nodeX, nodeY, nodeZ];

            // Now need to cover all boxes covered by that (currently only correct function on boxColliders)
            for(int x = botNodeX; x <= TopNodeX; x++)
            {
                for (int y = botNodeY; y <= TopNodeY; y++)
                {
                    for (int z = botNodeZ; z <= TopNodeZ; z++)
                    {
                        grid[x, y, z].occupied = true;
                    }
                }
            }

            // can use the collider's size to determine which grid cells the obstacle overlaps
            // then update the grid accordingly.


        }
    }

    // helper function for accessing nodes using Vector3 positions.
    // MAX VALUE CANNOT EQUAL OR EXCEED MAP SIZE
    Node ReturnNodeFromVector3(Vector3 vec)
    {
        Vector3 nodePos = vec - new Vector3(vec.x % nodeSize, vec.y % nodeSize, vec.z % nodeSize);

        // -1 for offset into 0 indexed array.
        int nodeX = (int)(nodePos.x / nodeSize);
        int nodeY = (int)(nodePos.y / nodeSize);
        int nodeZ = (int)(nodePos.z / nodeSize);

        return grid[nodeX, nodeY, nodeZ];
    }

    //JUST FOR TESTING
    void drawGrid()
    {
        MeshFilter filter = gameObject.GetComponent<MeshFilter>();
        var mesh = new Mesh();
        var verticies = new List<Vector3>();

        var indicies = new List<int>();
        for (int x = 0; x < GameManager.Instance.mapX; x++)
        {
            for (int y = 0; y < GameManager.Instance.mapY; y++)
            {
                for (int z = 0; z < GameManager.Instance.mapZ; z++)
                {                    
                    if (grid[x, y, z].occupied)
                    {
                        float halfNodeSize = nodeSize / 2;
                        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cube.transform.position = grid[x, y, z].pos;
                        cube.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                    }
                }
            }
        }
    }
}
