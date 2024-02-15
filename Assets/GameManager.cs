using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject levelPrefab;
    public float levelSpawnDistance = 85.5f;
    public List<GameObject> levels;
    public float fallSpeed;
    void Start()
    {
        GameObject firstLevel = Instantiate(levelPrefab, new(0f, 0f, 0f), Quaternion.identity);
        GameObject secondLevel = Instantiate(levelPrefab, new(0f,levelSpawnDistance,0f), Quaternion.identity);
        GameObject thirdLevel = Instantiate(levelPrefab, new(0f, levelSpawnDistance*2, 0f), Quaternion.identity);
        levels.Add(firstLevel);
        levels.Add(secondLevel);
        levels.Add(thirdLevel);
    }
    void FixedUpdate()
    {
        foreach (GameObject level in levels)
        {
            level.transform.position -= new Vector3(0f, fallSpeed * Time.fixedDeltaTime,0f);
            if(level.transform.position.y < -levelSpawnDistance)
            {
                level.transform.position = new(0f, levelSpawnDistance * 2, 0f);
            }
        }    
    }
}
