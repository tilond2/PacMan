using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*****************************************************************************
 * IMPORTANT NOTES - PLEASE READ
 * 
 * This is where all the code needed for the Ghost AI goes. There should not
 * be any other place in the code that needs your attention.
 * 
 * There are several sets of variables set up below for you to use. Some of
 * those settings will do much to determine how the ghost behaves. You don't
 * have to use this if you have some other approach in mind. Other variables
 * are simply things you will find useful, and I am declaring them for you
 * so you don't have to.
 * 
 * If you need to add additional logic for a specific ghost, you can use the
 * variable ghostID, which is set to 1, 2, 3, or 4 depending on the ghost.
 * 
 * Similarly, set ghostID=ORIGINAL when the ghosts are doing the "original" 
 * PacMan ghost behavior, and to CUSTOM for the new behavior that you supply. 
 * Use ghostID and ghostMode in the Update() method to control all this.
 * 
 * You could if you wanted to, create four separate ghost AI modules, one per
 * ghost, instead. If so, call them something like BlinkyAI, PinkyAI, etc.,
 * and bind them to the correct ghost prefabs.
 * 
 * Finally there are a couple of utility routines at the end.
 * 
 * Please note that this implementation of PacMan is not entirely bug-free.
 * For example, player does not get a new screenful of dots once all the
 * current dots are consumed. There are also some issues with the sound 
 * effects. By all means, fix these bugs if you like.
 * 
 *****************************************************************************/

public class GhostAI : MonoBehaviour {

    const int BLINKY = 1;   // These are used to set ghostID, to facilitate testing.
    const int PINKY = 2;
    const int INKY = 3;
    const int CLYDE = 4;
    public int ghostID;     // This variable is set to the particular ghost in the prefabs,

    const int ORIGINAL = 1; // These are used to set ghostMode, needed for the assignment.
    const int CUSTOM = 2;
    public int ghostMode;   // ORIGINAL for "original" ghost AI; CUSTOM for your unique new AI

    Movement move;
    private Vector3 startPos;
    private bool[] dirs = new bool[4];
	private bool[] prevDirs = new bool[4];

	public float releaseTime = 0f;          // This could be a tunable number
	private float releaseTimeReset = 0f;
	public float waitTime = 0f;             // This could be a tunable number
    private const float ogWaitTime = .1f;
	public int range = 0;                   // This could be a tunable number

    public float chaseTime = 0f;
    public float gateTime = 0f;

    public bool dead = false;               // state variables
	public bool fleeing = false;

	//Default: base value of likelihood of choice for each path
	public float Dflt = 1f;

	//Available: Zero or one based on whether a path is available
	int A = 0;

	//Value: negative 1 or 1 based on direction of pacman
	int V = 1;

	//Fleeing: negative if fleeing
	int F = 1;

	//Priority: calculated preference based on distance of target in one direction weighted by the distance in others (dist/total)
	float P = 0f;

    // Variables to hold distance calcs
	float distX = 0f;
	float distY = 0f;
	float total = 0f;

    // Percent chance of each coice. order is: up < right < 0 < down < left for random choice
    // These could be tunable numbers. You may or may not find this useful.
    public float[] directions = new float[4];
    public float[] distances = new float[4];
    Vector2 distance;
    //remember previous choice and make inverse illegal!
    private int[] prevChoices = new int[4]{1,1,1,1};

    // This will be PacMan when chasing, or Gate, when leaving the Pit
	public GameObject target;
    public Vector2 targetLocation;
	GameObject gate;
    GameObject[] gates;
	GameObject pacMan;

	public bool chooseDirection = true;
	public int[] choices ;
	public float choice;
    public float differenceX, differenceY;
    public int prevDir;


    public enum State{
		waiting,
		entering,
		leaving,
		active,
		fleeing,
        scatter         // Optional - This is for more advanced ghost AI behavior
	}

	public State _state = State.waiting;

    // Use this for initialization
    private void Awake()
    {
        startPos = this.gameObject.transform.position;
    }

    void Start () {
		move = GetComponent<Movement> ();
		gate = GameObject.Find("Gate(Clone)");
		pacMan = GameObject.Find("PacMan(Clone)") ? GameObject.Find("PacMan(Clone)") : GameObject.Find("PacMan 1(Clone)");
		releaseTimeReset = releaseTime;
        choices = new int[4];
        gates = GameObject.FindGameObjectsWithTag("gate");
    }

