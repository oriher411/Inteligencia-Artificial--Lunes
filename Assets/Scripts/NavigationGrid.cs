using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(-100)]
public class NavigationGrid : MonoBehaviour {

    public static NavigationGrid Instance { get; private set; }

    [Header("Grid Settings")]
    public Vector3 gridOrigin = new Vector3(-35f, 0f, -35f);
    public int gridWidth = 50;
    public int gridHeight = 50;
    public float cellSize = 1.4f;
    public LayerMask obstacleMask;

    private bool[,] walkable;

    private class PathNode {
        public Vector2Int cell;
        public PathNode parent;
        public float gCost;
        public float hCost;

        public float FCost {
            get { return gCost + hCost; }
        }
    }

    private static readonly Vector2Int[] NeighborOffsets = new Vector2Int[] {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1)
    };

    public int GridWidth {
        get { return gridWidth; }
    }

    public int GridHeight {
        get { return gridHeight; }
    }

    void Awake() {
        Instance = this;
        BuildGrid();
    }

    public void BuildGrid() {
        GameObject barriersRoot = GameObject.Find("Arena Barriers");
        if (barriersRoot != null) {
            InteriorBarrierColliderFix.Prepare(barriersRoot.transform);
        }

        walkable = new bool[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++) {
            for (int z = 0; z < gridHeight; z++) {
                walkable[x, z] = IsWorldPositionWalkable(CellToWorld(new Vector2Int(x, z)));
            }
        }
    }

    bool IsWorldPositionWalkable(Vector3 worldPosition) {
        NavMeshHit navHit;

        if (!NavMesh.SamplePosition(worldPosition, out navHit, cellSize * 0.75f, NavMesh.AllAreas)) {
            return false;
        }

        if (Physics.CheckSphere(navHit.position + Vector3.up * 0.6f, cellSize * 0.25f, obstacleMask)) {
            return false;
        }

        return true;
    }

    public Vector2Int WorldToCell(Vector3 worldPosition) {
        Vector3 local = worldPosition - gridOrigin;
        int x = Mathf.Clamp(Mathf.FloorToInt(local.x / cellSize), 0, gridWidth - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(local.z / cellSize), 0, gridHeight - 1);
        return new Vector2Int(x, z);
    }

    public Vector3 CellToWorld(Vector2Int cell) {
        float x = gridOrigin.x + (cell.x + 0.5f) * cellSize;
        float z = gridOrigin.z + (cell.y + 0.5f) * cellSize;
        Vector3 samplePosition = new Vector3(x, gridOrigin.y + 1f, z);
        NavMeshHit hit;

        if (NavMesh.SamplePosition(samplePosition, out hit, cellSize * 2f, NavMesh.AllAreas)) {
            return hit.position;
        }

        return new Vector3(x, gridOrigin.y, z);
    }

    public bool IsWalkable(Vector2Int cell) {
        if (cell.x < 0 || cell.y < 0 || cell.x >= gridWidth || cell.y >= gridHeight) {
            return false;
        }

        return walkable[cell.x, cell.y];
    }

    public Vector2Int FindNearestWalkableCell(Vector2Int fromCell) {
        if (IsWalkable(fromCell)) {
            return fromCell;
        }

        int maxSearchRadius = Mathf.Max(gridWidth, gridHeight);

        for (int radius = 1; radius <= maxSearchRadius; radius++) {
            for (int x = -radius; x <= radius; x++) {
                for (int z = -radius; z <= radius; z++) {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(z) != radius) {
                        continue;
                    }

                    Vector2Int candidate = new Vector2Int(fromCell.x + x, fromCell.y + z);
                    if (IsWalkable(candidate)) {
                        return candidate;
                    }
                }
            }
        }

        return fromCell;
    }

    public List<Vector3> FindPath(Vector3 startWorld, Vector3 endWorld) {
        List<Vector3> worldPath = new List<Vector3>();

        Vector2Int startCell = WorldToCell(startWorld);
        Vector2Int endCell = WorldToCell(endWorld);

        startCell = FindNearestWalkableCell(startCell);
        endCell = FindNearestWalkableCell(endCell);

        if (startCell == endCell) {
            worldPath.Add(CellToWorld(endCell));
            return worldPath;
        }

        List<PathNode> openList = new List<PathNode>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        PathNode startNode = new PathNode {
            cell = startCell,
            gCost = 0f,
            hCost = GetHeuristicDistance(startCell, endCell)
        };

        openList.Add(startNode);

        while (openList.Count > 0) {
            PathNode currentNode = GetLowestFCostNode(openList);
            openList.Remove(currentNode);
            closedSet.Add(currentNode.cell);

            if (currentNode.cell == endCell) {
                return RetracePath(currentNode);
            }

            foreach (Vector2Int offset in NeighborOffsets) {
                Vector2Int neighborCell = new Vector2Int(currentNode.cell.x + offset.x, currentNode.cell.y + offset.y);

                if (!IsWalkable(neighborCell) || closedSet.Contains(neighborCell)) {
                    continue;
                }

                float moveCost = offset.x != 0 && offset.y != 0 ? 1.414f : 1f;
                float tentativeGCost = currentNode.gCost + moveCost;

                PathNode existingNode = openList.Find(node => node.cell == neighborCell);

                if (existingNode == null) {
                    PathNode neighborNode = new PathNode {
                        cell = neighborCell,
                        parent = currentNode,
                        gCost = tentativeGCost,
                        hCost = GetHeuristicDistance(neighborCell, endCell)
                    };
                    openList.Add(neighborNode);
                } else if (tentativeGCost < existingNode.gCost) {
                    existingNode.gCost = tentativeGCost;
                    existingNode.parent = currentNode;
                }
            }
        }

        worldPath.Add(CellToWorld(endCell));
        return worldPath;
    }

    PathNode GetLowestFCostNode(List<PathNode> nodes) {
        PathNode lowest = nodes[0];

        for (int i = 1; i < nodes.Count; i++) {
            if (nodes[i].FCost < lowest.FCost ||
                (Mathf.Approximately(nodes[i].FCost, lowest.FCost) && nodes[i].hCost < lowest.hCost)) {
                lowest = nodes[i];
            }
        }

        return lowest;
    }

    float GetHeuristicDistance(Vector2Int a, Vector2Int b) {
        int distX = Mathf.Abs(a.x - b.x);
        int distY = Mathf.Abs(a.y - b.y);
        return distX + distY + (1.414f - 2f) * Mathf.Min(distX, distY);
    }

    List<Vector3> RetracePath(PathNode endNode) {
        List<Vector3> path = new List<Vector3>();
        PathNode currentNode = endNode;

        while (currentNode != null) {
            path.Add(CellToWorld(currentNode.cell));
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path;
    }

    public Vector3 GetRandomWalkableWorldPosition(Vector3 nearPosition, float radius) {
        Vector2Int centerCell = WorldToCell(nearPosition);
        int cellRadius = Mathf.CeilToInt(radius / cellSize);

        for (int attempt = 0; attempt < 30; attempt++) {
            int offsetX = Random.Range(-cellRadius, cellRadius + 1);
            int offsetZ = Random.Range(-cellRadius, cellRadius + 1);
            Vector2Int candidateCell = new Vector2Int(centerCell.x + offsetX, centerCell.y + offsetZ);

            if (IsWalkable(candidateCell)) {
                return CellToWorld(candidateCell);
            }
        }

        return CellToWorld(FindNearestWalkableCell(centerCell));
    }

    public Vector3 GetFleeWorldPosition(Vector3 currentPosition, Vector3 threatPosition, float fleeDistance) {
        Vector3 fleeDirection = (currentPosition - threatPosition).normalized;
        Vector3 desiredPosition = currentPosition + fleeDirection * fleeDistance;
        Vector2Int fleeCell = FindNearestWalkableCell(WorldToCell(desiredPosition));
        return CellToWorld(fleeCell);
    }

    void OnDrawGizmosSelected() {
        if (walkable == null) {
            return;
        }

        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);

        for (int x = 0; x < gridWidth; x++) {
            for (int z = 0; z < gridHeight; z++) {
                if (!walkable[x, z]) {
                    continue;
                }

                Vector3 center = CellToWorld(new Vector2Int(x, z));
                Gizmos.DrawCube(center + Vector3.up * 0.05f, new Vector3(cellSize * 0.9f, 0.05f, cellSize * 0.9f));
            }
        }
    }

}
