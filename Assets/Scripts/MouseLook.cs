using UnityEngine;

// 이 스크립트는 마우스 입력으로 카메라를 회전시킵니다.
public class MouseLook : MonoBehaviour
{
    // 플레이어 오브젝트 (Y축 회전용)
    public Transform playerBody;
    
    // X축 회전값 (위아래 시선)
    private float xRotation = 0f;
    
    // 마우스 커서 잠금 상태
    private bool isCursorLocked = true;
    
    // 부드러운 회전을 위한 변수
    private float currentYRotation;
    private float yRotationVelocity;
    
    void Start()
    {
        // 게임 시작 시 마우스 커서를 잠급니다
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    void Update()
    {
        // ESC 키로 마우스 커서 잠금/해제
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isCursorLocked = !isCursorLocked;
            Cursor.lockState = isCursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
        }
        
        // 마우스 커서가 잠겨있을 때만 카메라 회전
        if (isCursorLocked)
        {
            // 마우스 입력 받기 (GameManager에서 감도 가져오기)
            float mouseX = Input.GetAxis("Mouse X") * GameManager.Instance.mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * GameManager.Instance.mouseSensitivity;
            
            // X축 회전 (위아래 시선) - 반대로 적용
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f); // 시선 각도 제한
            
            // 카메라의 X축 회전 적용
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            
            // 플레이어의 Y축 회전 (좌우 시선) - 부드럽게 적용
            if (playerBody != null)
            {
                currentYRotation += mouseX;
                float smoothYRotation = Mathf.SmoothDampAngle(playerBody.eulerAngles.y, currentYRotation, ref yRotationVelocity, GameManager.Instance.rotationSmoothTime);
                playerBody.rotation = Quaternion.Euler(0f, smoothYRotation, 0f);
            }
        }
    }
}
