using com.github.NestorasAngelopoulos;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine;
using UnityEditor;
using System.Data;

[CustomEditor(typeof(LevelGenerator))]
public class LevelGeneratorInspector : Editor
{
    LevelGenerator instance;
    
    void OnEnable() => instance = (LevelGenerator)target;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("seedInfo"), GUIContent.none);
        
        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        // Regenerate toggle
        if (instance.regenerate) GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
        else GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
        if (GUILayout.Button("Regenerate")) instance.regenerate = !instance.regenerate;
        GUI.backgroundColor = Color.white;
        // Buttons
        if (GUILayout.Button("Generate")) instance.GeneratePath();
        if (GUILayout.Button("Clear")) instance.ClearLevel();
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }
}

[CustomPropertyDrawer(typeof(LevelGenerator.SeedInfo))]
public class SeedInfoDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        SerializedProperty toggle = property.FindPropertyRelative("useSeed");

        // Label
        Rect labelRect = position;
        labelRect.width = 82;
        EditorGUI.LabelField(labelRect, new GUIContent(toggle.displayName));

        // Toggle
        Rect toggleRect = position;
        toggleRect.width = 10;
        toggleRect.x = position.x + labelRect.width - (toggleRect.width * 2);
        EditorGUI.PropertyField(toggleRect, toggle, GUIContent.none);

        // Seed
        Rect seedRect = position;
        seedRect.width -= labelRect.width;
        seedRect.x = position.x + position.width - seedRect.width;
        EditorGUI.PropertyField(seedRect, property.FindPropertyRelative("seed"), GUIContent.none);

        EditorGUI.EndProperty();
    }
}

public class LevelGenerator : MonoBehaviour
{
    [HideInInspector] public bool regenerate;
    [System.Serializable] public class SeedInfo
    {
        public bool useSeed;
        public int seed;
    }
    [HideInInspector] public SeedInfo seedInfo;

    [Tooltip("The types of flooring that can spawn.")] [SerializeField] private WeightedList<GameObject> tiles;
    [Tooltip("The dimentions of a single tile (used to calculate offset).")] [SerializeField] private Vector3 tileDimentions = new Vector3(1f, 0.1f, 1f);
    [Tooltip("The width of the level (in tiles).")] [SerializeField] private int gridWidth = 10;
    [Tooltip("The length of the level (in tiles).")] [SerializeField] private int gridLength = 10;

    private GameObject[] generatedTiles;

    [Tooltip("A list of props to spawn at random, and their respective spawn rates.")] [SerializeField] private WeightedList<GameObject> props = new WeightedList<GameObject>();
    [Tooltip("The maximum deviation from the center of a tile that a prop can spawn with.")] [Range(0f, 0.5f)] [SerializeField] private float propMaxOffset = 0.4f;

    [Tooltip("The minimum number of rooms that a generated level can contain.")] [SerializeField] private int minimumPathLength = 20;
    private List<int> visitedRooms = new List<int>();

    private void OnValidate()
    {
        // Limit grid size to positives
        if (gridWidth < 0) gridWidth = 0;
        if (gridLength < 0) gridLength = 0;

        tiles.BalanceWeights();
        props.BalanceWeights();

        // Regenerate level on value changed in inspector, and disable option when playmode starts
        if (regenerate)
        {
            if (Time.time != 0) EditorApplication.delayCall += () => GeneratePath();
            else regenerate = false;
        }
    }

    private void Awake() => GeneratePath();

    public void ClearLevel() // Clear all children from object
    {
        while (transform.childCount > 0) DestroyImmediate(transform.GetChild(0).gameObject);
    }
    private void GenerateLevel()
    {
        ClearLevel();

        // Create new root GameObject
        Transform root = new GameObject("Root").transform;
        root.transform.parent = transform;

        GameObject[] generatedTiles = new GameObject[gridWidth * gridLength];

        for (int z = 0; z < gridLength; z++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                GameObject tileToPlace = tiles[1].item;
                int tileIndex = (z * gridWidth) + x;

                // If tile is inside path, make it a room
                if (visitedRooms.Contains(tileIndex))
                {
                    tileToPlace = tiles[0].item;
                }

                // Error if no tiles are provided
                if (tiles.Count == 0)
                {
                    Debug.LogError("No tiles to generate.");
                    return;
                }

                // Generate grid of tiles
                Vector3 tilePosition = new Vector3(x * tileDimentions.x + tileDimentions.x / 2, 0f, z * tileDimentions.z + tileDimentions.z / 2);
                generatedTiles[tileIndex] = Instantiate(tileToPlace, tilePosition, Quaternion.identity, root);

                // If tile was a room, clear the walls between adjacent rooms
                if (visitedRooms.Contains(tileIndex))
                {
                    CleanRoomWalls(tileIndex, generatedTiles[tileIndex]);

                    // Instantiate a random prop with offset as child of last tile
                    GameObject propToSpawn = props.RandomItemOrDefault();
                    if (propToSpawn != null)
                    {
                        GameObject spawnedProp = Instantiate(propToSpawn, tilePosition + Vector3.up * tileDimentions.y + Vector3.right * Random.Range(0f, propMaxOffset) + Vector3.forward * Random.Range(0f, propMaxOffset), Quaternion.identity);
                        spawnedProp.transform.SetParent(generatedTiles[tileIndex].transform, true);
                    }
                }
            }
        }

        // Add NavMesh
        NavMeshSurface navMesh = root.AddComponent<NavMeshSurface>();
        navMesh.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        navMesh.collectObjects = CollectObjects.Children;
        navMesh.BuildNavMesh();
    }

    public void GeneratePath()
    {
        // Use set seed or generate a random one
        if (!seedInfo.useSeed) seedInfo.seed = Random.Range(0, int.MaxValue);
        Random.InitState(seedInfo.seed);

        if (gridWidth * gridLength < minimumPathLength) throw new InvalidConstraintException($"Path length is larger than the size of the level.");

        int roomsVisited = 0;
        do
        {
            visitedRooms.Clear();
            roomsVisited = 0;
            int currentIndex = Random.Range(0, gridWidth * gridLength);

            for (int i = 0; i < minimumPathLength * 4; i++)
            {
                int nextPointOnPath = FindNextTile(currentIndex);

                if (nextPointOnPath != -1)
                {
                    if (!visitedRooms.Contains(nextPointOnPath))
                    {
                        visitedRooms.Add(nextPointOnPath);
                        currentIndex = nextPointOnPath;
                        roomsVisited++;
                    }
                }
            }
        } while (roomsVisited < minimumPathLength);

        GenerateLevel();
    }

    private int FindNextTile(int incomingIndex, int direction = -1)
    {
        if (direction == -1) direction = Random.Range(0, 4);
        int nextIndex = -1;

        switch (direction)
        {
            case 0: // North
                if (incomingIndex < gridWidth * (gridLength - 1)) nextIndex = incomingIndex + gridWidth;
                break;
            case 1: // East
                if (incomingIndex % gridWidth != gridWidth - 1) nextIndex = incomingIndex + 1;
                break;
            case 2: // South
                if (incomingIndex >= gridWidth) nextIndex = incomingIndex - gridWidth;
                break;
            case 3: // West
                if (incomingIndex % gridWidth != 0) nextIndex = incomingIndex - 1;
                break;
        }

        return nextIndex;
    }

    private void CleanRoomWalls(int tileIndex, GameObject room)
    {
        for (int i = 0; i < 4; i++) if (visitedRooms.Contains(FindNextTile(tileIndex, i))) room.transform.GetChild(i).gameObject.SetActive(false);
    }
}