using UnityEngine;

// 이 스크립트는 CharacterController 컴포넌트가 있어야만 작동합니다.
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // private은 Inspector 창에 노출되지 않는 변수입니다.
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    private Camera playerCamera;
    
    // 땅 감지를 위한 변수들
    public float groundCheckDistance = 0.1f;
    public LayerMask groundMask = 1; // Default layer
    
    // 공중 제어를 위한 변수들
    private Vector3 lastMoveDirection;
    private bool wasGroundedLastFrame;
    private Vector3 horizontalVelocity; // 수평 속도 보존용
    
    // 점프 시점의 가속도 벡터 고정용
    private Vector3 jumpTimeMomentum; // 점프할 때의 수평 가속도 벡터
    private bool hasJumped = false; // 점프했는지 여부
    
    // 착지 시 가속도 보존용
    private Vector3 landingMomentum; // 착지할 때의 수평 가속도
    private bool hasLanded = false; // 착지했는지 여부
    private float landingTransitionTimer = 0f; // 착지 후 전환 타이머

    // GameSettings에서 값을 가져오므로 public 변수 제거

    // Start는 게임 시작 시 한 번만 호출됩니다.
    private void Start()
    {
        // 스크립트가 붙어있는 게임오브젝트에서 CharacterController 컴포넌트를 찾아옵니다.
        controller = gameObject.GetComponent<CharacterController>();
        // 카메라 컴포넌트를 찾습니다.
        playerCamera = Camera.main;
    }

    // Update는 매 프레임마다 호출됩니다.
    void Update()
    {
        // 이전 프레임의 땅 상태 저장 (CheckGrounded 이전에)
        wasGroundedLastFrame = groundedPlayer;
        
        // 캐릭터가 땅에 닿아있는지 확인합니다. (Raycast 사용)
        CheckGrounded();
        
        // 땅에 착지했을 때 Y 속도 리셋
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }
        

        // 키보드의 수평(A, D, ←, →)과 수직(W, S, ↑, ↓) 입력을 받습니다.
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        // 이동 처리 (간단하고 확실한 방법)
        Vector3 moveDirection = Vector3.zero;
        
        // 땅에 있을 때 - 완전한 제어
        if (groundedPlayer)
        {
            // 착지했을 때 점프 상태 리셋 및 가속도 보존
            if (!wasGroundedLastFrame)
            {
                hasJumped = false;
                // 착지할 때의 수평 가속도를 저장
                landingMomentum = horizontalVelocity;
                hasLanded = true;
                landingTransitionTimer = 0f;
            }
            
            if (move != Vector3.zero)
            {
                // 카메라의 Y축 회전만 고려하여 이동 방향을 계산합니다.
                float cameraYRotation = playerCamera.transform.eulerAngles.y;
                
                // Y축 회전만을 고려한 수평면 방향 벡터들
                Vector3 forward = new Vector3(
                    Mathf.Sin(cameraYRotation * Mathf.Deg2Rad), 
                    0f, 
                    Mathf.Cos(cameraYRotation * Mathf.Deg2Rad)
                );
                Vector3 right = new Vector3(
                    Mathf.Sin((cameraYRotation + 90f) * Mathf.Deg2Rad), 
                    0f, 
                    Mathf.Cos((cameraYRotation + 90f) * Mathf.Deg2Rad)
                );
                
                // 카메라 방향을 기준으로 이동 벡터를 계산합니다.
                moveDirection = forward * move.z + right * move.x;
                lastMoveDirection = moveDirection;
                
                // 착지 후 가속도 전환 처리
                if (hasLanded && landingTransitionTimer < 0.5f)
                {
                    landingTransitionTimer += Time.deltaTime;
                    // 착지 가속도와 새로운 방향을 부드럽게 혼합
                    Vector3 newVelocity = lastMoveDirection * GameManager.Instance.playerSpeed;
                    float blendFactor = Mathf.Clamp01(landingTransitionTimer / 0.5f);
                    horizontalVelocity = Vector3.Lerp(landingMomentum, newVelocity, blendFactor);
                }
                else
                {
                    // 일반적인 땅 이동
                    horizontalVelocity = lastMoveDirection * GameManager.Instance.playerSpeed;
                    hasLanded = false;
                }
            }
            else
            {
                // 입력이 없을 때는 착지 가속도를 점진적으로 감소
                if (hasLanded && landingTransitionTimer < 0.5f)
                {
                    landingTransitionTimer += Time.deltaTime;
                    horizontalVelocity = Vector3.Lerp(landingMomentum, Vector3.zero, Mathf.Clamp01(landingTransitionTimer / 0.5f));
                }
                else
                {
                    horizontalVelocity = Vector3.zero;
                    // 착지 상태를 리셋하여 다음 이동이 정상 작동하도록 함
                    hasLanded = false;
                }
            }
        }
        // 공중에 있을 때 - 점프 시점의 가속도 벡터 보존
        else
        {
            if (hasJumped)
            {
                // 점프 시점의 가속도 벡터를 그대로 유지 (WASD 입력 무시)
                horizontalVelocity = jumpTimeMomentum;
            }
            else
            {
                // 점프하지 않은 상태에서 공중에 있을 때는 가속도를 점진적으로 감소
                horizontalVelocity *= 0.98f;
            }
        }
        
        // 수평 이동 적용
        if (horizontalVelocity != Vector3.zero)
        {
            controller.Move(horizontalVelocity * Time.deltaTime);
        }

        // 점프 로직 (스페이스바) - GameManager에서 값 가져오기
        if (Input.GetButtonDown("Jump") && groundedPlayer)
        {
            float jumpForce = Mathf.Sqrt(GameManager.Instance.jumpHeight * -3.0f * GameManager.Instance.gravityValue);
            playerVelocity.y += jumpForce;
            
            // 점프할 때의 현재 수평 가속도 벡터를 저장
            jumpTimeMomentum = horizontalVelocity;
            hasJumped = true;
        }

        // 중력을 적용합니다. (GameManager에서 값 가져오기)
        playerVelocity.y += GameManager.Instance.gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }
    
    // 땅 감지 메서드 (Raycast 사용)
    private void CheckGrounded()
    {
        // CharacterController의 바닥에서 약간 아래로 Raycast
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        RaycastHit hit;
        
        // 아래쪽으로 Raycast
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, groundCheckDistance + 0.1f, groundMask))
        {
            groundedPlayer = true;
        }
        else
        {
            groundedPlayer = false;
        }
        
        // 디버그용 Ray 그리기 (Scene 뷰에서 확인 가능)
        Debug.DrawRay(rayOrigin, Vector3.down * (groundCheckDistance + 0.1f), groundedPlayer ? Color.green : Color.red);
    }
}