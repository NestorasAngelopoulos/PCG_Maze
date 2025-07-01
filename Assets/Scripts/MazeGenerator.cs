using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [SerializeField] private Vector3 chunkDimentions = new Vector3(1f, 1f, 1f);

    private List<Vector2Int> generatedChunks = new List<Vector2Int>();

    [SerializeField] private Transform player;

    [SerializeField] private GameObject subMaze;
    
    private void Awake() => GenerateChunk(Vector2Int.zero);
    private void Update() => GenerateAdjacentChunks();

    private void GenerateAdjacentChunks()
    {
        // If the chunk hasn't been generated, generate a new chunk, making sure it is accessible from the player's current chunk
        if (!generatedChunks.Contains(GetChunkCoordFromPosition(player.position + Vector3.forward * chunkDimentions.z))) GenerateChunk(Vector2Int.up);
        if (!generatedChunks.Contains(GetChunkCoordFromPosition(player.position - Vector3.forward * chunkDimentions.z))) GenerateChunk(Vector2Int.down);
        if (!generatedChunks.Contains(GetChunkCoordFromPosition(player.position + Vector3.right * chunkDimentions.x))) GenerateChunk(Vector2Int.right);
        if (!generatedChunks.Contains(GetChunkCoordFromPosition(player.position - Vector3.right * chunkDimentions.x))) GenerateChunk(Vector2Int.left);
    }

    private void GenerateChunk(Vector2Int direction)
    {
        Vector2Int chunkCoords = GetChunkCoordFromPosition(player.position) + direction;
        MazeChunk newChunk = Instantiate(subMaze, new Vector3(chunkCoords.x, 0, chunkCoords.y), Quaternion.identity, transform).GetComponent<MazeChunk>();
        generatedChunks.Add(chunkCoords);
        newChunk.ChooseForm(direction);
    }

    private Vector2Int GetChunkCoordFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / chunkDimentions.x);
        int z = Mathf.FloorToInt(position.z / chunkDimentions.z);
        return new Vector2Int(x, z);
    }
}