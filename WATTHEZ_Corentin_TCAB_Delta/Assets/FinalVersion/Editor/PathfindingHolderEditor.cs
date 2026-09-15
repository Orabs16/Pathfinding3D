using UnityEngine;
using UnityEditor;
using Unity.Mathematics;
using System;

namespace Pathfinding{
    [CustomEditor(typeof(PathfindingHolder))]
    public class PathfindingHolderEditor : Editor
    {
        PathfindingHolder holder;
        Vector3 position;
        Vector3 snappedSize;
        Vector3 size;

        void OnEnable()
        {
            holder = target as PathfindingHolder;
            if (holder.editorSize != Vector3.zero)
            {
                position = holder.editorPosition;
                size = holder.editorSize / 2f;
            }
            else
            {
                position = holder.transform.position;
                size = (float3)holder._size * holder._voxelSize * 2;
            }
            float snap = holder._voxelSize * PathfindingHolder.SUBDIVIDEBOX;
            snappedSize = new Vector3(
                Math.Abs((float)Math.Round(size.x / snap) * snap),
                Math.Abs((float)Math.Round(size.y / snap) * snap),
                Math.Abs((float)Math.Round(size.z / snap) * snap)
            );
        }
        
        

        void OnSceneGUI()
        {

            EditorGUI.BeginChangeCheck();



            Vector3 xMin = Handles.Slider(
                position + Vector3.Scale(size, Vector3.left),
                Vector3.left,
                HandleUtility.GetHandleSize(position) * 0.6f,
                Handles.RectangleHandleCap,
                0
            );

            Vector3 xMax = Handles.Slider(
                position + Vector3.Scale(size, Vector3.right),
                Vector3.right,
                HandleUtility.GetHandleSize(position) * 0.6f,
                Handles.RectangleHandleCap,
                0
            );

            Vector3 yMin = Handles.Slider(
                position + Vector3.Scale(size, Vector3.down),
                Vector3.down,
                HandleUtility.GetHandleSize(position) * 0.6f,
                Handles.RectangleHandleCap,
                0
            );

            Vector3 yMax = Handles.Slider(
                position + Vector3.Scale(size, Vector3.up),
                Vector3.up,
                HandleUtility.GetHandleSize(position) * 0.6f,
                Handles.RectangleHandleCap,
                0
            );

            Vector3 zMin = Handles.Slider(
                position + Vector3.Scale(size, Vector3.back),
                Vector3.back,
                HandleUtility.GetHandleSize(position) * 0.6f,
                Handles.RectangleHandleCap,
                0
            );
            Vector3 zMax = Handles.Slider(
                position + Vector3.Scale(size, Vector3.forward),
                Vector3.forward,
                HandleUtility.GetHandleSize(position) * 0.6f,
                Handles.RectangleHandleCap,
                0

            );
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Move Handle");
                position = new Vector3((xMin.x + xMax.x) /2, (yMin.y + yMax.y) /2, (zMin.z + zMax.z) /2);
                size =  position - new Vector3( xMin.x, yMin.y, zMin.z);
                float snap = holder._voxelSize * PathfindingHolder.SUBDIVIDEBOX;
                snappedSize = new Vector3(
                    Math.Abs((float)Math.Round(size.x/snap)*snap),
                    Math.Abs((float)Math.Round(size.y/snap)*snap),
                    Math.Abs((float)Math.Round(size.z/snap)*snap)
                );
                holder.editorPosition = position;
                holder.editorSize = snappedSize*2;  
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
                Repaint();
            }

            


            Handles.color = new Color(0.2f,0.8f,0.4f,0.35f); 
            Vector3[] corner = new Vector3[8]
            {
                position + new Vector3(snappedSize.x, snappedSize.y, snappedSize.z),
                position + new Vector3(-snappedSize.x, snappedSize.y, snappedSize.z),
                position + new Vector3(snappedSize.x, -snappedSize.y, snappedSize.z),
                position + new Vector3(-snappedSize.x, -snappedSize.y, snappedSize.z),
                position + new Vector3(snappedSize.x, snappedSize.y, -snappedSize.z),
                position + new Vector3(-snappedSize.x, snappedSize.y, -snappedSize.z),
                position + new Vector3(snappedSize.x, -snappedSize.y, -snappedSize.z),
                position + new Vector3(-snappedSize.x, -snappedSize.y, -snappedSize.z)
            };
            Handles.DrawSolidRectangleWithOutline(
                new Vector3[4]
                {
                    corner[0],
                    corner[1],
                    corner[3],
                    corner[2]
                },
                Handles.color,
                Color.clear
            );
            Handles.DrawSolidRectangleWithOutline(
                new Vector3[4]
                {
                    corner[4],
                    corner[5],
                    corner[7],
                    corner[6]
                },
                Handles.color,
                Color.clear
            );
            Handles.DrawSolidRectangleWithOutline(
                new Vector3[4]
                {
                    corner[1],
                    corner[3],
                    corner[7],
                    corner[5]
                },
                Handles.color,
                Color.clear
            );
            Handles.DrawSolidRectangleWithOutline(
                new Vector3[4]
                {
                    corner[0],
                    corner[2],
                    corner[6],
                    corner[4]
                },
                Handles.color,
                Color.clear
            );
            
            Handles.DrawSolidRectangleWithOutline(
                new Vector3[4]
                {
                    corner[0],
                    corner[1],
                    corner[5],
                    corner[4]
                },
                Handles.color,
                Color.clear
            );
            Handles.DrawSolidRectangleWithOutline(
                new Vector3[4]
                {
                    corner[2],
                    corner[3],
                    corner[7],
                    corner[6]
                },
                Handles.color,
                Color.clear
            );

        }
/*
        public override void OnInspectorGUI()
        {
            
        }*/
    }
}


