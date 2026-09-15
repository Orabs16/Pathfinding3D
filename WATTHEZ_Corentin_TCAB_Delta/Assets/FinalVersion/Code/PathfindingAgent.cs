using Unity.Mathematics;
using UnityEngine;
namespace Pathfinding{
    public class PathfindingAgent : MonoBehaviour
    {
        public PathfindingHolder holder;
        public Transform Target;
        public int3[] path;
        float timer=0;
        public int maxSearch = 10;
        int pathIndex;
        public float speed = 4;
        Vector3 previousTargetPosition;
        float timeBeforeRefresh = 0.5f;
        bool refreshed = false;
        public float rotationSpeed = 50;
        public bool drawPathFinding;
        private bool _pathFindingSuccessful;

        private void Start()
        {
            PathfindingUtilities.TryFindClosestWalkableGridPosition(transform.position, holder, maxSearch, out int3 start);
            pathIndex = 0;
            path = new int3[] { start };
        }
        void Update()
        {
            refreshPathFinding();
            followPath();
            Orient();
        }

        private void OnDrawGizmos()
        {
            DrawPath();
        }
        private void DrawPath()
        {
            if (!drawPathFinding) return;

            Gizmos.color = _pathFindingSuccessful? Color.green : Color.red;
            PathfindingDraw.DrawPath(path, holder, pathIndex);
        }
        private void refreshPathFinding()
        {
            if (previousTargetPosition == Target.position)
            {
                if (refreshed) return;
                timer -= Time.deltaTime;
                if (timer < 0)
                {
                    timer += timeBeforeRefresh;
                    refreshed = AskForPath();
                }
            }
            else
            {
                refreshed = false;
                previousTargetPosition = Target.position;
                timer = timeBeforeRefresh;
            }

            return;
        }

        public bool AskForPath()
        {
            if (!PathfindingUtilities.TryFindClosestWalkableGridPosition(transform.position, holder, maxSearch, out int3 start)) return false;
            if (!PathfindingUtilities.TryFindClosestWalkableGridPosition(Target.position, holder, maxSearch, out int3 end)) return false;
            if (!holder.TryFindPath(start, end, receiveThePath)) return false;
            pathIndex = 0;
            path = new int3[] { start };
            return true;
        }

        public void receiveThePath(int3[] receivedPath, bool successful)
        {
            path = receivedPath;
            pathIndex = math.max(path.Length - 1, 0);
            _pathFindingSuccessful = successful;
        }

        public void followPath()
        {
            float remainingMove = speed * Time.deltaTime;
            while (remainingMove > 0 && pathIndex != 0)
            {
                Vector3 nextTarget = PathfindingUtilities.FindWorldPosition(path[pathIndex], holder);
                Vector3 direction = nextTarget - transform.position;
                transform.position += direction.normalized * math.min(direction.magnitude, remainingMove);
                remainingMove -= direction.magnitude;
                if (direction.magnitude < holder._voxelSize)
                    if (pathIndex > 0)
                    {
                        pathIndex--;
                    }
            }
        }
        public void Orient()
        {
            if (pathIndex == 0) return;
            Collider[] coll = Physics.OverlapSphere(transform.position, holder._maxDistanceFromWall, holder._collisionMask, QueryTriggerInteraction.Ignore);
            Vector3 closestPoint = Vector3.zero;
            float closestDistance = float.MaxValue;
            foreach (Collider collider in coll)
            {
                if (collider.transform.IsChildOf(transform)) continue;
                Vector3 testPoint = collider.ClosestPoint(transform.position);
                float testDistance = math.distance(transform.position, testPoint);
                if (testDistance < closestDistance)
                {
                    closestDistance = testDistance;
                    closestPoint = testPoint;
                }
            }
            if (closestDistance == float.MaxValue) return;

            Quaternion target = Quaternion.LookRotation( PathfindingUtilities.FindWorldPosition(path[pathIndex], holder) - (float3)transform.position, transform.position - closestPoint);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
    }
}