	public void restart(){
		releaseTime = releaseTimeReset;
		transform.position = startPos;
		_state = State.waiting;
	}
	
    /// <summary>
    /// This is where most of the work will be done. A switch/case statement is probably 
    /// the first thing to test for. There can be additional tests for specific ghosts,
    /// controlled by the GhostID variable. But much of the variations in ghost behavior
    /// could be controlled by changing values of some of the above variables, like
    /// 
    /// </summary>
	void Update () {
        switch (_state){
        case (State.waiting):
            //releaseTime += Time.deltaTime;
            Debug.Log("Waiting");
            move.state = 0;
            // below is some sample code showing how you deal with animations, etc.
            move._dir = Movement.Direction.still;
            if (releaseTime <= 0f)
            {
                chooseDirection = true;
                gameObject.GetComponent<Animator>().SetBool("Dead", false);
                gameObject.GetComponent<Animator>().SetBool("Running", false);
                gameObject.GetComponent<Animator>().SetInteger("Direction", 0);
                gameObject.GetComponent<Movement>().MSpeed = 5f;

                _state = State.leaving;

                // etc.
            }
            gameObject.GetComponent<Animator>().SetBool("Dead", false);
            gameObject.GetComponent<Animator>().SetBool("Running", false);
            gameObject.GetComponent<Animator>().SetInteger("Direction", 0);
            gameObject.GetComponent<Movement>().MSpeed = 5f;
            releaseTime -= Time.deltaTime;
            // etc.
            break;


        case (State.leaving):
            move.state = 1;
            getDirections();
            Debug.Log("Leaving");
            target = gate;
            targetLocation = new Vector2(13.5f, -11f);
                
                differenceX = transform.position.x - targetLocation.x;
            differenceY = transform.position.y - targetLocation.y;
                prevDir = 0;
            chaseTarget();
            if (transform.position.y >= -11f)
            {
                _state = State.active;
            }
            break;
        case (State.active):
            target = pacMan;
            Debug.Log("Active");
            getDirections();
            chaseTime -= Time.deltaTime;
            if (chaseTime < 0f)
            {
                activeBehavior();
            }
                
            move.state = 2;

            


            if (dead)
            {
                // etc.
                // most of your AI code will be placed here!
            }
            if (gameObject.GetComponent<Movement>().MSpeed == 0)
            {
                Debug.Log("Stopped");
            }
            // etc.

            break;

        case State.entering:
            Debug.Log("Entering");
            // Leaving this code in here for you.
            move._dir = Movement.Direction.still;
               
            if (transform.position.x < 13.48f || transform.position.x > 13.52)
            {
                //print ("GOING LEFT OR RIGHT");
                transform.position = Vector3.Lerp(transform.position, new Vector3(13.5f, transform.position.y, transform.position.z), 3f * Time.deltaTime);
            }
            else if (transform.position.y > -13.99f || transform.position.y < -14.01f)
            {
                gameObject.GetComponent<Animator>().SetInteger("Direction", 2);
                transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, -14f, transform.position.z), 3f * Time.deltaTime);
            }
            else
            {
                fleeing = false;
                dead = false;
                gameObject.GetComponent<Animator>().SetBool("Running", true);
                _state = State.waiting;
            }

