using UnityEngine;


namespace Pathfinding
{
    public class PathfindingSave : ScriptableObject
    {
        [SerializeField]
        public long[] _save; //long and not ulong because unity can't serialize it and it won't be saved
    }
}
