using UnityEngine;

public class ResourceSpawnPoint : MonoBehaviour
{
    // ★★★ 변경된 부분: 새로운 상태 정의 ★★★
    public enum SpawnPointState
    {
        Undiscovered, // 발견되지 않은 초기 상태 (빨간색)
        Discovered,   // 플레이어가 근처에 와서 성공적으로 발견한 상태 (초록색)
        Standby,      // 발견 후 플레이어가 멀어져서 대기 중인 상태 (노란색)
        Missed,       // 발견 시도에 실패하여 하루 동안 비활성화된 상태 (검은색)
        Depleted      // 채굴이 완료되어 고갈된 상태 (검은색)
    }

    [Header("상태 (런타임 확인용)")]
    [SerializeField]
    private SpawnPointState currentState = SpawnPointState.Undiscovered;
    private GameObject spawnedResource = null;

    // ResourceManager가 이 스폰 포인트의 근접 여부를 알려주는 함수
    public void SetProximity(bool isPlayerNear)
    {
        // 플레이어가 근처에 있을 때의 로직
        if (isPlayerNear)
        {
            // 1. 처음 발견을 시도할 때 (상태가 Undiscovered일 때)
            if (currentState == SpawnPointState.Undiscovered)
            {
                AttemptDiscovery();
            }
            // 2. 이미 발견했다가 멀어져서 대기(Standby) 중일 때
            else if (currentState == SpawnPointState.Standby)
            {
                currentState = SpawnPointState.Discovered;
            }
        }
        // 플레이어가 멀어졌을 때의 로직
        else
        {
            // 발견된(Discovered) 상태였다면 대기(Standby) 상태로 변경
            if (currentState == SpawnPointState.Discovered)
            {
                currentState = SpawnPointState.Standby;
            }
        }
    }

    // '발견'을 시도하는 핵심 함수
    private void AttemptDiscovery()
    {
        // ResourceManager로부터 현재 발견 확률을 가져옴
        float currentDiscoveryChance = ResourceManager.Instance.globalDiscoveryChance;

        if (Random.value < currentDiscoveryChance)
        {
            // 발견 성공!
            currentState = SpawnPointState.Discovered;
            // 자원을 실제로 생성합니다.
            GameObject resourceToSpawn = ResourceManager.Instance.GetResourceToSpawn(transform.position);
            if (resourceToSpawn != null)
            {
                spawnedResource = Instantiate(resourceToSpawn, transform.position, Quaternion.identity, transform);
            }
        }
        else
        {
            // 발견 실패!
            currentState = SpawnPointState.Missed;
        }
    }

    // 플레이어가 채굴을 완료했을 때 호출하는 함수
    public void OnMined()
    {
        currentState = SpawnPointState.Depleted;
        spawnedResource = null;
    }

    // 다음 날이 되어 모든 상태를 초기화하는 함수
    public void ResetSpawnPoint()
    {
        if (spawnedResource != null)
        {
            Destroy(spawnedResource);
            spawnedResource = null;
        }
        currentState = SpawnPointState.Undiscovered;
    }

    // ★★★ 변경된 부분: 새로운 기즈모 색상 규칙 적용 ★★★
    private void OnDrawGizmos()
    {
        switch (currentState)
        {
            // 초기 상태 (플레이어가 멀리 있을 때)
            case SpawnPointState.Undiscovered:
                Gizmos.color = Color.red;
                break;

            // 성공적으로 발견하여 플레이어가 근처에 있는 상태
            case SpawnPointState.Discovered:
                Gizmos.color = Color.green;
                break;

            // 발견 후 플레이어가 멀어져서 대기 중인 상태
            case SpawnPointState.Standby:
                Gizmos.color = Color.yellow;
                break;

            // 발견에 실패했거나, 채굴이 완료된 상태
            case SpawnPointState.Missed:
            case SpawnPointState.Depleted:
                Gizmos.color = Color.black;
                break;
        }
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}
