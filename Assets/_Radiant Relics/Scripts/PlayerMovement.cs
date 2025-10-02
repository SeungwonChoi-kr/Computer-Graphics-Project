using UnityEngine;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private Camera playerCamera;

    // 이동 관련 변수들을 통합하고 단순화했습니다.
    private Vector3 playerVelocity;       // Y축 속도 (중력, 점프)를 관리합니다.
    private Vector3 horizontalVelocity;   // X, Z축 속도 (좌우, 앞뒤)를 관리합니다.

    private bool groundedPlayer;
    private bool wasGroundedLastFrame;

    // 착지 후 부드러운 전환을 위한 변수
    private float landingGracePeriod = 0.2f; // 착지 후 급격한 방향 전환을 막는 시간
    private float landingTimer = 0f;
    
    // 달리기 관련 변수
    [Header("달리기 설정")]
    [Tooltip("달리기 기능 활성화 여부")]
    public bool enableRun = true;
    
    private bool isRunning = false;
    
    // 물 관련 변수
    [Header("물 설정")]
    [Tooltip("물 레이어 번호")]
    public int waterLayer = 4; // "Water" 레이어
    
    private bool isInWater = false;
    
    // 시작 위치 설정 변수
    [Header("시작 위치 설정")]
    [Tooltip("시작 위치를 사용할지 여부")]
    public bool useCustomStartPosition = true;

    private void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
        playerCamera = Camera.main;

        // 시작 위치 설정
        if (useCustomStartPosition)
        {
            // 현재 Transform의 X, Z 위치를 사용하고 Y만 터레인 높이에 맞춰 보정
            Vector2 currentXZ = new Vector2(transform.position.x, transform.position.z);
            float heightOffset = GameManager.Instance.playerTerrainHeightOffset;
            
            // X, Z는 현재 위치 그대로, Y만 터레인 높이에 맞춰 자동 계산
            Vector3 spawnPosition = GetTerrainAdjustedPosition(currentXZ, heightOffset);
            SetPlayerPosition(spawnPosition);
        }
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        // 1. 상태 체크
        wasGroundedLastFrame = groundedPlayer;
        groundedPlayer = controller.isGrounded;

        // 2. 착지 처리
        HandleLanding();

        // 3. 물 감지 및 이동 입력 처리
        CheckWaterStatus();
        HandleMovementInput();
        HandleRunInput();
        HandleJumpInput();
        
        // 4. 중력 적용
        ApplyGravity();

        // 달리기 상태 및 물 상태 포함 디버그 로그
        //string speedInfo = isRunning ? " (달리기)" : " (걷기)";
        //string waterInfo = isInWater ? " [물속]" : "";
        //Debug.Log("최종 수평 속도: " + horizontalVelocity.magnitude + speedInfo + waterInfo);

        // 5. 최종 이동 적용
        // 수평 이동과 수직 이동을 합쳐서 한 번만 Move()를 호출합니다.
        Vector3 finalMove = (horizontalVelocity + playerVelocity) * Time.deltaTime;
        controller.Move(finalMove);
    }

    // 착지 시 로직을 처리하는 함수
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

    // 이동 입력을 처리하는 함수
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
                
                // 달리기 속도 적용
                float currentSpeed = GameManager.Instance.playerSpeed;
                if (isRunning)
                {
                    currentSpeed *= GameManager.Instance.runSpeedMultiplier;
                }
                
                // 물 속도 감소 적용
                if (isInWater)
                {
                    currentSpeed *= GameManager.Instance.waterSpeedMultiplier;
                }
                
                // 바닥 마찰력을 적용한 부드러운 방향 전환
                Vector3 targetVelocity = targetDirection.normalized * currentSpeed;
                float friction = GameManager.Instance.groundFriction * Time.deltaTime;
                horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, friction);
            }
            else
            {
                // 입력이 없으면 바닥 마찰력으로 속도를 부드럽게 줄입니다.
                float friction = GameManager.Instance.groundFriction * Time.deltaTime;
                horizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, friction);
            }
        }
        else
        {
            // 공중에 있을 때: 약간의 공중 제어력으로 방향 전환 가능하지만 관성 유지
            if (moveInput.magnitude > 0.1f)
            {
                float cameraYRotation = playerCamera.transform.eulerAngles.y;
                Vector3 targetDirection = Quaternion.Euler(0f, cameraYRotation, 0f) * moveInput;
                Vector3 targetVelocity = targetDirection.normalized * GameManager.Instance.playerSpeed;
                
                // 공중에서는 약한 제어력으로 부드럽게 방향 전환 (관성 유지)
                float controlValue = GameManager.Instance.playerAirControl * Time.deltaTime;
                horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, controlValue);
            }
            else
            {
                // 공중에서 입력이 없으면 약간의 마찰력으로 속도 감소 (완전히 멈추지는 않음)
                float dragValue = GameManager.Instance.playerAirDrag * Time.deltaTime;
                horizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, dragValue);
            }
        }
    }

    // 물 상태를 확인하는 함수 (보이지 않는 육면체 영역 기반)
    private void CheckWaterStatus()
    {
        // 보이지 않는 물 영역 오브젝트들을 찾아서 감지
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        isInWater = false;
        
        foreach (GameObject obj in allObjects)
        {
            // 물 영역 오브젝트인지 확인 (이름에 "waterzone" 포함)
            if (obj.name.ToLower().Contains("waterzone"))
            {
                // Collider가 있는지 확인
                Collider collider = obj.GetComponent<Collider>();
                if (collider != null)
                {
                    // 플레이어가 물 영역 안에 있는지 확인
                    if (collider.bounds.Contains(transform.position))
                    {
                        isInWater = true;
                        // Debug.Log($"물 영역 감지됨: {obj.name}");
                        break;
                    }
                }
            }
        }
    }
    
    // 물 영역에 들어갔을 때 호출 (Collider 기반)
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name.ToLower().Contains("waterzone"))
        {
            isInWater = true;
            // Debug.Log($"물 영역에 들어감: {other.gameObject.name}");
        }
    }
    
    // 물 영역에서 나왔을 때 호출 (Collider 기반)
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name.ToLower().Contains("waterzone"))
        {
            isInWater = false;
            // Debug.Log($"물 영역에서 나옴: {other.gameObject.name}");
        }
    }
    
    // 달리기 입력을 처리하는 함수
    private void HandleRunInput()
    {
        if (!enableRun) return;
        
        // Shift + W (앞으로) 조합만 달리기 가능
        bool isPressingW = Input.GetAxisRaw("Vertical") > 0.1f;
        bool isPressingShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        
        isRunning = isPressingW && isPressingShift && groundedPlayer;
    }
    
    // 점프 입력을 처리하는 함수
    private void HandleJumpInput()
    {
        if (Input.GetButtonDown("Jump") && groundedPlayer)
        {
            // 점프 힘 계산 및 적용
            float jumpVelocity = Mathf.Sqrt(GameManager.Instance.jumpHeight * -2.0f * GameManager.Instance.gravityValue);
            playerVelocity.y = jumpVelocity;
        }
    }
    
    // 중력을 적용하는 함수
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
    
    // 플레이어 위치를 설정하는 메서드
    public void SetPlayerPosition(Vector3 newPosition)
    {
        // CharacterController를 비활성화하여 위치 변경 가능하게 함
        controller.enabled = false;
        transform.position = newPosition;
        controller.enabled = true;
        
        // 속도 초기화
        playerVelocity = Vector3.zero;
        horizontalVelocity = Vector3.zero;
        
        Debug.Log($"플레이어 위치가 {newPosition}로 설정되었습니다.");
    }
    
    // 터레인 높이를 고려한 위치 계산
    private Vector3 GetTerrainAdjustedPosition(Vector2 xzPosition, float heightOffset = -1f)
    {
        float offset = heightOffset >= 0 ? heightOffset : GameManager.Instance.playerTerrainHeightOffset;
        float terrainHeight = GetTerrainHeight(xzPosition.x, xzPosition.y);
        return new Vector3(xzPosition.x, terrainHeight + offset, xzPosition.y);
    }
    
    // 특정 X, Z 좌표에서의 터레인 높이를 가져오는 메서드
    private float GetTerrainHeight(float x, float z)
    {
        // 씬의 모든 터레인을 찾아서 높이를 계산
        Terrain[] terrains = FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        
        if (terrains.Length == 0)
        {
            Debug.LogWarning("씬에 터레인이 없습니다. 기본 높이 0을 사용합니다.");
            return 0f;
        }
        
        float maxHeight = float.MinValue;
        
        foreach (Terrain terrain in terrains)
        {
            if (terrain.terrainData != null)
            {
                // 터레인 로컬 좌표로 변환
                Vector3 terrainPos = terrain.transform.position;
                float localX = x - terrainPos.x;
                float localZ = z - terrainPos.z;
                
                // 터레인 데이터의 높이맵 좌표로 변환
                int heightmapWidth = terrain.terrainData.heightmapResolution;
                int heightmapHeight = terrain.terrainData.heightmapResolution;
                
                // 터레인 범위 내에 있는지 확인
                if (localX >= 0 && localX < terrain.terrainData.size.x && 
                    localZ >= 0 && localZ < terrain.terrainData.size.z)
                {
                    float normalizedX = localX / terrain.terrainData.size.x;
                    float normalizedZ = localZ / terrain.terrainData.size.z;
                    
                    // 높이맵에서 높이 가져오기
                    float height = terrain.terrainData.GetHeight(
                        Mathf.RoundToInt(normalizedX * heightmapWidth),
                        Mathf.RoundToInt(normalizedZ * heightmapHeight)
                    );
                    
                    // 터레인의 월드 Y 위치를 고려
                    float worldHeight = height + terrainPos.y;
                    maxHeight = Mathf.Max(maxHeight, worldHeight);
                }
            }
        }
        
        // 터레인을 찾지 못한 경우 기본 높이 반환
        if (maxHeight == float.MinValue)
        {
            Debug.LogWarning($"좌표 ({x}, {z})에서 터레인 높이를 찾을 수 없습니다. 기본 높이 0을 사용합니다.");
            return 0f;
        }
        
        return maxHeight;
    }
    
    // 현재 위치를 터레인 높이에 맞춰 보정하는 메서드
    public void AdjustToTerrainHeight()
    {
        Vector2 currentXZ = new Vector2(transform.position.x, transform.position.z);
        Vector3 adjustedPosition = GetTerrainAdjustedPosition(currentXZ);
        SetPlayerPosition(adjustedPosition);
    }
}
