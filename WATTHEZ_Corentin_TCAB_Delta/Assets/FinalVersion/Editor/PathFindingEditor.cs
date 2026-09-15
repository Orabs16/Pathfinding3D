using System;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
#endif

namespace Pathfinding
{
    public class PathfindingEditor : EditorWindow
    {
        const string WindowTitle = "PathFinding";

        float boxSize = 1;
        float detectionSize = 2;
        LayerMask collisionMask = ~0;
        PathfindingHolder _pathfindingHolder;

#if UNITY_EDITOR_WIN
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        private static void MakeTopMost()
        {
            IntPtr handle = FindWindow(null, WindowTitle);
            if (handle == IntPtr.Zero) return;
            SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }
#endif

        [MenuItem("Window/PathFinding")]
        public static void ShowWindow()
        {
            EditorWindow window = GetWindow<PathfindingEditor>(utility: true, title: WindowTitle, focus: true);
            window.name = WindowTitle;
#if UNITY_EDITOR_WIN
            EditorApplication.delayCall += MakeTopMost;
#endif
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            RefreshHolder();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnFocus()
        {
#if UNITY_EDITOR_WIN
            MakeTopMost();
#endif
        }

        private void OnSelectionChanged()
        {
            RefreshHolder();
            Repaint();
        }

        private void RefreshHolder()
        {
            PathfindingHolder selected = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<PathfindingHolder>()
                : null;

            _pathfindingHolder = selected;
            if (_pathfindingHolder == null) return;
            boxSize = _pathfindingHolder._voxelSize;
            detectionSize = _pathfindingHolder._maxDistanceFromWall;
            collisionMask = _pathfindingHolder._collisionMask;
        }


        private void OnGUI()
        {
            if (_pathfindingHolder == null)
            {
                DrawCreateGridWindow();
                return;
            }
            DrawBakeWindow();
        }



        private void DrawBakeWindow()
        {

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("voxel size", GUILayout.MaxWidth(120));
            boxSize = EditorGUILayout.FloatField(boxSize, GUILayout.MaxWidth(180));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("max distance from the wall", GUILayout.MaxWidth(120));
            detectionSize = EditorGUILayout.FloatField(detectionSize, GUILayout.MaxWidth(180));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("collision mask", GUILayout.MaxWidth(120));
            int concatenatedMask = UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(collisionMask);
            concatenatedMask = EditorGUILayout.MaskField(concatenatedMask, UnityEditorInternal.InternalEditorUtility.layers, GUILayout.MaxWidth(180));
            collisionMask = UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(concatenatedMask);
            EditorGUILayout.EndHorizontal();


            if (GUILayout.Button("Bake"))
            {
                if (_pathfindingHolder._size.x * _pathfindingHolder._size.y * _pathfindingHolder._size.z > 100000) PopupAlert.ShowAlert(waitForBake);
                else
                    Bake();
            }

            if (GUILayout.Button("select"))
            {
                Selection.objects = new UnityEngine.Object[] { _pathfindingHolder.gameObject };
            }
            /*

            EditorGUILayout.Space(120);


            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("int", GUILayout.MaxWidth(120));
            int temp2 = toTest;
            toTest = EditorGUILayout.IntField(toTest, GUILayout.MaxWidth(180));
            if (temp2 != toTest) toTestVector = PathfindingUtilities.IntToVector(toTest, _pathfindingHolder._totalSize);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("vector", GUILayout.MaxWidth(120));
            int3 temp3 = toTest;

            toTestVector.x = EditorGUILayout.IntField(toTestVector.x, GUILayout.MaxWidth(180));
            toTestVector.y = EditorGUILayout.IntField(toTestVector.y, GUILayout.MaxWidth(180));
            toTestVector.z = EditorGUILayout.IntField(toTestVector.z, GUILayout.MaxWidth(180));
            bool3 temp4 = temp3 == toTestVector;
            if (!(temp4.x && temp4.x && temp4.x)) toTest = PathfindingUtilities.VectorToInt(toTestVector, _pathfindingHolder._totalSize);
            EditorGUILayout.EndHorizontal();*/
        }

        private void waitForBake(bool answer)
        {
            if (answer) Bake();
        }
        private void Bake()
        {
            _pathfindingHolder._voxelSize = boxSize;
            _pathfindingHolder._maxDistanceFromWall = detectionSize;
            _pathfindingHolder._collisionMask = collisionMask;
            _pathfindingHolder._startToBake = true;
        }

        private void DrawCreateGridWindow()
        {
            GUILayout.Label("No PathfindingHolder selected. Select one in the Hierarchy, or create a new one.");
            if (GUILayout.Button("Create")) createGridObject();
        }

        private void createGridObject()
        {
            GameObject newInstance = new GameObject("Pathfinding");
            _pathfindingHolder = newInstance.AddComponent<PathfindingHolder>();
            Selection.activeGameObject = newInstance;
        }

            
    }


    public class PopupAlert : EditorWindow
    {
        Action<bool> callBack;
        public static void ShowAlert(Action<bool> callback)
        {
            PopupAlert alert = GetWindow<PopupAlert>();
            alert.callBack = callback;
            alert.Show();

        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("The bake you are going to do is very expensive");

            EditorGUILayout.LabelField("I suggest you reduce the voxel size or the bake size");


            EditorGUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Do it anyways"))
            {
                callBack?.Invoke(true);
                Close();
            }

            if (GUILayout.Button("Cancel"))
            {
                callBack?.Invoke(false);
                Close();
            }
            EditorGUILayout.EndHorizontal();

            
        }
    }
}