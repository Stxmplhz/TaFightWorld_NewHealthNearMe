using UnityEngine;

namespace Core
{
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator animator;
    private bool canMove = true;
    private bool isJumping = false;

    // Animation parameter names
    private static readonly int IdleTrigger = Animator.StringToHash("Idle");
    private static readonly int JumpTrigger = Animator.StringToHash("Jump");
    private static readonly int WalkTrigger = Animator.StringToHash("Walk");

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D not found on PlayerController!");
            enabled = false;
            return;
        }

        if (animator == null)
        {
            Debug.LogError("Animator not found on PlayerController!");
            enabled = false;
            return;
        }

        // Start in idle state
        animator.SetTrigger(IdleTrigger);
        Debug.Log("Starting in Idle state");
    }

    public void PerformJump(float forwardForce, float upwardForce)
    {
        if (!canMove || rb == null) return;

        Debug.Log("Starting Jump Animation");
        isJumping = true;  // Set jumping flag first
        animator.ResetTrigger(IdleTrigger);  // Reset other triggers
        animator.ResetTrigger(WalkTrigger);
        animator.SetTrigger(JumpTrigger);  // Set jump trigger
        
        // Apply forces after animation trigger
        rb.AddForce(new Vector2(forwardForce, upwardForce), ForceMode2D.Impulse);
        
        // Log current animation state
        Debug.Log($"Current Animator State: {animator.GetCurrentAnimatorStateInfo(0).nameHash}");
    }

    public void EnableMovement(bool enable)
    {
        canMove = enable;
        if (!enable)
        {
            StopMovement();
        }
    }

    public void StopMovement()
    {
        if (rb != null)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            if (!isJumping)
            {
                animator.ResetTrigger(WalkTrigger);
                animator.ResetTrigger(JumpTrigger);
                animator.SetTrigger(IdleTrigger);
            }
        }
    }

    public void ResetVelocity()
    {
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            if (!isJumping)
            {
                animator.ResetTrigger(WalkTrigger);
                animator.ResetTrigger(JumpTrigger);
                animator.SetTrigger(IdleTrigger);
            }
        }
    }

    private void Update()
    {
        if (!canMove || rb == null) return;

        if (!isJumping)
        {
            float horizontalSpeed = Mathf.Abs(rb.velocity.x);
            if (horizontalSpeed > 0.1f)
            {
                animator.ResetTrigger(IdleTrigger);
                animator.ResetTrigger(JumpTrigger);
                animator.SetTrigger(WalkTrigger);
            }
            else
            {
                animator.ResetTrigger(WalkTrigger);
                animator.ResetTrigger(JumpTrigger);
                animator.SetTrigger(IdleTrigger);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // When landing from a jump
        if (collision.contacts[0].normal.y > 0.7f && isJumping)
        {
            Debug.Log("Landing from jump");
            isJumping = false;
            animator.ResetTrigger(JumpTrigger);
            animator.SetTrigger(IdleTrigger);
        }
    }
}
}