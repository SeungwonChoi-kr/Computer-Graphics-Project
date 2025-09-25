using UnityEngine;

// 게임의 모든 설정을 한 곳에서 관리하는 싱글톤 클래스
public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static GameManager Instance { get; private set; }
    
    [Header("플레이어 설정")]
    [Tooltip("플레이어 이동 속도")]
    public float playerSpeed = 2.0f;
    
    [Tooltip("점프 높이")]
    public float jumpHeight = 1.0f;
    
    [Tooltip("중력 값")]
    public float gravityValue = -9.81f;
    
    [Tooltip("공중에서의 이동 제어력 (현재 비활성화됨 - 점프 가속도 벡터 보존)")]
    [Range(0.0f, 1.0f)]
    public float airControl = 0.0f;
    
    [Header("카메라 설정")]
    [Tooltip("마우스 감도")]
    [Range(0.1f, 5.0f)]
    public float mouseSensitivity = 0.7f;
    
    [Tooltip("카메라 회전 부드러움")]
    [Range(0.01f, 0.5f)]
    public float rotationSmoothTime = 0.1f;
    
    [Header("게임 시간 설정")]
    [Tooltip("게임 시간 배율 (1.0 = 정상, 2.0 = 2배 빠름)")]
    [Range(0.1f, 5.0f)]
    public float timeScale = 1.0f;
    
    [Header("UI 설정")]
    [Tooltip("UI 애니메이션 속도")]
    [Range(0.1f, 3.0f)]
    public float uiAnimationSpeed = 1.0f;
    
    [Tooltip("UI 페이드 인/아웃 시간")]
    [Range(0.1f, 2.0f)]
    public float uiFadeTime = 0.5f;
    
    [Tooltip("UI 스케일")]
    [Range(0.5f, 2.0f)]
    public float uiScale = 1.0f;
    
    [Header("오디오 설정")]
    [Tooltip("마스터 볼륨")]
    [Range(0.0f, 1.0f)]
    public float masterVolume = 1.0f;
    
    [Tooltip("음악 볼륨")]
    [Range(0.0f, 1.0f)]
    public float musicVolume = 0.8f;
    
    [Tooltip("효과음 볼륨")]
    [Range(0.0f, 1.0f)]
    public float sfxVolume = 0.9f;
    
    [Tooltip("음성 볼륨")]
    [Range(0.0f, 1.0f)]
    public float voiceVolume = 1.0f;
    
    [Tooltip("음소거")]
    public bool mute = false;
    
    [Header("디버그 설정")]
    [Tooltip("디버그 모드 활성화")]
    public bool debugMode = false;
    
    void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        // 시간 배율 적용
        Time.timeScale = timeScale;
        
        // 오디오 볼륨 적용
        ApplyAudioSettings();
    }
    
    // 오디오 설정을 실제로 적용하는 메서드
    private void ApplyAudioSettings()
    {
        if (mute)
        {
            AudioListener.volume = 0f;
        }
        else
        {
            AudioListener.volume = masterVolume;
        }
    }
    
    // 설정값을 런타임에서 변경할 수 있는 메서드들
    public void SetMouseSensitivity(float value)
    {
        mouseSensitivity = Mathf.Clamp(value, 0.1f, 5.0f);
    }
    
    public void SetPlayerSpeed(float value)
    {
        playerSpeed = Mathf.Clamp(value, 0.1f, 10.0f);
    }
    
    public void SetTimeScale(float value)
    {
        timeScale = Mathf.Clamp(value, 0.1f, 5.0f);
    }
    
    // UI 설정 메서드들
    public void SetUIAnimationSpeed(float value)
    {
        uiAnimationSpeed = Mathf.Clamp(value, 0.1f, 3.0f);
    }
    
    public void SetUIScale(float value)
    {
        uiScale = Mathf.Clamp(value, 0.5f, 2.0f);
    }
    
    // 오디오 설정 메서드들
    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
    }
    
    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
    }
    
    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
    }
    
    public void SetVoiceVolume(float value)
    {
        voiceVolume = Mathf.Clamp01(value);
    }
    
    public void ToggleMute()
    {
        mute = !mute;
    }
    
    // 설정을 기본값으로 리셋
    public void ResetToDefaults()
    {
        playerSpeed = 2.0f;
        jumpHeight = 1.0f;
        gravityValue = -9.81f;
        mouseSensitivity = 0.7f;
        rotationSmoothTime = 0.1f;
        timeScale = 1.0f;
        uiAnimationSpeed = 1.0f;
        uiFadeTime = 0.5f;
        uiScale = 1.0f;
        masterVolume = 1.0f;
        musicVolume = 0.8f;
        sfxVolume = 0.9f;
        voiceVolume = 1.0f;
        mute = false;
        debugMode = false;
    }
}
