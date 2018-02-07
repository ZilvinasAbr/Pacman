using System;
using UnityEngine;
using System.Collections;
using Tobii.Gaming;

public class PlayerController : MonoBehaviour
{

    public float speed = 0.4f;
    Vector2 _dest = Vector2.zero;
    Vector2 _dir = Vector2.zero;
    Vector2 _nextDir = Vector2.zero;

    [Serializable]
    public class PointSprites
    {
        public GameObject[] pointSprites;
    }

    public PointSprites points;

    public static int killstreak = 0;

    // script handles
    private GameGUINavigation GUINav;
    private GameManager GM;
    private ScoreManager SM;

    private bool _deadPlaying = false;

    // Use this for initialization
    void Start()
    {
        GM = GameObject.Find("Game Manager").GetComponent<GameManager>();
        SM = GameObject.Find("Game Manager").GetComponent<ScoreManager>();
        GUINav = GameObject.Find("UI Manager").GetComponent<GameGUINavigation>();
        _dest = transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        switch (GameManager.gameState)
        {
            case GameManager.GameState.Game:
                ReadInputAndMove();
                Animate();
                break;

            case GameManager.GameState.Dead:
                if (!_deadPlaying)
                    StartCoroutine("PlayDeadAnimation");
                break;
        }


    }

    IEnumerator PlayDeadAnimation()
    {
        _deadPlaying = true;
        GetComponent<Animator>().SetBool("Die", true);
        yield return new WaitForSeconds(1);
        GetComponent<Animator>().SetBool("Die", false);
        _deadPlaying = false;

        if (GameManager.lives <= 0)
        {
            Debug.Log("Treshold for High Score: " + SM.LowestHigh());
            if (GameManager.score >= SM.LowestHigh())
                GUINav.getScoresMenu();
            else
                GUINav.H_ShowGameOverScreen();
        }

        else
            GM.ResetScene();
    }

    void Animate()
    {
        Vector2 dir = _dest - (Vector2)transform.position;
        GetComponent<Animator>().SetFloat("DirX", dir.x);
        GetComponent<Animator>().SetFloat("DirY", dir.y);
    }

    bool Valid(Vector2 direction)
    {
        // cast line from 'next to pacman' to pacman
        // not from directly the center of next tile but just a little further from center of next tile
        Vector2 pos = transform.position;
        direction += new Vector2(direction.x * 0.45f, direction.y * 0.45f);
        RaycastHit2D hit = Physics2D.Linecast(pos + direction, pos);
        return hit.collider.name == "pacdot" || (hit.collider == GetComponent<Collider2D>());
    }

    public void ResetDestination()
    {
        _dest = new Vector2(15f, 11f);
        GetComponent<Animator>().SetFloat("DirX", 1);
        GetComponent<Animator>().SetFloat("DirY", 0);
    }

    void ReadInputAndMove()
    {
        // move closer to destination
        Vector2 p = Vector2.MoveTowards(transform.position, _dest, speed);
        GetComponent<Rigidbody2D>().MovePosition(p);

        _nextDir = GetDirectionFromGaze();

        // if pacman is in the center of a tile
        if (Vector2.Distance(_dest, transform.position) < 0.00001f)
        {
            if (Valid(_nextDir))
            {
                _dest = (Vector2)transform.position + _nextDir;
                _dir = _nextDir;
            }
            else   // if next direction is not valid
            {
                if (Valid(_dir))  // and the prev. direction is valid
                    _dest = (Vector2)transform.position + _dir;   // continue on that direction

                // otherwise, do nothing
            }
        }
    }

    private Vector2 GetDirectionFromKeyboard()
    {
        if (Input.GetAxis("Horizontal") > 0) return Vector2.right;
        if (Input.GetAxis("Horizontal") < 0) return -Vector2.right;
        if (Input.GetAxis("Vertical") > 0) return Vector2.up;
        if (Input.GetAxis("Vertical") < 0) return -Vector2.up;
        
        return Vector2.zero;
    }

    public float MinimumRadiusToChangeDirection = 100f;
    private Vector3 _previousDirection;

    private Vector2 GetDirectionFromGaze()
    {
        var gazePosition = TobiiAPI.GetGazePoint().Screen;
        var playerPosition = Camera.main.WorldToScreenPoint(transform.position);

        var polarRelativeToPlayer = CartesianToPolar(gazePosition, playerPosition);
        var radius = polarRelativeToPlayer.x;
        var angle = polarRelativeToPlayer.y;

        var nextDirection = GetNextDirection(radius, angle);
        _previousDirection = nextDirection;
        return nextDirection;
    }

    private static Vector2 CartesianToPolar(Vector2 gazePosition, Vector2 playerPosition)
    {
        var angle = 180.0 - Math.Atan2(gazePosition.y - playerPosition.y, playerPosition.x - gazePosition.x) * 180.0 / Math.PI;
        var radius = Math.Sqrt(Math.Pow(gazePosition.x - playerPosition.x, 2) +
                               Math.Pow(gazePosition.y - playerPosition.y, 2));
        return new Vector2((float)radius, (float)angle);
    }

    private Vector2 GetNextDirection(float radius, float angle)
    {
        if (angle >= 0 && angle < 45 || angle >= 315 && angle < 360) return Vector2.right;
        if (angle >= 45 && angle < 135) return Vector2.up;
        if (angle >= 135 && angle < 225) return Vector2.left;
        if (angle >= 225 && angle < 315) return Vector2.down;
        return Vector2.zero;
    }

    private bool ShouldChangeDirection(float radius)
    {
        return radius > MinimumRadiusToChangeDirection;
    }

    private static Vector3 DirectionRight(float speed)
    {
        return new Vector3(speed, 0, 0);
    }

    private static Vector3 DirectionLeft(float speed)
    {
        return new Vector3(-speed, 0, 0);
    }

    private static Vector3 DirectionUp(float speed)
    {
        return new Vector3(0, speed, 0);
    }

    private static Vector3 DirectionDown(float speed)
    {
        return new Vector3(0, -speed, 0);
    }

    public Vector2 getDir()
    {
        return _dir;
    }

    public void UpdateScore()
    {
        killstreak++;

        // limit killstreak at 4
        if (killstreak > 4) killstreak = 4;

        Instantiate(points.pointSprites[killstreak - 1], transform.position, Quaternion.identity);
        GameManager.score += (int)Mathf.Pow(2, killstreak) * 100;

    }
}
