using UnityEngine;
using Pathfinding;

public class FrameSynchronAStar : MonoBehaviour
{
    public Transform target;
    private Seeker _seeker;
    private Path _path;

    void Start()
    {
        _seeker = GetComponent<Seeker>();
        _seeker.pathCallback += OnPathCallback;

        FixVector3 startPos = new FixVector3(transform.position.x, transform.position.y, transform.position.z);
        FixVector3 endPos = new FixVector3(target.position.x, target.position.y, target.position.z);
        _seeker.StartPath(transform.position, target.position);
    }

    void OnDestroy()
    {
        if (_seeker != null)
        {
            _seeker.pathCallback -= OnPathCallback;
        }
    }

    void OnPathCallback(Path path)
    {
        if (!path.error)
        {
            _path = path;
        }
        else
        {
            Debug.LogError("OnPathCallback Error!!!");
        }
    }
}