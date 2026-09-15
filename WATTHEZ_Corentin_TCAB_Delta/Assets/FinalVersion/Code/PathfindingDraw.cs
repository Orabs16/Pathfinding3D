using Pathfinding;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class PathfindingDraw
{
    public static void DrawWalkableVoxels(NativeArray<ulong> data, PathfindingHolder grid)
    {
        if (!data.IsCreated) return;
        for (int j = 0; j < data.Length; j++)
        {
            ulong box = data[j];
            int3 position = new int3(j % grid._size.x, (j / grid._size.x) % grid._size.y, j / (grid._size.x * grid._size.y));
            for (int i = 0; i < PathfindingHolder.DATASTRUCTSIZE; i++)
            {
                int3 localPosition = position * PathfindingHolder.SUBDIVIDEBOX
                                     + new int3(i % PathfindingHolder.SUBDIVIDEBOX,
                                               (i / PathfindingHolder.SUBDIVIDEBOX) % PathfindingHolder.SUBDIVIDEBOX,
                                                i / (PathfindingHolder.SUBDIVIDEBOX * PathfindingHolder.SUBDIVIDEBOX));
                if (box % 2 == 1) Gizmos.DrawWireCube(PathfindingUtilities.FindWorldPosition(localPosition, grid), grid._voxelSize * new float3(1, 1, 1));

                box >>= 1;
            }
        }
    }


    static public void DrawPath(int3[] path, PathfindingHolder holder, int indexOffset)
    {
        for(int i = indexOffset; i > 0; i--)
        {
            int3 coord = path[i];
            Gizmos.DrawCube(PathfindingUtilities.FindWorldPosition(coord, holder), holder._voxelSize * new float3(1, 1, 1));
        }
    }
}
