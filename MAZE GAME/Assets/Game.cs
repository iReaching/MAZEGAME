using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Game : MonoBehaviour
{
    public float holep;
    public int w, h, x, y;
    public bool[,] hwalls, vwalls;
    public Transform Level, Player, Goal;
    public GameObject Floor, Wall, TorchPrefab, TorchPickupPrefab;
    public TextMeshProUGUI torchCounterText;
    public Joystick joystick;
    [SerializeField] private Cinemachine.CinemachineVirtualCamera virtualCamera;
    private bool cameraDebugLogged = false; // Tracks whether the zoom log has been triggered


    private int torchCount = 0; // Number of torches the player has
    private Vector2 currentJoystickDirection;
    private Vector2 lastJoystickDirection;
    private float joystickMoveCooldown = 0.4f;
    private float lastMoveTime;
    private float tapStartTime = -1f; // Tracks when the joystick was tapped
    private bool isMoving = false; // Tracks if the player is moving
    private float torchPlaceCooldown = 1.5f; // Cooldown duration in seconds
    private float lastTorchPlaceTime = -1f;  // Tracks the last time a torch was placed


    void Start()
    {
        GenerateMaze();
        UpdateTorchUI();
    }

    void Update()
    {
        HandleJoystickInput();
        SmoothPlayerMovement();
        CheckGoalReached();
        AdjustCameraZoom(); // Dynamically adjust the zoom
    }
    void AdjustCameraZoom()
    {
        // Calculate the desired zoom based on maze size
        float desiredZoom = Mathf.Clamp(Mathf.Max(w, h) * 0.5f, 2f, 10f);

        // Log debug message only once per maze generation
        if (!cameraDebugLogged)
        {
            Debug.Log($"Maze Generated: Adjusting Camera Zoom to {desiredZoom}");
            cameraDebugLogged = true; // Prevent logging again until the maze regenerates
        }

        // Smoothly interpolate the current zoom to the desired zoom
        virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(
            virtualCamera.m_Lens.OrthographicSize,
            desiredZoom,
            Time.deltaTime * 2f // Adjust zoom speed
        );
    }



    void GenerateMaze()
    {
        cameraDebugLogged = false; // Reset the flag to allow logging for the new maze
        foreach (Transform child in Level)
            Destroy(child.gameObject);

        hwalls = new bool[w + 1, h];
        vwalls = new bool[w, h + 1];
        var st = new int[w, h];
        var reachableCells = new List<Vector2Int>(); // Store all reachable cells

        void dfs(int x, int y)
        {
            st[x, y] = 1;
            Instantiate(Floor, new Vector3(x, y), Quaternion.identity, Level);
            reachableCells.Add(new Vector2Int(x, y)); // Add cell to reachable list

            var dirs = new[]
            {
            (x - 1, y, hwalls, x, y, Vector3.right, 90),
            (x + 1, y, hwalls, x + 1, y, Vector3.right, 90),
            (x, y - 1, vwalls, x, y, Vector3.up, 0),
            (x, y + 1, vwalls, x, y + 1, Vector3.up, 0),
        };
            foreach (var (nx, ny, wall, wx, wy, sh, ang) in dirs.OrderBy(d => Random.value))
            {
                if (!(0 <= nx && nx < w && 0 <= ny && ny < h) || (st[nx, ny] == 2 && Random.value > holep))
                {
                    wall[wx, wy] = true;
                    Instantiate(Wall, new Vector3(wx, wy) - sh / 2, Quaternion.Euler(0, 0, ang), Level);
                }
                else if (st[nx, ny] == 0)
                {
                    dfs(nx, ny);
                }
            }
            st[x, y] = 2;
        }
        dfs(0, 0);

        x = Random.Range(0, w);
        y = Random.Range(0, h);
        Player.position = new Vector3(x, y);

        do Goal.position = new Vector3(Random.Range(0, w), Random.Range(0, h));
        while (Vector3.Distance(Player.position, Goal.position) < (w + h) / 4);

        // Remove unreachable cells (e.g., near the flag or the player spawn point)
        reachableCells.RemoveAll(cell => cell == new Vector2Int(x, y));
        reachableCells.RemoveAll(cell => IsBlockedByFlag(cell));

        if (Random.Range(0f, 1f) < 0.75f) // 75% chance to spawn a torch
        {
            if (reachableCells.Count > 0)
            {
                bool torchPlaced = false;

                foreach (var cell in reachableCells)
                {
                    if (IsReachable(new Vector2Int(x, y), cell))
                    {
                        Instantiate(TorchPickupPrefab, new Vector3(cell.x, cell.y, 0), Quaternion.identity, Level);
                        Debug.Log($"Torch successfully spawned at: {cell}");
                        torchPlaced = true;
                        break;
                    }
                    else
                    {
                        Debug.LogWarning($"Cell {cell} is not reachable from the player's position ({x}, {y})!");
                    }
                }

                if (!torchPlaced)
                {
                    Debug.LogWarning("Fallback: Placing torch without IsReachable check!");

                    // Place the torch at the closest valid cell
                    Vector2Int fallbackCell = reachableCells[0];
                    float minDistance = float.MaxValue;

                    foreach (var cell in reachableCells)
                    {
                        float distance = Vector2Int.Distance(new Vector2Int(x, y), cell);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            fallbackCell = cell;
                        }
                    }

                    Instantiate(TorchPickupPrefab, new Vector3(fallbackCell.x, fallbackCell.y, 0), Quaternion.identity, Level);
                    Debug.Log($"Torch fallback placed at closest cell: {fallbackCell}");
                }

            }
            else
            {
                Debug.LogWarning("No reachable cells available for torch placement!");
            }
        }



    }

    bool IsBlockedByFlag(Vector2Int cell)
    {
        Vector3 flagPosition = Goal.position;
        return Vector2.Distance(new Vector2(cell.x, cell.y), new Vector2(flagPosition.x, flagPosition.y)) < 1.0f;
    }




    void HandleJoystickInput()
    {
        float horizontal = joystick.Horizontal;
        float vertical = joystick.Vertical;

        currentJoystickDirection = new Vector2(
            Mathf.Abs(horizontal) > Mathf.Abs(vertical) ? Mathf.Sign(horizontal) : 0,
            Mathf.Abs(vertical) > Mathf.Abs(horizontal) ? Mathf.Sign(vertical) : 0
        );

        if (currentJoystickDirection == Vector2.zero) // Joystick released
        {
            if (!isMoving && tapStartTime > 0 && Time.time - tapStartTime <= 0.2f) // Tap detected
            {
                PlaceTorch();
            }
            tapStartTime = -1f; // Reset tap timer
            isMoving = false;
        }
        else // Joystick is being used
        {
            if (tapStartTime < 0) // Start tracking tap
            {
                tapStartTime = Time.time;
            }

            if (Time.time - tapStartTime > 0.2f) // Hold detected
            {
                isMoving = true; // Start moving the player
                if (Time.time - lastMoveTime > joystickMoveCooldown)
                {
                    if (currentJoystickDirection.x < 0) MoveLeft();
                    if (currentJoystickDirection.x > 0) MoveRight();
                    if (currentJoystickDirection.y < 0) MoveDown();
                    if (currentJoystickDirection.y > 0) MoveUp();

                    lastMoveTime = Time.time;
                }
            }
        }
    }

    void SmoothPlayerMovement()
    {
        Player.position = Vector3.Lerp(Player.position, new Vector3(x, y), Time.deltaTime * 12);
    }

    void CheckGoalReached()
    {
        if (Vector3.Distance(Player.position, Goal.position) < 0.12f)
        {
            if (Random.Range(0, 5) < 3) w++;
            else h++;
            Start();
        }
    }

    public void MoveLeft()
    {
        TryMove(-1, 0, hwalls, x, y);
    }

    public void MoveRight()
    {
        TryMove(1, 0, hwalls, x + 1, y);
    }

    public void MoveDown()
    {
        TryMove(0, -1, vwalls, x, y);
    }

    public void MoveUp()
    {
        TryMove(0, 1, vwalls, x, y + 1);
    }

    void TryMove(int dx, int dy, bool[,] walls, int wx, int wy)
    {
        int nx = x + dx;
        int ny = y + dy;

        if (walls[wx, wy])
        {
            Player.position = Vector3.Lerp(Player.position, new Vector3(nx, ny), 0.1f);
        }
        else
        {
            x = nx;
            y = ny;

            CheckTorchPickup(new Vector3(nx, ny));
        }
    }
    bool IsReachable(Vector2Int start, Vector2Int target)
    {
        var queue = new Queue<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        queue.Enqueue(start);

        Debug.Log($"Starting pathfinding from {start} to {target}");

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == target)
            {
                Debug.Log($"Path found to {target}");
                return true;
            }

            if (visited.Contains(current))
                continue;

            visited.Add(current);

            // Add neighbors (check bounds and walls)
            var neighbors = new[]
            {
            new Vector2Int(current.x - 1, current.y),
            new Vector2Int(current.x + 1, current.y),
            new Vector2Int(current.x, current.y - 1),
            new Vector2Int(current.x, current.y + 1),
        };
            foreach (var neighbor in neighbors)
            {
                if (!visited.Contains(neighbor) && IsValidCell(neighbor))
                {
                    queue.Enqueue(neighbor);
                }
                else
                {
                    Debug.Log($"Neighbor {neighbor} is invalid or already visited.");
                }
            }
        }

        Debug.LogWarning($"No path found from {start} to {target}");
        return false;
    }


    // Check if the cell is valid (not a wall, within bounds, etc.)
    bool IsValidCell(Vector2Int cell)
    {
        // Check if the cell is within maze bounds
        if (cell.x < 0 || cell.x >= w || cell.y < 0 || cell.y >= h)
            return false;

        // Additional wall checks only if in bounds
        if (cell.x >= 0 && cell.x < hwalls.GetLength(0) && cell.y >= 0 && cell.y < hwalls.GetLength(1))
        {
            if (hwalls[cell.x, cell.y]) return false;
        }

        if (cell.x >= 0 && cell.x < vwalls.GetLength(0) && cell.y >= 0 && cell.y < vwalls.GetLength(1))
        {
            if (vwalls[cell.x, cell.y]) return false;
        }

        return true;
    }



    void CheckTorchPickup(Vector3 position)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, 0.1f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("TorchPickup"))
            {
                torchCount++;
                Destroy(hit.gameObject);
                UpdateTorchUI();
            }
        }
    }

    void PlaceTorch()
    {
        // Check cooldown
        if (Time.time - lastTorchPlaceTime < torchPlaceCooldown)
        {
            Debug.Log("Torch placement is on cooldown!");
            return;
        }

        if (torchCount > 0)
        {
            // Place the torch
            Instantiate(TorchPrefab, new Vector3(x, y, 0), Quaternion.identity, Level);
            torchCount--;
            UpdateTorchUI();

            // Update the last placement time
            lastTorchPlaceTime = Time.time;
        }
        else
        {
            Debug.Log("No torches available!");
        }
    }


    void UpdateTorchUI()
    {
        torchCounterText.text = "Torches: " + torchCount;
    }
}
