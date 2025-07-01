using UnityEngine;
using System.Collections.Generic;

/// <summary>
///  Manages infinite world generation using a chunk-based system.
///  Loads and unloads chunks based on player position to maintain performance.
/// </summary>
public class ChunkManager : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] Transform player;
    [SerializeField] ChunkPool chunkPool;
    [SerializeField] ObjectPool objectPool;

    [Header("Settings")]
    [SerializeField] int chunkSize = 10;
    [SerializeField] int viewDistance = 3;
    [SerializeField] int minObjectsPerChunk = 1;
    [SerializeField] int maxObjectsPerChunk = 4;

    /// <summary>
    /// Internal class to represent a chunk and its contents.
    /// Keeps track of the chunk GameObject and all objects spawned within it.
    /// </summary>
    private class LoadedArea
    {
        public GameObject chunkObject;
        public List<GameObject> spawnedObjects = new List<GameObject> ();
    }

    // Dictionary to track all currently loaded chunks by their grid coordinates.
    private Dictionary<Vector2Int, LoadedArea> loadedChunks = new Dictionary<Vector2Int, LoadedArea> ();

    // Cache the last player chunk position to avoid unecessary updates.
    private Vector2Int lastPlayerChunk;

    private void Start()
    {
        UpdateChunks();
    }

    private void Update()
    {
        Vector2Int currentChunk = GetChunkCoordFromPosition(player.position);

        // Only update chunks if player moved to a different chunk.
        if (currentChunk != lastPlayerChunk)
        {
            lastPlayerChunk = currentChunk;
            UpdateChunks();
        }
    }

    /// <summary>
    /// Core chunk management logic. Loads new chunks and unloads distant ones.
    /// This method handles the entire chunk lifecycle.
    /// </summary>
    private void UpdateChunks()
    {
        Vector2Int currentCoord = GetChunkCoordFromPosition(player.position);
        List<Vector2Int> neededChunks = new List<Vector2Int>();

        // Phase 1: Determine which chunks should be loaded and load any missing ones.
        for(int x = -viewDistance; x <= viewDistance; x++)
        {
            for(int z =  -viewDistance; z <= viewDistance; z++)
            {
                // Calculate chunk coordinate relative to player.
                Vector2Int coord = new Vector2Int(currentCoord.x + x, currentCoord.y + z);
                neededChunks.Add(coord);

                // If this chunk isn't loaded, create it.
                if (!loadedChunks.ContainsKey(coord))
                {
                    // Calculate world position for this chunk.
                    Vector3 chunkPosition = new Vector3(coord.x * chunkSize, 0.0f, coord.y * chunkSize);

                    // Get chunk GameObject from pool and position it.
                    GameObject chunkGO = chunkPool.Get();
                    chunkGO.transform.position = chunkPosition;
                    chunkGO.name = "Chunk_" + coord.x + "_" + coord.y;

                    // Create chunk data structure.
                    LoadedArea chunk = new LoadedArea();
                    chunk.chunkObject = chunkGO;

                    // Spawn random number of objects within this chunk.
                    int spawnCount = Random.Range(minObjectsPerChunk, maxObjectsPerChunk + 1);
                    for (int i = 0; i < spawnCount; i++)
                    {
                        GameObject obj = objectPool.Get();

                        // Generate random position within chunk bounds.
                        float offSetX = Random.Range(0.0f, (float)chunkSize);
                        float offSetZ = Random.Range(0.0f, (float) chunkSize);

                        // Position object within chunk.
                        Vector3 position = new Vector3(chunkPosition.x + offSetX, 0.0f, chunkPosition.z +  offSetZ);
                        obj.transform.position = position;
                        obj.SetActive(true);

                        // Track this object as part of the chunk.
                        chunk.spawnedObjects.Add(obj);
                    }

                    // Register the new chunk.
                    loadedChunks.Add(coord, chunk);
                }
            }
        }

        // Phase 2: Unload chunks that are no longer needed.
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();

        // Check each loaded chunk to see if it's still needed.
        foreach (KeyValuePair<Vector2Int, LoadedArea> entry in loadedChunks)
        {
            if(!neededChunks.Contains(entry.Key))
            {
                // Return all objects in this chunk to their pool.
                foreach(GameObject obj in entry.Value.spawnedObjects)
                {
                    objectPool.ReturnToPool(obj);
                }
                // Return the chunk itself to the pool.
                chunkPool.ReturnToPool(entry.Value.chunkObject);

                // Mark for removal from loaded chunks.
                chunksToRemove.Add(entry.Key);
            }
        }

        // Remove unloaded chunks from the dictionary.
        foreach(Vector2Int coord in chunksToRemove)
        {
            loadedChunks.Remove(coord);
        }
    }

    /// <summary>
    /// Convert a world position to chunk grid coordinates.
    /// Uses floor division to ensure consistent chunk boundaries.
    /// </summary>
    /// <param name="position">World position to convert</param>
    /// <returns>Chunk coordinates as Vector2Int.</returns>
    private Vector2Int GetChunkCoordFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / chunkSize);
        int z = Mathf.FloorToInt(position.z / chunkSize);
        return new Vector2Int(x, z);
    }
}
// Have a Jägermeister shot each time the word "chunk" comes up. Hf. ;)
