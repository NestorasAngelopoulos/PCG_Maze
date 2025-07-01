using System.Collections.Generic;
using UnityEngine;

public class PickForm : MonoBehaviour
{
    [SerializeField] private List<GameObject> UpIncoming;
    [SerializeField] private List<GameObject> DownIncoming;
    [SerializeField] private List<GameObject> RightIncoming;
    [SerializeField] private List<GameObject> LeftIncoming;

    public void ChooseForm(int incomingDirection)
    {
        switch (incomingDirection)
        {
            case 0:
                Instantiate(UpIncoming[Random.Range(0, UpIncoming.Count)], transform.position, transform.rotation);
                break;
            case 1:
                Instantiate(DownIncoming[Random.Range(0, DownIncoming.Count)], transform.position, transform.rotation);
                break;
            case 2:
                Instantiate(RightIncoming[Random.Range(0, RightIncoming.Count)], transform.position, transform.rotation);
                break;
            case 3:
                Instantiate(LeftIncoming[Random.Range(0, LeftIncoming.Count)], transform.position, transform.rotation);
                break;
        }
    }
}
