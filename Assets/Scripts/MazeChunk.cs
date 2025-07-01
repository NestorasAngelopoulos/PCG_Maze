using System.Collections.Generic;
using UnityEngine;

public class MazeChunk : MonoBehaviour
{
    [SerializeField] private GameObject startChunk;
    [SerializeField] private List<GameObject> UpEntrance;
    [SerializeField] private List<GameObject> DownEntrance;
    [SerializeField] private List<GameObject> RightEntrance;
    [SerializeField] private List<GameObject> LeftEntrance;

    public void ChooseForm(Vector2Int incomingDirection)
    {
        // Instantiate a random chunk that is accessible from the incoming direction
        if (incomingDirection == Vector2Int.up) Instantiate(DownEntrance[Random.Range(0, DownEntrance.Count)], transform.position, transform.rotation, transform);
        else if (incomingDirection == Vector2Int.down) Instantiate(UpEntrance[Random.Range(0, UpEntrance.Count)], transform.position, transform.rotation, transform);
        else if (incomingDirection == Vector2Int.right) Instantiate(LeftEntrance[Random.Range(0, LeftEntrance.Count)], transform.position, transform.rotation, transform);
        else if (incomingDirection == Vector2Int.left) Instantiate(RightEntrance[Random.Range(0, RightEntrance.Count)], transform.position, transform.rotation, transform);
        else Instantiate(startChunk, transform.position, transform.rotation, transform);
    }
}
