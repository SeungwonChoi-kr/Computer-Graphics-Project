using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [System.Serializable]
    public class ResourceData { public string name; public GameObject prefab; }

    [System.Serializable]
    public class SourceMine { public string resourceName; public Transform mineLocation; }

    [Header("플레이어 설정")]
    public Transform playerTransform;
    public float detectionRadius = 20f;
    public float checkInterval = 0.5f;

    // ★★★ 추가된 부분: 탐지기 업그레이드와 연동될 '발견 확률' ★★★
    [Header("탐지기 설정")]
    [Tooltip("스폰 포인트를 발견할 기본 확률 (0.0 = 0%, 1.0 = 100%)")]
    [Range(0f, 1f)]
    public float globalDiscoveryChance = 0.7f; // 기본 70% 확률

    [Header("자원 정보")]
    public List<ResourceData> resourceList;
    public List<SourceMine> sourceMines;

    [Header("스폰 포인트 생성 설정")]
    public GameObject spawnPointPrefab;
    public List<Terrain> allTerrains;
    public int numberOfSpawns = 200;
    public float depthBelowSurface = 1.0f;
    public float minDistanceBetweenSpawns = 5.0f;
    public float minAltitude = 10.0f;

    private List<ResourceSpawnPoint> allSpawnPoints = new List<ResourceSpawnPoint>();
    private Transform spawnPointParent;
    private float timeSinceLastCheck = 0f;

    private void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
    }

    private void Update()
    {
        timeSinceLastCheck += Time.deltaTime;
        if (timeSinceLastCheck >= checkInterval)
        {
            UpdateNearbySpawnPoints();
            timeSinceLastCheck = 0f;
        }
    }

    // ★★★ 변경된 부분: 이제 스폰 포인트에게 근접 여부만 알려주는 역할 ★★★
    private void UpdateNearbySpawnPoints()
    {
        if (playerTransform == null) return;

        foreach (var sp in allSpawnPoints)
        {
            if (sp != null)
            {
                float distance = Vector3.Distance(playerTransform.position, sp.transform.position);
                bool isPlayerNear = distance <= detectionRadius;

                // 각 스폰 포인트에게 플레이어가 근처에 있는지 없는지 신호를 보냅니다.
                // 모든 상태 판단과 로직은 이제 스폰 포인트가 알아서 처리합니다.
                sp.SetProximity(isPlayerNear);
            }
        }
    }

    // 이 아래 함수들은 이전 버전과 동일합니다.
    #region Unchanged Methods
    public void GenerateSpawnPoints()
    {
        if (spawnPointParent != null) Destroy(spawnPointParent.gameObject);
        spawnPointParent = new GameObject("Spawn Points").transform;
        allSpawnPoints.Clear();
        List<Vector3> placedPositions = new List<Vector3>();
        for (int i = 0; i < numberOfSpawns; i++)
        {
            int attempts = 0;
            while (attempts < 30)
            {
                attempts++;
                Terrain randomTerrain = allTerrains[Random.Range(0, allTerrains.Count)];
                Vector3 terrainPos = randomTerrain.transform.position;
                Vector3 terrainSize = randomTerrain.terrainData.size;
                float randomX = Random.Range(terrainPos.x, terrainPos.x + terrainSize.x);
                float randomZ = Random.Range(terrainPos.z, terrainPos.z + terrainSize.z);
                float terrainHeight = randomTerrain.SampleHeight(new Vector3(randomX, 0, randomZ));
                if (terrainHeight < minAltitude) continue;
                Vector3 potentialPosition = new Vector3(randomX, terrainHeight - depthBelowSurface, randomZ);
                bool isTooClose = placedPositions.Any(pos => Vector3.Distance(potentialPosition, pos) < minDistanceBetweenSpawns);
                if (!isTooClose)
                {
                    placedPositions.Add(potentialPosition);
                    GameObject newSpawnObj = Instantiate(spawnPointPrefab, potentialPosition, Quaternion.identity, spawnPointParent);
                    allSpawnPoints.Add(newSpawnObj.GetComponent<ResourceSpawnPoint>());
                    break;
                }
            }
        }
        Debug.Log(placedPositions.Count + "개의 스폰 포인트를 생성했습니다.");
    }
    public void RespawnAllPointsForNewDay()
    {
        Debug.Log("새로운 날이 되어 모든 자원 스폰 포인트를 재배치합니다.");
        GenerateSpawnPoints();
    }
    public GameObject GetResourceToSpawn(Vector3 spawnPosition)
    {
        Dictionary<string, float> weights = new Dictionary<string, float>();
        float totalWeight = 0f;
        foreach (var mine in sourceMines)
        {
            float distance = Vector3.Distance(spawnPosition, mine.mineLocation.position);
            float weight = 1f / (distance + 0.001f);
            weights[mine.resourceName] = weight;
            totalWeight += weight;
        }
        float randomValue = Random.Range(0, totalWeight);
        foreach (var mine in sourceMines)
        {
            if (randomValue < weights[mine.resourceName])
            {
                return resourceList.Find(r => r.name == mine.resourceName)?.prefab;
            }
            randomValue -= weights[mine.resourceName];
        }
        return null;
    }
    #endregion
}
