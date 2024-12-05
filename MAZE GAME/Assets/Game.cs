using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Game : MonoBehaviour
{
    public float holep;
    public int w, h, x, y;
    public bool[,] hwalls, vwalls;
    public Transform Level, Player, Goal;
    public GameObject Floor, Wall;
    public CinemachineVirtualCamera cam;
    public Joystick joystick; // Reference to Joystick
    private Vector2 currentJoystickDirection; // Tracks the current joystick direction
    private float joystickMoveCooldown = 0.4f; // Cooldown duration between moves
    private float lastMoveTime; // Timestamp of the last move

    void Start()
    {
        foreach (Transform child in Level)
            Destroy(child.gameObject);

        hwalls = new bool[w + 1, h];
        vwalls = new bool[w, h + 1];
        var st = new int[w, h];

        void dfs(int x, int y)
        {
            st[x, y] = 1;
            Instantiate(Floor, new Vector3(x, y), Quaternion.identity, Level);

            var dirs = new[]
            {
                (x - 1, y, hwalls, x, y, Vector3.right, 90, KeyCode.A),
                (x + 1, y, hwalls, x + 1, y, Vector3.right, 90, KeyCode.D),
                (x, y - 1, vwalls, x, y, Vector3.up, 0, KeyCode.S),
                (x, y + 1, vwalls, x, y + 1, Vector3.up, 0, KeyCode.W),
            };
            foreach (var (nx, ny, wall, wx, wy, sh, ang, k) in dirs.OrderBy(d => Random.value))
                if (!(0 <= nx && nx < w && 0 <= ny && ny < h) || (st[nx, ny] == 2 && Random.value > holep))
                {
                    wall[wx, wy] = true;
                    Instantiate(Wall, new Vector3(wx, wy) - sh / 2, Quaternion.Euler(0, 0, ang), Level);
                }
                else if (st[nx, ny] == 0) dfs(nx, ny);
            st[x, y] = 2;
        }
        dfs(0, 0);

        x = Random.Range(0, w);
        y = Random.Range(0, h);
        Player.position = new Vector3(x, y);
        do Goal.position = new Vector3(Random.Range(0, w), Random.Range(0, h));
        while (Vector3.Distance(Player.position, Goal.position) < (w + h) / 4);
        cam.m_Lens.OrthographicSize = Mathf.Pow(w / 3 + h / 2, 0.7f) + 1;
    }

    void Update()
    {
        HandleJoystickInput();
        SmoothPlayerMovement();
        CheckGoalReached();
    }

    void HandleJoystickInput()
    {
        // Get joystick input
        float horizontal = joystick.Horizontal;
        float vertical = joystick.Vertical;

        // Determine the primary direction of the joystick
        Vector2 direction = new Vector2(
            Mathf.Abs(horizontal) > Mathf.Abs(vertical) ? Mathf.Sign(horizontal) : 0,
            Mathf.Abs(vertical) > Mathf.Abs(horizontal) ? Mathf.Sign(vertical) : 0
        );

        // Update movement based on joystick direction
        if (direction != Vector2.zero)
        {
            // If direction changes or enough time has passed, move
            if (direction != currentJoystickDirection || Time.time - lastMoveTime > joystickMoveCooldown)
            {
                if (direction.x < 0) MoveLeft();
                if (direction.x > 0) MoveRight();
                if (direction.y < 0) MoveDown();
                if (direction.y > 0) MoveUp();

                currentJoystickDirection = direction;
                lastMoveTime = Time.time;
            }
        }
        else
        {
            // Reset direction if joystick is released
            currentJoystickDirection = Vector2.zero;
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
        }
    }
}
