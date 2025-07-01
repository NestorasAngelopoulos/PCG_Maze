using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [SerializeField] private Vector3 chunkDimentions = new Vector3(1f, 0.1f, 1f);

    private List<Vector2Int> generatedChunks = new List<Vector2Int>();

    [SerializeField] private Vector3 playerPos;

    [SerializeField] private List<GameObject> chunks;

    private void Update() => GenerateChunks();

    private void Awake()
    {
        Vector2Int newChunkCoords = GetChunkCoordFromPosition(playerPos);
        PickForm newChunk = Instantiate(chunks[Random.Range(0, chunks.Count)], new Vector3(newChunkCoords.x, 0, newChunkCoords.y), Quaternion.identity).GetComponent<PickForm>();
        generatedChunks.Add(newChunkCoords);
    }

    private void GenerateChunks()
    {
        Vector2Int currentCoord = GetChunkCoordFromPosition(playerPos);
        Vector2Int newChunkCoords = new();

        // Up
        if (!generatedChunks.Contains(GetChunkCoordFromPosition(playerPos + Vector3.forward * chunkDimentions.z)))
        {
            newChunkCoords = currentCoord + new Vector2Int(0, 1);
            PickForm newChunk = Instantiate(chunks[Random.Range(0, chunks.Count)], new Vector3(newChunkCoords.x, 0, newChunkCoords.y), Quaternion.identity).GetComponent<PickForm>();
            generatedChunks.Add(newChunkCoords);
            newChunk.ChooseForm(0);
        }

        // Down
        if (!generatedChunks.Contains(GetChunkCoordFromPosition(playerPos - Vector3.forward * chunkDimentions.z)))
        {
            newChunkCoords = currentCoord + new Vector2Int(0, -1);
            PickForm newChunk = Instantiate(chunks[Random.Range(0, chunks.Count)], new Vector3(newChunkCoords.x, 0, newChunkCoords.y), Quaternion.identity).GetComponent<PickForm>();
            generatedChunks.Add(newChunkCoords);
            newChunk.ChooseForm(1);
        }

        // Right
        if (!generatedChunks.Contains(GetChunkCoordFromPosition(playerPos + Vector3.right * chunkDimentions.x)))
        {
            newChunkCoords = currentCoord + new Vector2Int(1, 0);
            PickForm newChunk = Instantiate(chunks[Random.Range(0, chunks.Count)], new Vector3(newChunkCoords.x, 0, newChunkCoords.y), Quaternion.identity).GetComponent<PickForm>();
            generatedChunks.Add(newChunkCoords);
            newChunk.ChooseForm(2);
        }
        
        // Left
        if (!generatedChunks.Contains(GetChunkCoordFromPosition(playerPos - Vector3.right * chunkDimentions.x)))
        {
            newChunkCoords = currentCoord + new Vector2Int(-1, 0);
            PickForm newChunk = Instantiate(chunks[Random.Range(0, chunks.Count)], new Vector3(newChunkCoords.x, 0, newChunkCoords.y), Quaternion.identity).GetComponent<PickForm>();
            generatedChunks.Add(newChunkCoords);
            newChunk.ChooseForm(3);
        }
    }

    private Vector2Int GetChunkCoordFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / chunkDimentions.x);
        int z = Mathf.FloorToInt(position.z / chunkDimentions.z);
        return new Vector2Int(x, z);
    }
}