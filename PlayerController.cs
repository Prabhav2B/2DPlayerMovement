using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{

    #region Variables

    [Space(15)]

    //Visual Representation of player Velocity
    public AnimationCurve movementCurve = new AnimationCurve();
    [Space(5)]
    public AnimationCurve movementDecayCurve = new AnimationCurve();

    [Space(15)]

    
    [Range(1, 100)]
    public int movementSpeed = 10;

    [Range(0.0f, 5.0f)]
    public float timeToReachFullSpeed = .5f;
    
    [Range(0.0f, 5.0f)]
    public float timeToFullyStop = .5f;

    [Range(1, 100)]
    public float jumpForce = 10f;

    [Range(1.0f, 10.0f)]
    public float fallMultiplier = 2.5f;
    [Range(1.0f, 10.0f)]
    public float lowJumpMultiplier = 2.0f;
    
    public LayerMask whatIsGround;

    [Space]
    public bool airControl;
    //Bolt is the increased acceleration achieved after a sudden change in direction, like a dash
    public bool shouldBolt = true;
    
    [Range(1, 20)]
    public int boltmovementSpeed = 5;

    [Space]
    public CircleCollider2D groundCheck;
    public GameObject jumpEffect;


    Rigidbody2D rb;
    Vector2 dir;
    float movementTimer;
    int prevDir;
    bool directionHasChanged;
    bool comingFromDirectionChange;
    bool jumpBuffer;
    bool doubleJump;

    #endregion
    
    

    void Start()
    {
        prevDir = 0;
        rb = GetComponent<Rigidbody2D>();
        dir = Vector2.zero;
        movementTimer = 0f;
        directionHasChanged = false;
        comingFromDirectionChange = false;
        jumpBuffer = false;
        doubleJump = false;

        if(groundCheck == null)
        {
            throw new UnassignedReferenceException();
        }
    }

    void Update()
    {
        if(Input.GetButtonDown("Jump"))
        {
            CancelInvoke();
            EnableJumpBuffer();
            Invoke("DisableJumpBuffer", 0.2f);         
        }

        if(groundCheck.IsTouchingLayers(whatIsGround) && jumpBuffer == true)
        {
            Jump();
            GameObject go = Instantiate(jumpEffect, new Vector3(groundCheck.gameObject.transform.position.x, groundCheck.gameObject.transform.position.y, 0), Quaternion.Euler(-90, 0, 0));
            go.transform.localScale = new Vector3(1, 1, 1);
            Destroy(go, 2f);
            CancelInvoke();
            DisableJumpBuffer();
            doubleJump = false;
        }

        if(!groundCheck.IsTouchingLayers(whatIsGround) && jumpBuffer == true && doubleJump == false)
        {
            Jump();
            CancelInvoke();
            DisableJumpBuffer();
            doubleJump = true;
        }
    }
    
    void FixedUpdate()
    {
        
        //Gets Raw Axis Values : 1, 0, or -1
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        
        //Value evaluated from Movement Curves
        float curveValue = 0;
        

        if(x != 0) 
        {
            if( prevDir == ( (int)x *(-1) ) )
            {
                directionHasChanged = true;
            }
           
            prevDir = (int)x;

            //If there is a change in direction, first come to stop using decay curve, then gain momentum
            if(directionHasChanged)  
            {
                movementTimer -=  Time.fixedDeltaTime;
                movementTimer = Mathf.Clamp(movementTimer, 0f, timeToFullyStop);
                curveValue = movementDecayCurve.Evaluate(movementTimer/timeToFullyStop);

                dir = rb.velocity.x != 0 ?  new Vector2(curveValue * (rb.velocity.x/Mathf.Abs(rb.velocity.x)), y) : new Vector2(0f, y);
               
                if(movementTimer == 0.0f)
                {
                    directionHasChanged = false;
                    comingFromDirectionChange = true;
                    
                    movementTimer +=  Time.fixedDeltaTime ;
                }

            }
            else
            {
                //Just Changed Direction
                if(shouldBolt == true && comingFromDirectionChange == true)
                {   
                    movementTimer +=  Time.fixedDeltaTime * boltmovementSpeed;
                }
                //Started gaining speed from rest
                else
                {
                    
                    movementTimer +=  Time.fixedDeltaTime;
                }


                movementTimer = Mathf.Clamp(movementTimer, 0f, timeToReachFullSpeed);
                curveValue = movementCurve.Evaluate(movementTimer/timeToReachFullSpeed);

                dir = new Vector2(curveValue * x, y);
            }
           
        }

        //Come to rest after a decay
        else
        {
            movementTimer -=  Time.fixedDeltaTime;
            movementTimer = Mathf.Clamp(movementTimer, 0f, timeToFullyStop);
            curveValue = movementDecayCurve.Evaluate(movementTimer/timeToFullyStop);

            if(rb.velocity.sqrMagnitude < 0.1f)
            {
                comingFromDirectionChange = false;
                directionHasChanged = false;
                prevDir = 0;
            }

            dir = rb.velocity.x != 0 ?  new Vector2(curveValue * (rb.velocity.x/Mathf.Abs(rb.velocity.x)), y) : new Vector2(x, y);
        }

        #region BetterJump
        
        if(rb.velocity.y < 0)
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        else if(rb.velocity.y > 0 && Input.GetButton("Jump")!=true)
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        
        #endregion


        if(airControl == true && !groundCheck.IsTouchingLayers(whatIsGround))
        {
           //do nothing
        }
        else
        {
            Walk(dir); 
        }

    }

    private void DisableJumpBuffer()
    {
        jumpBuffer = false; 
    }

    private void EnableJumpBuffer()
    {
        jumpBuffer = true; 
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0f);

        rb.velocity += Vector2.up * jumpForce;

    }

    private void Walk(Vector2 dir)
    {
        rb.velocity = new Vector2(dir.x * movementSpeed , rb.velocity.y);
    }
}
