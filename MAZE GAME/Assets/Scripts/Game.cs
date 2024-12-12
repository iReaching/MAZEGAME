using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{


    public Animator animator;
    public static Game instance;
    public GameObject gameOverPanel; // Reference to the Game Over popup
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI statsText; // UI Text for detailed stats
    public GameObject trapPrefab; // Assign the Trap prefab in the inspector
    private int trapCount = 2; // Number of traps to spawn per maze
    private int mazeGenerationCount = 0; // Tracks the number of mazes generated

    public float initialTime = 60f; // Starting time in seconds
    private float currentTime; // Current remaining time
    private bool isGameStarted = false; // Tracks if gameplay has started
    private bool isGameOver = false; // Tracks game over state
    public TextMeshProUGUI timerText; // UI Text to display the timer

    public float holep;
    public int w, h, x, y;
    private int initialW, initialH; // Store initial maze size
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
    
    private int totalScore = 0;
    private int totalTorchesCollected = 0;
    private int totalGoalsReached = 0;
    private float totalTimePlayed = 0f;
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

    private void Start()
    {
        initialW = w;
        initialH = h;
        currentTime = initialTime;

        // Don't start gameplay logic until the Start button is pressed
        Debug.Log("Waiting for game to start...");
    }
    public void StartGame()
    {
        Debug.Log("Game Started!");
        isGameStarted = true;
        currentTime = initialTime;
        GenerateMaze();
        UpdateTorchUI();
        UpdateTimerUI();
    }
    void Update()
    {
        // Skip gameplay logic if the game hasn't started or is over
        if (!isGameStarted || isGameOver) return;

        // Update timer
        currentTime -= Time.deltaTime;
        totalTimePlayed += Time.deltaTime;

        if (currentTime <= 0)
        {
            currentTime = 0;
            GameOver();
        }
        UpdateTimerUI();
        
        HandleJoystickInput();
        SmoothPlayerMovement();
        CheckGoalReached();
        AdjustCameraZoom(); // Dynamically adjust the zoom
        AdjustCharacterAnimation();
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
    private void AddScore(int points)
    {
        totalScore += points;
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
        Debug.Log($"Total time spent in the game: {Mathf.FloorToInt(totalTimePlayed)} seconds");
        Time.timeScale = 0f; // Pause the game
        gameOverPanel.SetActive(true); // Show the Game Over popup
        // Display score and stats
        scoreText.text = $"Score: {totalScore}";
        statsText.text = $"Torches Collected: {totalTorchesCollected}\n" +
                         $"Goals Reached: {totalGoalsReached}\n" +
                         $"Total Time Played: {Mathf.FloorToInt(totalTimePlayed)}s";
    }
    private void ResetGameState()
    {
        // Reset game variables (time, torch count, etc.)
        currentTime = initialTime;
        torchCount = 0;
        isGameOver = false;

        // Reset maze dimensions
        w = initialW;
        h = initialH;
        // Reset stats
        totalScore = 0;
        totalTorchesCollected = 0;
        totalGoalsReached = 0;
        totalTimePlayed = 0f;

        foreach (Transform child in Level)
        {
            if (child.CompareTag("Trap") || child.CompareTag("TorchPickup"))
            {
                Destroy(child.gameObject); // Destroy traps and torches
                Debug.Log($"Destroyed object: {child.name}");
            }
        }
        // Regenerate the maze or reset any other states
        GenerateMaze();
        UpdateTimerUI();
        UpdateTorchUI();

    }
    private void AdjustCharacterAnimation()
    {
        Vector2 movement = new Vector2(
            currentJoystickDirection.x,
            currentJoystickDirection.y
        );

        bool isMoving = movement != Vector2.zero;
        animator.SetBool("isWalking", isMoving);
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
    private void ClearExistingSpikes()
{
    // Iterate through all children of the Level parent and destroy traps
    foreach (Transform child in Level)
    {
        if (child.CompareTag("Trap")) // Ensure the object has the "Trap" tag
        {
            Destroy(child.gameObject);
            Debug.Log($"Cleared spike: {child.name}");
        }
    }
}

    void GenerateMaze()
    {
        // Clear previous maze objects
        foreach (Transform child in Level)
            Destroy(child.gameObject);

        ClearExistingSpikes();

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

        Debug.Log($"Reachable cells before filtering: {reachableCells.Count}");

        // Step 1: Remove existing traps
        foreach (Transform child in Level)
        {
            if (child.CompareTag("Trap"))
            {
                Destroy(child.gameObject); // Destroy existing traps
                Debug.Log("Existing trap destroyed.");
            }
        }
        reachableCells.RemoveAll(cell => cell == new Vector2Int(0, 0)); // Remove bottom-left corner
        // Remove cells occupied by the player
        reachableCells.RemoveAll(cell => cell == new Vector2Int(x, y));

        // Remove cells near or on the flag (goal)
        reachableCells.RemoveAll(cell => IsBlockedByFlag(cell));

        // Ensure traps do not overlap the goal
        Vector2Int goalCell = new Vector2Int((int)Goal.position.x, (int)Goal.position.y);
        reachableCells.RemoveAll(cell => cell == goalCell);

        Debug.Log($"Reachable cells after filtering: {reachableCells.Count}");
        if (reachableCells.Count == 0)
        {
            Debug.LogWarning("No valid cells available for traps!");
            return;
        }

        for (int i = 0; i < trapCount; i++)
        {
            int attempts = 0;
            bool valid = false;
            Vector2Int trapCell = Vector2Int.zero;

            do
            {
                if (reachableCells.Count == 0)
                {
                    Debug.LogWarning("No valid cells remaining for traps!");
                    return;
                }

                trapCell = reachableCells[Random.Range(0, reachableCells.Count)];
                valid = IsValidTrapCell(trapCell);
                Debug.Log($"Attempt {attempts + 1}: Checking cell {trapCell} - Valid: {valid}");
                attempts++;
            }
            while (!valid && attempts < 100);

            if (valid)
            {
                reachableCells.Remove(trapCell); // Prevent overlapping traps
                Instantiate(trapPrefab, new Vector3(trapCell.x, trapCell.y, 0), Quaternion.identity, Level);
                Debug.Log($"Trap placed at: {trapCell}");
            }
            else
            {
                Debug.LogWarning("Unable to find a valid position for trap after 100 attempts.");
            }
        }
    }



    bool IsValidTrapCell(Vector2Int cell)
    {
        // Avoid player position
        if (cell == new Vector2Int(x, y))
        {
            Debug.Log($"Invalid cell {cell}: Player position.");
            return false;
        }

        // Avoid goal (stairs) position
        Vector2Int goalCell = new Vector2Int((int)Goal.position.x, (int)Goal.position.y);
        if (cell == goalCell)
        {
            Debug.Log($"Invalid cell {cell}: Goal position.");
            return false;
        }

        // Check for collisions with existing objects (like torches or traps)
        Collider2D[] hits = Physics2D.OverlapCircleAll(new Vector2(cell.x, cell.y), 0.1f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("TorchPickup") || hit.CompareTag("Trap") || hit.CompareTag("Goal"))
            {
                Debug.Log($"Invalid cell {cell}: Overlaps with {hit.gameObject.name} ({hit.tag}).");
                return false;
            }
        }

        return true; // Valid cell
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
            totalGoalsReached++; // Increment goals reached
            AddScore(100); // Add points for reaching the goal
            AddTime(10);
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
        animator.SetBool("isWalking", true);
        TryMove(-1, 0, hwalls, x, y);
        

    }

    public void MoveRight()
    {
        animator.SetBool("isWalking", true);
        TryMove(1, 0, hwalls, x + 1, y);
        
    }

    public void MoveDown()
    {
        animator.SetBool("isWalking", true);
        TryMove(0, -1, vwalls, x, y);
        
    }

    public void MoveUp()
    {
        animator.SetBool("isWalking", true);
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
                totalTorchesCollected++;
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

                // Trigger the glow animation
                animator.SetBool("isGlowing", true);

                // Set a timer to turn off the glow after the torch cooldown
                StartCoroutine(DeactivateGlowAfterDelay(5));

                // Update last activation time
                lastTorchActivationTime = Time.time;
            }
        }
    }
    private IEnumerator DeactivateGlowAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        animator.SetBool("isGlowing", false);
    }
    void UpdateTorchUI()
    {
        torchCounterText.text = "Torches: " + torchCount;
    }


}
