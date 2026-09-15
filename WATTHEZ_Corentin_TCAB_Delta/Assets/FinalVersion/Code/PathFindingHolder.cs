using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Pathfinding
{
    [ExecuteAlways]
    public class PathfindingHolder : MonoBehaviour
    {
        #region parameter
        [HideInInspector]
        public NativeArray<ulong> _data;

        public int3 _size = new int3(10, 10, 10);

        [HideInInspector]
        public Vector3 editorSize;
        [HideInInspector]
        public Vector3 editorPosition;
        [HideInInspector]
        public int3 _totalSize;
        [SerializeField]
        public float _voxelSize = 1;
        public float _maxDistanceFromWall = 2;
        public LayerMask _collisionMask = ~0;
        public bool _drawGrid;
        private const int MAX_GIZMO_BLOCKS = 200000;
        [System.NonSerialized]
        private bool _loggedGizmoSizeWarning;
        public const int DATASTRUCTSIZE = 64;
        public const int SUBDIVIDEBOX = 4; //64 can be divide in square of 4*4*4
        [SerializeField]
        public int _iterationMaximumPathFinding = 100000;

        [HideInInspector]
        public PathFindingGridBake _generateGridJob;
        [HideInInspector]
        public PathfindingJob _pathFinding;
        [HideInInspector]
        public JobHandle _bakeHandle;
        [HideInInspector]
        public JobHandle _pathHandle;
        [HideInInspector]
        public PathNode _pathNode;

        [HideInInspector]
        public int3[] _path;
        [HideInInspector]
        private int _pathFindingTarget;
        [HideInInspector]
        public bool _pathFindingSuccessful;

        [HideInInspector]
        public bool _startToBake;
        [HideInInspector]
        public bool _baking = false;
        [HideInInspector]
        public bool _seekingForPath = false;
        private Action<int3[], bool> giveBackPath;

        public PathfindingSave _save;

        float timeOfBaking = 0;
        float timeOfPathFinding = 0;
        #endregion


        public void Bake()
        {
            transform.position = editorPosition;
            float3 temp = editorSize/(_voxelSize*SUBDIVIDEBOX);
            _size = (int3)temp;
            Initialize();
            timeOfBaking = -Time.realtimeSinceStartup;
            if (!_bakeHandle.IsCompleted) return;
            if(_generateGridJob == null) _generateGridJob = new PathFindingGridBake() { holder = this};
            _baking = true;
            _generateGridJob.Execute();
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
        public void Initialize()
        {
            _pathHandle.Complete();
            _totalSize = _size * SUBDIVIDEBOX;
            int arraySize = _size.x * _size.y * _size.z;
            if(_data != null && _data.IsCreated)
            {
                _data.Dispose();
            }
            _data = new NativeArray<ulong>(arraySize, Allocator.Persistent);
            if(_save != null)
            if (_save._save != null)
                if(_save._save.Length == arraySize) _data.Reinterpret<long>().CopyFrom(_save._save);
        }
        public void ReleaseData()
        {
            _pathHandle.Complete();
            _data.Dispose();
        }
        private void OnEnable()
        {
            Initialize();
        }
        private void OnDrawGizmosSelected()
        {
            if (!_drawGrid || !_data.IsCreated || Application.isPlaying) return;
            int blockCount = _size.x * _size.y * _size.z;
            if (blockCount > MAX_GIZMO_BLOCKS)
            {
                if (!_loggedGizmoSizeWarning)
                {
                    Debug.LogWarning($"PathfindingHolder '{name}': grid has {blockCount} blocks (limit {MAX_GIZMO_BLOCKS}) - skipping walkable-voxel gizmo draw to avoid freezing the editor. Reduce voxel size or bake area to use Draw Grid.");
                    _loggedGizmoSizeWarning = true;
                }
                return;
            }
            _loggedGizmoSizeWarning = false;
            PathfindingDraw.DrawWalkableVoxels(_data, this);
        }
        private void OnDisable()
        {
            ReleaseData();
        }
        public bool TryFindPath(int3 startPosition, int3 endPosition, Action<int3[], bool> callback)
        {
            if(_seekingForPath) return false;
            LaunchPathfinding(PathfindingUtilities.VectorToInt(startPosition, _totalSize), PathfindingUtilities.VectorToInt(endPosition, _totalSize));
            giveBackPath = callback;
            return true;
        }

        private void LaunchPathfinding(int startPosition, int endPosition)
        {

            timeOfPathFinding = -Time.time;
            if (!_pathHandle.IsCompleted) return;
            createPathFinding(startPosition, endPosition);
            _seekingForPath = true;
            _pathFindingTarget = endPosition;
            _pathHandle = _pathFinding.Schedule<PathfindingJob>();
        }

        private void createPathFinding(int startPosition, int endPosition)
        {
            _pathFinding = new PathfindingJob()
            {
                neighborDirection = new NativeArray<int3>(26, Allocator.Persistent),
                openList = new NativeList<PathNode>(Allocator.Persistent),
                closedBool = new NativeArray<bool>(_size.x * _size.y * _size.z * DATASTRUCTSIZE, Allocator.Persistent),
                gScore = new NativeArray<float>(_size.x * _size.y * _size.z * DATASTRUCTSIZE, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
                parents = new NativeArray<int>(_size.x * _size.y * _size.z * DATASTRUCTSIZE, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
                finalPath = new NativeList<int3>(Allocator.Persistent),
                _data = _data,
                _size = _size,
                _totalSize = _totalSize,
                start = startPosition,
                end = endPosition,
                maxIteration = _iterationMaximumPathFinding
            };
        }

        private void Update()
        {
            UpdateBaking();
            UpdatePath();
        }
        private void UpdatePath()
        {
            if (_seekingForPath)
            {
                if (_pathHandle.IsCompleted)
                {
                    _seekingForPath = false;
                    CompletePathFinding();
                }
            }
        }

        private void CompletePathFinding()
        {
            timeOfPathFinding += Time.time;
            //Debug.Log($"path found in {timeOfPathFinding} seconds");
            
            _pathHandle.Complete();
            _seekingForPath = false;
            _path = new int3[_pathFinding.finalPath.Length];
            _pathFinding.finalPath.AsArray().CopyTo(_path);
            _pathFinding.finalPath.Dispose();
            _pathFinding.openList.Dispose();
            _pathFindingSuccessful = _path.Length == 0
                ? _pathFinding.start == _pathFindingTarget
                : PathfindingUtilities.VectorToInt(_path[0], _totalSize) == _pathFindingTarget;
            giveBackPath?.Invoke(_path, _pathFindingSuccessful);
        }
        private void UpdateBaking()
        {
            if (_startToBake)
            {
                _startToBake = false;
                Bake();
            }
            if (_baking)
            {
                if (_bakeHandle.IsCompleted)
                {
                    CompleteBake();
                }
            }
        }

        private void CompleteBake()
        {
            _bakeHandle.Complete();
            timeOfBaking += Time.realtimeSinceStartup;
            Debug.Log($"baked in {timeOfBaking} seconds");
            _baking = false;
#if UNITY_EDITOR
            SaveBaking();
#endif
        }

#if UNITY_EDITOR
        private void SaveBaking()
        {
            if(_save == null)
            {
                MonoScript monoScript = MonoScript.FromMonoBehaviour(this);
                string path = AssetDatabase.GetAssetPath(monoScript);
                path = path.Remove(path.Length - "/Code/PathFindingHolder.cs".Length);
                if (!AssetDatabase.IsValidFolder(path + "/Save"))
                {
                    AssetDatabase.CreateFolder(path, "Save");
                }
                path += "/Save/PathfindingSave.asset";
                    Debug.Log($"bake saved at {path}");
                PathfindingSave save = ScriptableObject.CreateInstance("PathfindingSave") as PathfindingSave;
                AssetDatabase.CreateAsset(save, path);
                _save = save;
            }
            _save._save = _data.Reinterpret<long>().ToArray();
            EditorUtility.SetDirty(_save);
            AssetDatabase.SaveAssets();
        }
#endif

        public ref NativeArray<ulong> GetData()
        {
            return ref _data;
        }
        public void SetDataID(ulong value, int index)
        {
            if (!_pathHandle.IsCompleted) return;
            _data[index] = value;
        }
    }
}