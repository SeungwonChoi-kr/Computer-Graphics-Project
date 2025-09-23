using UnityEngine;

// 이 스크립트는 CharacterController 컴포넌트가 있어야만 작동합니다.
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private Camera playerCamera;

    // [수정됨] 이동 관련 변수들을 통합하고 단순화했습니다.
    private Vector3 playerVelocity;       // Y축 속도 (중력, 점프)를 관리합니다.
    private Vector3 horizontalVelocity;   // X, Z축 속도 (좌우, 앞뒤)를 관리합니다.

    private bool groundedPlayer;
    private bool wasGroundedLastFrame;

    // [개선됨] 착지 후 부드러운 전환을 위한 변수
    private float landingGracePeriod = 0.2f; // 착지 후 급격한 방향 전환을 막는 시간
    private float landingTimer = 0f;

    private void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
        playerCamera = Camera.main;

        // Start 로직의 다른 부분들은 문제가 없으므로 그대로 둡니다.
        // ... 기존 Start()의 Debug.Log 및 기타 설정 ...
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        // 1. 상태 체크
        wasGroundedLastFrame = groundedPlayer;
        groundedPlayer = controller.isGrounded;

        // 2. 착지 처리
        HandleLanding();

        // 3. 이동 및 점프 입력 처리
        HandleMovementInput();
        HandleJumpInput();
        
        // 4. 중력 적용
        ApplyGravity();

        Debug.Log("최종 수평 속도: " + horizontalVelocity.magnitude);

        // 5. 최종 이동 적용 [가장 중요한 수정!]
        // 수평 이동과 수직 이동을 합쳐서 한 번만 Move()를 호출합니다.
        Vector3 finalMove = (horizontalVelocity + playerVelocity) * Time.deltaTime;
        controller.Move(finalMove);
    }

    // [신규] 착지 시 로직을 처리하는 함수
    private void HandleLanding()
    {
        // 막 착지한 순간이라면
        if (groundedPlayer && !wasGroundedLastFrame)
        {
            // playerVelocity.y를 -2f 정도로 설정하여 바닥에 확실히 붙도록 합니다.
            playerVelocity.y = -2f; 
            landingTimer = landingGracePeriod; // 착지 유예 시간 시작
        }

        if (groundedPlayer)
        {
            // 착지 타이머 감소
            if(landingTimer > 0)
            {
                landingTimer -= Time.deltaTime;
            }
        }
    }

    // [개선됨] 이동 입력을 처리하는 함수
    private void HandleMovementInput()
    {
        // WASD 입력 받기
        Vector3 moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        if (groundedPlayer)
        {
            // 땅에 있을 때: 입력을 받아 이동 방향을 결정합니다.
            if (moveInput.magnitude > 0.1f)
            {
                float cameraYRotation = playerCamera.transform.eulerAngles.y;
                Vector3 targetDirection = Quaternion.Euler(0f, cameraYRotation, 0f) * moveInput;
                
                // [개선됨] 착지 직후 로직
                if(landingTimer > 0)
                {
                    // 착지 유예 시간 동안에는 기존 속도(관성)에서 새로운 방향으로 부드럽게 전환
                    horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetDirection * GameManager.Instance.playerSpeed, 1 - (landingTimer / landingGracePeriod));
                }
                else
                {
                    // 일반적인 이동
                    horizontalVelocity = targetDirection.normalized * GameManager.Instance.playerSpeed;
                }
            }
            else
            {
                // 입력이 없으면 속도를 부드럽게 줄입니다.
                horizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, Time.deltaTime * 10f);
            }
        }
        else
        {
            // 공중에 있을 때: horizontalVelocity를 변경하지 않아 관성이 유지됩니다.
            // (WASD 입력이 무시되는 효과)
        }
    }

    // [개선됨] 점프 입력을 처리하는 함수
    private void HandleJumpInput()
    {
        if (Input.GetButtonDown("Jump") && groundedPlayer)
        {
            // 점프 힘 계산 및 적용
            float jumpVelocity = Mathf.Sqrt(GameManager.Instance.jumpHeight * -2.0f * GameManager.Instance.gravityValue);
            playerVelocity.y = jumpVelocity;
        }
    }
    
    // [신규] 중력을 적용하는 함수
    private void ApplyGravity()
    {
        // 땅에 있고, 아래로 떨어지는 중이 아니라면 중력 누적 방지
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f; // -2f 정도로 살짝 눌러주는 힘을 유지
        }

        // 중력 가속도 적용
        playerVelocity.y += GameManager.Instance.gravityValue * Time.deltaTime;
    }
}
