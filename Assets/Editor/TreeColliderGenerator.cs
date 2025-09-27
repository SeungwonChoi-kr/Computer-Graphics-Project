using UnityEngine;
using UnityEditor;

public class TreeColliderGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Tree Colliders for ALL Terrains")]
    public static void GenerateTreeColliders()
    {
        // 씬에 있는 모든 터레인을 찾습니다.
        Terrain[] allTerrains = FindObjectsOfType<Terrain>(true);
        if (allTerrains.Length == 0)
        {
            Debug.LogError("No terrains found in the scene.");
            return;
        }

        // 기존에 생성된 콜라이더가 있다면 삭제
        GameObject existingColliders = GameObject.Find("Tree Colliders");
        if (existingColliders != null)
        {
            DestroyImmediate(existingColliders);
        }

        // 콜라이더들을 담을 부모 오브젝트 생성
        GameObject colliderParent = new GameObject("Tree Colliders");

        // 찾은 모든 터레인에 대해 반복 실행
        foreach (Terrain terrain in allTerrains)
        {
            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainSize = terrainData.size;

            foreach (TreeInstance tree in terrainData.treeInstances)
            {
                GameObject treePrefab = terrainData.treePrototypes[tree.prototypeIndex].prefab;

                // 프리팹의 이름이 "Tree"로 시작하는 경우에만 콜라이더를 생성
                if (treePrefab.name.StartsWith("Tree"))
                {
                    if (treePrefab.GetComponent<Collider>() == null)
                    {
                        GameObject treeColliderObject = new GameObject(treePrefab.name + "_Collider");
                        treeColliderObject.transform.SetParent(colliderParent.transform);

                        CapsuleCollider collider = treeColliderObject.AddComponent<CapsuleCollider>();

                        // 나무의 월드 위치를 정확히 계산
                        Vector3 treeWorldPosition = new Vector3(
                            tree.position.x * terrainSize.x,
                            tree.position.y * terrainSize.y,
                            tree.position.z * terrainSize.z
                        ) + terrain.transform.position; // 각 터레인의 위치를 더해줌

                        treeColliderObject.transform.position = treeWorldPosition;

                        // 나무 스케일에 맞게 콜라이더 크기 조절
                        collider.height = tree.heightScale * 10;
                        collider.radius = tree.widthScale * 1.35f;
                        collider.center = new Vector3(0, collider.height / 2, 0);
                    }
                }
            }
        }

        Debug.Log(allTerrains.Length + " terrain(s) processed. Tree colliders generated successfully for trees only!");
    }
}
