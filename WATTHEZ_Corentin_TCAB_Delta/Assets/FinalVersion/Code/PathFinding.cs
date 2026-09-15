using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Pathfinding
{
    [BurstCompile]
    public struct PathfindingJob : IJob
    {
        [DeallocateOnJobCompletion]
        public NativeArray<int3> neighborDirection;

        public int start;
        public int end;
        public NativeList<PathNode> openList;
        [DeallocateOnJobCompletion]
        public NativeArray<bool> closedBool;
        [DeallocateOnJobCompletion]
        public NativeArray<float> gScore;
        [DeallocateOnJobCompletion]
        public NativeArray<int> parents;

        public NativeList<int3> finalPath;
        public PathNode Path;
        public int maxIteration;
        public int3 _size;
        public int3 _totalSize;
        [ReadOnly]
        public NativeArray<ulong> _data;
        public void Execute()
        {
            SetNeighbor();
            Initialize();
            A_Star(end, out Path);
            ExtractPath(Path);
        }

        public void Initialize()
        {
            float baseDist = math.distance(PathfindingUtilities.IntToVector(start, _totalSize), PathfindingUtilities.IntToVector(end, _totalSize));
            for (int i = 0; i < gScore.Length; i++) gScore[i] = float.MaxValue;
            PathNode StartNode = new PathNode(start, 10, baseDist, baseDist, -1);
            openList.Add(StartNode);
            Path = StartNode;
            gScore[start] = StartNode.cost;
        }


        public void SetNeighbor()
        {
            int3 center = new int3(1, 1, 1);
            for (int i = 0; i < 26; i++)
            {
                int c = i;
                if (i >= 13) c++;
                int3 value = new int3(c / 9, (c / 3) % 3, c % 3) - center;
                neighborDirection[i] = value;
            }
        }


        public void A_Star(int end, out PathNode path)
        {
            path = openList[0];
            int iterationCount = 0;
            while (openList.Length != 0 && iterationCount < maxIteration)
            {
                PathNode current =  MinHeap.Pop(ref openList);
                if (closedBool[current.pos]) continue;
                closedBool[current.pos] = true;
                if (path.estimation > current.estimation) path = current;
                parents[current.pos] = current.parent;
                if (current.pos == end) return;

                AddNeighborToOpen(current);
                iterationCount++;
            }
        }


        public void AddNeighborToOpen(PathNode parent)
        {
            int3 pos = PathfindingUtilities.IntToVector(parent.pos, _totalSize);
            for (int i = 0; i < neighborDirection.Length; i++)
            {
                int3 neighborPosition = pos + neighborDirection[i];
                if (PathfindingUtilities.IsValidCoord(neighborPosition, _totalSize)) AddNodeToOpen(PathfindingUtilities.VectorToInt(neighborPosition, _totalSize), parent);
            }
        }


        public void AddNodeToOpen(int id, PathNode parent)
        {
            if (closedBool[id]) return;
            if (!PathfindingUtilities.IsWalkable(id, ref _data, _size)) return;
            float cost = parent.cost + PathfindingUtilities.Distance(id, parent.pos, _totalSize);
            if (cost >= gScore[id]) return;
            gScore[id] = cost;
            float dist = PathfindingUtilities.Distance(id, end, _totalSize);
            PathNode newNode = new PathNode(id, cost, dist, cost + dist, parent.pos);
            MinHeap.Push(ref openList, newNode);
        }

        
        private void ExtractPath(PathNode path)
        {
            int pos = path.pos;
            int parent = path.parent;
            while (parent != -1)
            {
                finalPath.Add(PathfindingUtilities.IntToVector(pos, _totalSize));
                pos = parent;
                parent = parents[pos];
            }
        }
    }


    [Serializable]
    public struct PathNode
    {

        public int pos;
        public float cost;
        public float estimation;
        public float total;
        public int parent;

        public PathNode(int Pos, float Cost,
        float Estimation,
        float Total,
        int Parent)
        {
            pos = Pos;
            cost = Cost;
            estimation = Estimation;
            total = Total;
            parent = Parent;
        }
    }

    [BurstCompile]
    public class PathfindingUtilities
    {

        static public bool isInside(int3 pos, int3 size)
        {
            bool ans = true;
            if (pos.x < 0 || pos.x >= size.x) ans = false;
            if (pos.y < 0 || pos.y >= size.y) ans = false;
            if (pos.z < 0 || pos.z >= size.z) ans = false;
            return ans;
        }
        static public bool isInside(int pos, int size)
        {
            return (pos >=0 && pos<size);
        }
        static public float Distance(int pos1, int pos2, int3 _size)
        {
            return math.distance(IntToVector(pos1, _size), IntToVector(pos2, _size));
        }
        static public bool IsWalkable(int id, ref NativeArray<ulong> data, int3 _size)
        {
            int3 pos = IntToVector(id, _size * PathfindingHolder.SUBDIVIDEBOX);
            if(!isInside(pos, _size * PathfindingHolder.SUBDIVIDEBOX)) return false;
            ulong ans = data[VectorToInt(pos / PathfindingHolder.SUBDIVIDEBOX, _size)];
            pos = pos % PathfindingHolder.SUBDIVIDEBOX;

            ulong mask = 1ul << VectorToInt(pos, PathfindingHolder.SUBDIVIDEBOX);
            return (ans & mask) != 0;
        }
        static public bool IsValidCoord(int3 pos, int3 size)
        {
            if (pos.x >= size.x || pos.x < 0) return false;
            if (pos.y >= size.y || pos.y < 0) return false;
            if (pos.z >= size.z || pos.z < 0) return false;
            return true;
        }


        static public int3 IntToVector(int pos, int3 dimension)
        {
            return new int3(pos % dimension.x, pos / dimension.x % dimension.y, pos / (dimension.x * dimension.y));
        }
        static public int VectorToInt(int3 pos, int3 dimension)
        {
            return pos.x + pos.y * dimension.x + pos.z * dimension.x * dimension.y;
        }

        static public float3 FindWorldPosition(int3 coordinate, PathfindingHolder holder)
        {

            float3 result = (float3)coordinate * holder._voxelSize;
            result -= holder._voxelSize * 0.5f * (float3)holder._totalSize;
            result += holder._voxelSize * 0.5f;

            return holder.transform.localToWorldMatrix.MultiplyPoint(result);
        }

        static public int3 FindGridPositionFromWorld(float3 coordinate, PathfindingHolder holder)
        {
            float3 localPosition = holder.transform.worldToLocalMatrix.MultiplyPoint(coordinate);
            localPosition /= holder._voxelSize;
            localPosition += holder._totalSize / 2;
            localPosition -= 0.5f;

            int3 position;
            position.x = Mathf.Clamp(Mathf.RoundToInt(localPosition.x), 0, holder._totalSize.x - 1);
            position.y = Mathf.Clamp(Mathf.RoundToInt(localPosition.y), 0, holder._totalSize.y - 1);
            position.z = Mathf.Clamp(Mathf.RoundToInt(localPosition.z), 0, holder._totalSize.z - 1);
            return position;
        }
        static public bool TryFindClosestWalkableGridPosition(Vector3 pos, PathfindingHolder holder, int maxSearchSize, out int3 result)
        {
            int3 gridPos = FindGridPositionFromWorld(pos, holder);
            result = gridPos;
            float dist = float.MaxValue;
            for (int searchSize = 0; searchSize <= maxSearchSize && searchSize <= dist; searchSize++)
                for (int x = -searchSize; x <= searchSize; x++)
                    for (int y = -searchSize; y <= searchSize; y++)
                        for (int z = -searchSize; z <= searchSize; z++)
                        {
                            if (math.max(math.abs(x), math.max(math.abs(y), math.abs(z))) != searchSize) continue;
                            int3 testPos = new int3(x, y, z) + gridPos;
                            if (!isInside(testPos, holder._totalSize)) continue;
                            float testDistance = math.length(new float3(x, y, z));
                            if (testDistance >= dist) continue;
                            if (!IsWalkable(VectorToInt(testPos, holder._totalSize), ref holder.GetData(), holder._size)) continue;

                            result = testPos;
                            dist = testDistance;
                        }
            return dist != float.MaxValue;
        }

    }

    [BurstCompile]
    public static class MinHeap
    {
        public static void Push(ref NativeList<PathNode> heap, in PathNode node)
        {
            int i = heap.Length;
            heap.Add(node);

            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (heap[parent].total <= node.total)
                    break;

                heap[i] = heap[parent];
                i = parent;
            }

            heap[i] = node;
        }

        public static PathNode Pop(ref NativeList<PathNode> heap)
        {
            PathNode root = heap[0];
            int last = heap.Length - 1;
            PathNode node = heap[last];
            heap.RemoveAt(last);

            if (last > 0)
            {
                int i = 0;
                int half = last >> 1;

                while (i < half)
                {
                    int left = (i << 1) + 1;
                    int right = left + 1;

                    int smallest = (right < last && heap[right].total < heap[left].total)
                        ? right
                        : left;

                    if (heap[smallest].total >= node.total)
                        break;

                    heap[i] = heap[smallest];
                    i = smallest;
                }

                heap[i] = node;
            }

            return root;
        }
    }

}
