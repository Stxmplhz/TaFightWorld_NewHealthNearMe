using UnityEngine;
using Core;
using System.Collections;
using UnityEngine.UI;
using TMPro; // เพิ่ม namespace สำหรับ TextMeshPro

[RequireComponent(typeof(DogChaseStageController))]
[RequireComponent(typeof(BoxCollider2D))]  // เพิ่ม BoxCollider2D
public class DogChaseChallenge : ChallengeTriggerZone
{
    [Header("Dog Chase Settings")]
    public KeyCode successKey = KeyCode.W;  // ปุ่มสำหรับ success
    public KeyCode failKey = KeyCode.S;     // ปุ่มสำหรับ fail
    public float dogStartDistance = 5f;     // ระยะห่างเริ่มต้นระหว่างหมากับผู้เล่น
    public float dogMinDistance = 1f;       // ระยะห่างน้อยที่สุดที่หมาจะเข้าใกล้ผู้เล่น
    public float dogSpeed = 2f;             // ความเร็วในการเคลื่อนที่ของหมา

    [Header("UI References")]
    public GameObject attemptTextObject;  // GameObject ที่มี component Text หรือ TextMeshProUGUI
    public GameObject failIndicator; // GameObject สำหรับแสดงว่า fail (optional)

    private Text legacyText;              // สำหรับ Unity UI Text
    private TextMeshProUGUI tmpText;      // สำหรับ TextMeshPro
    private DogChaseStageController stageController;
    private DogChaseGameManager gameManager;
    private BoxCollider2D triggerCollider;

    protected override void Start()
    {
        stageController = GetComponent<DogChaseStageController>();
        gameManager = FindFirstObjectByType<DogChaseGameManager>();

        // Setup collider
        triggerCollider = GetComponent<BoxCollider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector2(2f, 4f);
        }

        if (gameManager == null)
        {
            Debug.LogError("DogChaseGameManager not found!");
            return;
        }

        // Setup canvas
        if (worldSpaceCanvas != null)
        {
            worldSpaceCanvas.gameObject.SetActive(false);
            worldSpaceCanvas.renderMode = RenderMode.WorldSpace;
            worldSpaceCanvas.worldCamera = Camera.main;
        }

        // ซ่อน fail indicator
        if (failIndicator != null)
        {
            failIndicator.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log($"[DogChase] Player entered challenge zone at position: {transform.position}");

        // Get player controller
        playerController = other.GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = other.gameObject.AddComponent<PlayerController>();
        }

        // Store start position and stop player movement
        challengeStartPosition = other.transform.position;
        playerController.ResetVelocity();
        playerController.StopMovement();

        // Start the challenge
        StartChallenge();
    }

    private void StartChallenge()
    {
        if (isActive) return; // ป้องกันการเริ่ม challenge ซ้ำ

        isActive = true;
        Debug.Log($"[DogChase] Starting challenge at position: {transform.position}, Stage Type: {stageController.stageType}");

        if (poseGameManager != null)
        {
            poseGameManager.SetCurrentChallengeTriggerZone(this);
        }

        if (challengeData == null)
        {
            Debug.LogError("[DogChase] ❌ No ChallengeData assigned!");
            return;
        }

        if (worldSpaceCanvas != null)
        {
            worldSpaceCanvas.gameObject.SetActive(true);
            UpdateUIPosition();
        }

        // ซ่อน fail indicator ถ้ามี
        if (failIndicator != null)
        {
            failIndicator.SetActive(false);
        }

        // Enable player input
        if (playerController != null)
        {
            playerController.EnableMovement(true);
        }

        // Start stage
        if (stageController != null)
        {
            stageController.StartDogChase();
        }

        StartCoroutine(DelayStartPoseChallenge());
    }

    private void Update()
    {
        if (!IsActive || playerController == null || stageController.IsTransitioning) return;

        UpdateUIPosition();
    }

    public override void CompleteChallenge(bool success, bool waitForPlayerRetry)
    {
        if (!IsActive || stageController.IsTransitioning) return;

        isActive = false;
        Debug.Log($"[DogChase] Challenge completed - Success: {success}, Position: {transform.position}, Stage Type: {stageController.stageType}");

        if (success)
        {
            // ถ้าสำเร็จให้ไปต่อ
            if (worldSpaceCanvas != null)
            {
                worldSpaceCanvas.gameObject.SetActive(false);
            }
            stageController.CompleteDogChase(true);
        }
        else
        {
            // แจ้ง game manager ว่า fail
            if (gameManager != null)
            {
                gameManager.OnChallengeFail();
            }

            // แสดง fail indicator ถ้ามี
            if (failIndicator != null)
            {
                failIndicator.SetActive(true);
                StartCoroutine(HideFailIndicator());
            }

            // Reset เฉพาะตำแหน่งหมา
            stageController.ResetDogPosition();

            // ถ้ายังไม่ครบจำนวนครั้ง ให้เริ่ม challenge ใหม่
            if (gameManager != null && gameManager.CanRetryChallenge())
            {
                Debug.Log("[DogChase] Fail but still can retry! Waiting for retry button...");
                // **รอให้ผู้เล่นกด Retry แทนจะ Restart ทันที**
            }
        }
    }

    private IEnumerator HideFailIndicator()
    {
        yield return new WaitForSeconds(1f); // แสดง fail indicator 1 วินาที
        if (failIndicator != null)
        {
            failIndicator.SetActive(false);
        }
    }
    
    private IEnumerator DelayStartPoseChallenge()
    {
        yield return new WaitForSeconds(2f);

        StartPoseChallenge();
    }

} 