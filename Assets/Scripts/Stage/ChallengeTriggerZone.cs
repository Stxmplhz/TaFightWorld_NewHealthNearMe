using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;
using Core;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class ChallengeTriggerZone : MonoBehaviour
{
    [Header("Pose System")] 
    public PoseGameManager poseGameManager;

    [Header("Challenge Configuration")]
    public ChallengeData challengeData;
    public float characterMoveSpeed = 5f;
    public float forwardJumpForce = 4f;
    public float upwardJumpForce = 4f;
    public bool isStandalone = false;
    public Transform nextChallengePoint;

    [Header("World Space UI")]
    public Canvas worldSpaceCanvas;
    public float uiOffset = 2f;
    public Vector3 uiRotation = Vector3.zero;

    [Header("Events")]
    public UnityEvent onChallengeStart;
    public UnityEvent onChallengeSuccess;
    public UnityEvent onChallengeFail;

    // Public properties
    public bool IsActive => isActive;
    public bool IsCompleted { get; private set; }

    // Private fields
    protected bool isActive = false;
    private bool isFailed = false;
    private StageManager stageManager;
    protected PlayerController playerController;
    protected Vector3 challengeStartPosition;

    protected virtual void Start()
    {
        stageManager = FindAnyObjectByType<StageManager>();
        if (stageManager == null)
        {
            Debug.LogError("StageManager not found!");
            return;
        }

        if (worldSpaceCanvas != null)
        {
            worldSpaceCanvas.gameObject.SetActive(false);
            worldSpaceCanvas.renderMode = RenderMode.WorldSpace;
            worldSpaceCanvas.worldCamera = Camera.main;
        }

        if (challengeData == null)
        {
            Debug.LogError("No ChallengeData assigned!");
            return;
        }

        Debug.Log("Poses to play: " + challengeData.poseRequirements.Count);
        foreach (var pose in challengeData.poseRequirements)
        {
            Debug.Log(" - " + pose.PoseName);
        }

        if (poseGameManager == null)
        {
            poseGameManager = PoseGameManager.Instance;
            if (poseGameManager == null)
            {
                Debug.LogError($"[ChallengeTriggerZone] ❌ PoseGameManager not found in scene for {gameObject.name}");
            }
            else
            {
                Debug.Log($"[ChallengeTriggerZone] ✅ Found PoseGameManager for {gameObject.name}");
            }
        }

    }

    private void Update()
    {
        if (!isActive || playerController == null) return;

        UpdateUIPosition();
    }

    public void StartPoseChallenge()
    {
        if (challengeData == null)
        {
            Debug.LogError("❌ ChallengeData is NULL!");
            return;
        }

        if (challengeData.poseRequirements == null || challengeData.poseRequirements.Count == 0)
        {
            Debug.LogError("❌ No poses assigned in ChallengeData! Did you forget to assign PoseRequirement?");
            return;
        }

        Debug.Log($"➡️ Starting PoseChallenge with {challengeData.poseRequirements.Count} poses");
        foreach (var pose in challengeData.poseRequirements)
        {
            if (pose == null)
            {
                Debug.LogError("❌ Found NULL PoseRequirement in list!");
            }
            else
            {
                Debug.Log($"✅ Pose: {pose.PoseName}, Icon: {(pose.PoseIcon != null ? "OK" : "MISSING")}");
            }
        }

        poseGameManager.PlayPoseExternal(
            challengeData.poseRequirements,
            success =>
            {
                Debug.Log($"➡️ Challenge finished: {(success ? "Success" : "Fail")}");

                CompleteChallenge(success, waitForPlayerRetry: !success);
            });
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log($"[Challenge] Player entered challenge zone at position: {transform.position}, isStandalone: {isStandalone}");

        // Get player controller first
        playerController = other.GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = other.gameObject.AddComponent<PlayerController>();
        }

        // Store start position and stop player movement
        challengeStartPosition = other.transform.position;
        playerController.ResetVelocity();
        playerController.StopMovement();

        // Only remove auto walk if this is not a standalone challenge or if we have a next point
        if (challengeData != null && (!isStandalone || nextChallengePoint != null))
        {
            PlayerAutoWalk autoWalk = other.GetComponent<PlayerAutoWalk>();
            if (autoWalk != null)
            {
                Debug.Log($"[Challenge] Removing auto walk at position: {transform.position}");
                Destroy(autoWalk);
            }
        }

        // Start the challenge
        StartChallenge();
    }

    private void StartChallenge()
    {
        if (stageManager != null && stageManager.IsStageFailed)
        {
            Debug.Log("[ChallengeTriggerZone] Cannot start challenge - stage failed");
            return;
        }

        if (poseGameManager != null)
        {
            poseGameManager.SetCurrentChallengeTriggerZone(this);
        }

        isActive = true;
        isFailed = false;

        Debug.Log($"[Challenge] Starting new challenge - Type: {challengeData.poseRequirements}, Position: {transform.position}");

        if (worldSpaceCanvas != null)
        {
            worldSpaceCanvas.gameObject.SetActive(true);
            UpdateUIPosition();
        }

        // Enable player input for this challenge
        if (playerController != null)
        {
            playerController.EnableMovement(true);
        }

        onChallengeStart.Invoke();  

        StartPoseChallenge();
    }

    protected void UpdateUIPosition()
    {
        if (worldSpaceCanvas != null && playerController != null)
        {
            Vector3 targetPos = playerController.transform.position + Vector3.up * uiOffset;
            worldSpaceCanvas.transform.position = targetPos;
            worldSpaceCanvas.transform.eulerAngles = uiRotation;

            if (Camera.main != null)
            {
                worldSpaceCanvas.transform.forward = Camera.main.transform.forward;
            }
        }
    }

    public void RestartChallenge()
    {
        if (playerController != null)
        {
            playerController.transform.position = challengeStartPosition;
            playerController.ResetVelocity();
            StartChallenge();
        }   
    }

    public virtual void CompleteChallenge(bool success, bool waitForPlayerRetry)
    {
        if (!isActive)
        {
            Debug.Log("[Challenge] Tried to complete challenge but it's not active");
            return;
        }

        isActive = false;
        IsCompleted = success;
        Debug.Log($"[Challenge] Challenge completed - Success: {success}, Position: {transform.position}");

        if (worldSpaceCanvas != null)
        {
            worldSpaceCanvas.gameObject.SetActive(false);
        }

        if (success && challengeData != null)
        {
            // Check if this is a jump challenge
            var poseList = challengeData.poseRequirements;
            if (poseList.Exists(p => p.PoseName.ToLower().Contains("jump")))
            {
                // Perform the jump
                playerController.PerformJump(forwardJumpForce, upwardJumpForce);
            }

            // Add auto walk component and initialize it only if we have somewhere to go
            var player = playerController.gameObject;
            bool shouldAddAutoWalk = !isStandalone || nextChallengePoint != null;
            
            if (shouldAddAutoWalk)
            {
                // Remove existing auto walk if any
                var existingAutoWalk = player.GetComponent<PlayerAutoWalk>();
                if (existingAutoWalk != null)
                {
                    Debug.Log($"[Challenge] Removing existing auto walk before adding new one");
                    Destroy(existingAutoWalk);
                }

                Debug.Log($"[Challenge] Adding auto walk component with speed: {characterMoveSpeed}");
                PlayerAutoWalk autoWalk = player.AddComponent<PlayerAutoWalk>();
                autoWalk.Initialize(characterMoveSpeed);
            }
            else
            {
                Debug.Log($"[Challenge] Skipping auto walk addition for standalone challenge without next point");
            }

            onChallengeSuccess.Invoke();
            
            // Add null check before calling StageManager
            if (stageManager != null)
            {
                Debug.Log($"[Challenge] Notifying StageManager of success");
                stageManager.OnChallengeSuccess(this);
            }
        }
        else
        {
            onChallengeFail.Invoke();
            
            // Add null check before calling StageManager
            if (stageManager != null)
            {
                Debug.Log($"[Challenge] Notifying StageManager of failure");
                stageManager.OnChallengeFail(this, waitForPlayerRetry);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (!isActive || isFailed)
            {
                if (worldSpaceCanvas != null)
                {
                    worldSpaceCanvas.gameObject.SetActive(false);
                }
            }
            
            playerController = null;
        }
    }

    public void OnDrawGizmos()
    {
        if (worldSpaceCanvas != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, worldSpaceCanvas.transform.position);
            Gizmos.DrawWireSphere(worldSpaceCanvas.transform.position, 0.5f);
        }
    }
}