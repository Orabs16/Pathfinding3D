using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;


namespace Pathfinding
{
    [BurstCompile]
    public class PathFindingGridBake
    {
        public PathfindingHolder holder;

        public void Execute()
        {
            Main();
        }

        public void Main()
        {
            LoopThroughAllTheChunks();
        }


        private void LoopThroughAllTheChunks()
        {

            for (int lengthLoop = 0; lengthLoop < holder._size.x; lengthLoop++)
            {
                for (int widthLoop = 0; widthLoop < holder._size.y; widthLoop++)
                {
                    for (int heightLoop = 0; heightLoop < holder._size.z; heightLoop++)
                    {
                        int3 pos = new int3(lengthLoop, widthLoop, heightLoop);
                        int index = pos.x + pos.y * holder._size.x + pos.z * holder._size.x * holder._size.y;
                        holder.SetDataID(DetectCollision(pos), index);
                    }
                }
            }
        }
        private ulong DetectCollision(int3 position)
        {
            ulong result = 0;
            for (int i = 0; i < PathfindingHolder.DATASTRUCTSIZE; i++)
            {
                int3 localPosition = position * PathfindingHolder.SUBDIVIDEBOX
                                     + new int3(i % PathfindingHolder.SUBDIVIDEBOX, 
                                               (i / PathfindingHolder.SUBDIVIDEBOX) % PathfindingHolder.SUBDIVIDEBOX,
                                                i / (PathfindingHolder.SUBDIVIDEBOX * PathfindingHolder.SUBDIVIDEBOX));

                float3 worldPosition = PathfindingUtilities.FindWorldPosition(localPosition, holder);
                // not too far from something
                bool notTooFar = Physics.CheckBox(worldPosition, Vector3.one * holder._maxDistanceFromWall / 2, Quaternion.identity, holder._collisionMask, QueryTriggerInteraction.Ignore);
                if(!notTooFar) continue;
                // not inside of something
                bool notInside = !Physics.CheckBox(worldPosition, Vector3.one * holder._voxelSize / 2, Quaternion.identity, holder._collisionMask, QueryTriggerInteraction.Ignore);
                if(notInside) result |= 1ul << i;
            }
            return result;
        }
    }
}