            break;
        }
    }

    // Utility routines

	Vector2 num2vec(int n){
        switch (n)
        {
            case 0:
                return new Vector2(0, 1);
            case 1:
    			return new Vector2(1, 0);
		    case 2:
			    return new Vector2(0, -1);
            case 3:
			    return new Vector2(-1, 0);
            default:    // should never happen
                return new Vector2(0, 0);
        }
	}
    void getDirections()
    {
        
        prevChoices = choices;
        for (int i = 0; i < 4; i++)
        {
            if (gameObject.GetComponent<Movement>().checkDirectionClear(num2vec(i)) && i!=prevDir)
            {
                choices[i] = i;
            }
            else
            {
                choices[i] = -1;
            }
        }
    }
    bool compareDirections(bool[] n, bool[] p){
		for(int i = 0; i < n.Length; i++){
			if (n [i] != p [i]) {
				return false;
			}
		}
		return true;
	}

    void activeBehavior() {
        switch (ghostID) {
            //Blinky
            case 1:
                distance.x = transform.position.x - target.transform.position.x;
                distance.y = transform.position.y - target.transform.position.y;
                differenceX = transform.position.x - target.transform.position.x;
                differenceY = transform.position.y - target.transform.position.y;
                chaseTarget();
                break;
            //Pinky
            case 2:
                int direction = pacMan.GetComponent<PlayerMovement>().move.checkOrientation();
                int addX =0, addY = 0;
                switch (direction)
                {
                    case 0:
                        addY = 4;
                        addX = 4;
                        break;
                    case 1:
                        addX = 4;
                        break;
                    case 2:
                        addY = -4;
                        break;
                    case 3:
                        addX = -4;
                        break;
                }
                
                differenceX = transform.position.x - target.transform.position.x + addX + addY;
                differenceY = transform.position.y - target.transform.position.y + addX + addY;
                chaseTarget();
                break;
            case 3:
                break;
            case 4:
                break;
        }
    }

    void fleeingBehavior() {
        switch (ghostID) {
            case 1:
                
                break;
            case 2:
                break;
            case 3:
                break;
            case 4:
                break;
        }
    }
    float lowest(float[] inputs)
    {
        float lowest = inputs[0];
        foreach (var input in inputs)
            if (input < lowest) lowest = input;
        return lowest;
    }
    void chaseTarget()
    {
        chaseTime = 0.1f;
        prevDir = move.checkOppositeDirection();
        /*
        for (int i = 0; i < 4; i++) distances[i] = 9999f;
        Vector2 distCopy;
        if (choices.Contains(0))
        {
            distCopy.x = transform.position.x - target.transform.position.x;
            distCopy.y = transform.position.y - target.transform.position.y + .75f;
            distances[0] = distCopy.magnitude;
        }
        if (choices.Contains(1))
        {
            distCopy.x = differenceX + .75f;
            distCopy.y = differenceY;
            distances[1] = distCopy.magnitude;
        }
        if (choices.Contains(2))
        {
            distCopy.x = differenceX;
            distCopy.y = differenceY - .75f;
            distances[2] = distCopy.magnitude;
        }
        if (choices.Contains(3))
        {
            distCopy.x = differenceX - .75f;
            distCopy.y = differenceY;
            distances[3] = distCopy.magnitude;
        }
        float turn = lowest(distances);
        Debug.Log("lowest = " + turn);
        for (int i = 0; i < 4; i++)
        {
            if (turn == distances[i])
            {
                if (i == 0)
                {
                    move._dir = Movement.Direction.up;
                    Debug.Log("going up " + distances[i]);
                    break;
                }
                if (i == 1)
                {
                    move._dir = Movement.Direction.right;
                    Debug.Log("going right " + distances[i]);
                    break;
                }
                if (i == 2)
                {
                    move._dir = Movement.Direction.down;
                    Debug.Log("going down = " + distances[i]);
                    break;
                }
                if (i == 3)
                {
                    move._dir = Movement.Direction.left;
                    Debug.Log("going left " + distances[i]);
                    break;
                }
                break;
            }
        }
        //prevDir = move.checkOppositeDirection();
        */


        
        if (differenceY < 0.1f && choices.Contains(0))
        {
            if (Mathf.Abs(differenceY) > Mathf.Abs(differenceX))
            {
                move._dir = Movement.Direction.up;
            }
        }
        else if (differenceX < 0.1f && choices.Contains(1))
        {
            if (Mathf.Abs(differenceX) > Mathf.Abs(differenceY))
            {
                move._dir = Movement.Direction.right;
            }
            else if (differenceY > 0.1f && choices.Contains(2))
            {
                move._dir = Movement.Direction.down;
            }
            else if (differenceY > 0.1f && !choices.Contains(2))
            {
                move._dir = Movement.Direction.right;
            }
            else
            {
                move._dir = Movement.Direction.right;
            } 
        }
        else if (differenceY > 0.1f && choices.Contains(2))
        {
            move._dir = Movement.Direction.down;
        }
        else if (differenceX > 0.1f && choices.Contains(3))
        {
            move._dir = Movement.Direction.left;
        }else if (choices.Contains(0)) move._dir = Movement.Direction.up;
        else if (choices.Contains(1)) move._dir = Movement.Direction.right;
        else if (choices.Contains(2)) move._dir = Movement.Direction.down;
        else if (choices.Contains(3)) move._dir = Movement.Direction.left;
        if (move._dir != Movement.Direction.still)
        {
            prevDir = move.checkOppositeDirection();
        }

    }
}
