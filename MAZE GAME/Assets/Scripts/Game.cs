using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    public static Game instance;
    public GameObject gameOverPanel; // Reference to the Game Over popup
    public GameObject trapPrefab; // Assign the Trap prefab in the inspector
    private int trapCount = 1; // Number of traps to spawn per maze
    private int mazeGenerationCount = 0; // Tracks the number of mazes generated

    public float initialTime = 60f; // Starting time in seconds
    private float currentTime; // Current remaining time
    private bool isGameOver = false; // Tracks game over state
    public TextMeshProUGUI timerText; // UI Text to display the timer

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
    private float torchActivationCooldown = 1.5f; // Cooldown duration in seconds
    private float lastTorchActivationTime = -1f;  // Tracks the last time a torch was placed
    private void Awake()
    {
        if (instance == null)
        {
            instance = this; // Set the singleton instance
            DontDestroyOnLoad(gameObject); // Ensure this Game object persists across scenes
        }
        else
        {
            Debug.LogWarning("Duplicate Game instance found. Destroying this instance.");
            Destroy(gameObject); // Prevent duplicate Game objects
        }
    }
   
    void Start()
    {
        // Initialize game state
        if (currentTime <= 0)
        {
            currentTime = initialTime;
        }
        isGameOver = false;
        UpdateTimerUI();
        GenerateMaze();
        UpdateTorchUI();
    }

    void Update()
    {
        if (!isGameOver)
        {
            currentTime -= Time.deltaTime; // Reduce time
            if (currentTime <= 0)
            {
                currentTime = 0;
                GameOver(); // Trigger Game Over
            }
            UpdateTimerUI();
        }
        HandleJoystickInput();
        SmoothPlayerMovement();
        CheckGoalReached();
        AdjustCameraZoom(); // Dynamically adjust the zoom
    }
    void UpdateTimerUI()
    {
        if (timerText == null)
        {
            Debug.LogWarning("TimerText is null. Cannot update timer UI.");
            return;
        }

        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        timerText.text = $"Time: {minutes:00}:{seconds:00}";

        // Change color to red if time is low
        timerText.color = currentTime <= 20f ? Color.red : Color.white;
    


        // Change color to red if time is low
        if (currentTime <= 20f)
        {
            timerText.color = Color.red; // Change to red
        }
        else
        {
            timerText.color = Color.white; // Default color
        }
    }
    public void AddTime(float extraTime)
    {
        currentTime += extraTime; // Update the timer
        UpdateTimerUI();          // Refresh the displayed timer
        Debug.Log($"Time adjusted by {extraTime}. Current time: {currentTime}");
    }
    public void RestartGame()
    {
        AudioManager.instance.PlaySFX("ButtonPress");
        Debug.Log("Restarting game...");
        Time.timeScale = 1f; // Resume the game
        gameOverPanel.SetActive(false); // Hide the popup

        // Reset the game state
        ResetGameState();
    }
    public void QuitGame()
    {
        AudioManager.instance.PlaySFX("ButtonPress");
        Application.Quit();
    }
    void GameOver()
    {
        isGameOver = true;
        Debug.Log("Game Over triggered!");
        Time.timeScale = 0f; // Pause the game
        gameOverPanel.SetActive(true); // Show the Game Over popup
    }
    private void ResetGameState()
    {
        // Reset game variables (time, torch count, etc.)
        currentTime = initialTime;
        torchCount = 0;
        isGameOver = false;

        // Regenerate the maze or reset any other states
        GenerateMaze();
        UpdateTimerUI();
        UpdateTorchUI();
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
        foreach (Transform child in Level)
            Destroy(child.gameObject);

        hwalls = new bool[w + 1, h];
        vwalls = new bool[w, h + 1];
        var st = new int[w, h];
        var reachableCells = new List<Vector2Int>();

        // Depth-first search (DFS) to generate the maze
        void dfs(int x, int y)
        {
            st[x, y] = 1;
            Instantiate(Floor, new Vector3(x, y), Quaternion.identity, Level);
            reachableCells.Add(new Vector2Int(x, y)); // Track all reachable cells

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

        // Place player
        x = Random.Range(0, w);
        y = Random.Range(0, h);
        Player.position = new Vector3(x, y);

        // Place the flag as far as possible from the player
        PlaceFlagAsFarAsPossible(reachableCells);

        // Generate torches and traps
        GenerateTorch(reachableCells);
        if (mazeGenerationCount >= 3)
        {
            GenerateTraps(reachableCells);
        }

        mazeGenerationCount++;
    }

    // Place the flag as far as possible from the player's starting position
    void PlaceFlagAsFarAsPossible(List<Vector2Int> reachableCells)
    {
        Vector2Int playerPosition = new Vector2Int(x, y);
        Vector2Int farthestCell = playerPosition;
        float maxDistance = float.MinValue;

        foreach (Vector2Int cell in reachableCells)
        {
            float distance = Vector2.Distance(playerPosition, cell);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                farthestCell = cell;
            }
        }

        Goal.position = new Vector3(farthestCell.x, farthestCell.y, 0);
        Debug.Log($"Flag placed at: {farthestCell}, distance from player: {maxDistance}");
    }

    // Torch Generation
    void GenerateTorch(List<Vector2Int> reachableCells)
    {
        reachableCells.RemoveAll(cell => cell == new Vector2Int(x, y)); // Remove player position
        reachableCells.RemoveAll(cell => IsBlockedByFlag(cell)); // Remove cells near the flag

        foreach (var cell in reachableCells.OrderBy(c => Random.value).Take(1)) // Generate 3 torches
        {
            Instantiate(TorchPickupPrefab, new Vector3(cell.x, cell.y, 0), Quaternion.identity, Level);
        }
    }

    // Trap Generation
    void GenerateTraps(List<Vector2Int> reachableCells)
    {
        if (trapCount <= 0) return; // No traps to spawn

        reachableCells.RemoveAll(cell => cell == new Vector2Int(x, y)); // Remove player position
        reachableCells.RemoveAll(cell => IsBlockedByFlag(cell)); // Remove cells near the flag

        for (int i = 0; i < trapCount; i++) // Loop based on trapCount
        {
            int attempts = 0;
            bool valid = false;
            Vector2Int trapCell = Vector2Int.zero;

            do
            {
                trapCell = reachableCells[Random.Range(0, reachableCells.Count)];
                valid = IsValidTrapCell(trapCell);
                attempts++;
            }
            while (!valid && attempts < 100);

            if (valid)
            {
                reachableCells.Remove(trapCell); // Remove cell to prevent overlap
                Instantiate(trapPrefab, new Vector3(trapCell.x, trapCell.y, 0), Quaternion.identity, Level);
                Debug.Log($"Trap placed at: {trapCell}");
            }
            else
            {
                Debug.LogWarning("Unable to find a valid position for trap.");
            }
        }
    }


    // Validates trap placement
    bool IsValidTrapCell(Vector2Int cell)
    {
        if (cell == new Vector2Int(x, y)) return false; // Avoid player position
        if (Vector2.Distance(new Vector2(cell.x, cell.y), Goal.position) < 1.0f) return false; // Avoid near flag

        Collider2D[] hits = Physics2D.OverlapCircleAll(new Vector2(cell.x, cell.y), 0.1f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("TorchPickup") || hit.CompareTag("Trap"))
            {
                return false; // Avoid overlapping torches or traps
            }
        }
        return true;
    }

    // Checks if a cell is blocked by the flag
    bool IsBlockedByFlag(Vector2Int cell)
    {
        Vector3 flagPosition = Goal.position;
        return Vector2.Distance(new Vector2(cell.x, cell.y), new Vector2(flagPosition.x, flagPosition.y)) < 1.0f;
    }
    void HandleJoystickInput()
    {
        if (joystick == null)
        {
            Debug.LogWarning("Joystick reference is null. Skipping joystick input.");
            return;
        }
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
                ActivateTorch();
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
            AudioManager.instance.PlaySFX("ObjectiveGrab");
            AddTime(10); // Add time for reaching the goal
            Debug.Log("Time extended! Current time: " + currentTime);

            // Increment maze dimensions
            if (Random.Range(0, 5) < 3) w++;
            else h++;


            RegenerateMaze(); // Call a dedicated maze regeneration method
        }
    }
    void RegenerateMaze()
    {
        // Reset any flags or states for the new maze
        cameraDebugLogged = false;

        // Regenerate the maze
        GenerateMaze();

        // Reset player and goal states, if necessary
        UpdateTorchUI();
        UpdateTimerUI();
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

        

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == target)
            {
                
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
                    
                }
            }
        }

       
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
                AudioManager.instance.PlaySFX("PickableTorch");
                UpdateTorchUI();
            }
        }
    }
    void ActivateTorch()
    {
        // Ensure cooldown and torch availability
        if (torchCount > 0 && Time.time - lastTorchActivationTime >= torchActivationCooldown)
        {
            // Check if a torch is already active
            Transform existingTorch = Player.Find("Torch");
            if (existingTorch == null)
            {
                // Instantiate and attach the torch to the player
                GameObject torch = Instantiate(TorchPrefab, Player.position, Quaternion.identity);
                torch.name = "Torch";
                torch.transform.SetParent(Player); // Attach to the player

                // Play the torch activation SFX
                AudioManager.instance.PlaySFX("PlaceTorch");

                torchCount--; // Decrease torch count
                UpdateTorchUI();

                // Update last activation time
                lastTorchActivationTime = Time.time;
            }
        }
    }
    void UpdateTorchUI()
    {
        torchCounterText.text = "Torches: " + torchCount;
    }


}
