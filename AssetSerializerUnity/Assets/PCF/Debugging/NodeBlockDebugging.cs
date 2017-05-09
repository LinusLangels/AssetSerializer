using UnityEngine;
using System.Collections;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PCFFileFormat.Debugging
{
    public class NodeBlockDebugging
    {
        #if UNITY_EDITOR
        EditorWindow nodeWindow;
        #endif

        public NodeBlockDebugging()
        {
        }

        public void DrawNodeTree(UnityNodeBase rootNode)
        {
			#if UNITY_EDITOR
			EditorApplication.ExecuteMenuItem("AssetSerializer/Node Graph");
            this.nodeWindow = EditorWindow.focusedWindow;

            DebugTree(rootNode);
			#endif
        }

        void DebugTree(UnityNodeBase node)
        {
			#if UNITY_EDITOR
            if (node != null)
            {
                JObject jsonNode = node.GetJSONRepresentation();

                Event e = EditorGUIUtility.CommandEvent(jsonNode.ToString());
                this.nodeWindow.SendEvent(e);

                if (node.ChildNodes!= null)
                {
                    for (int i = 0; i < node.ChildNodes.Count; i++)
                    {
                        UnityNodeBase child = node.ChildNodes[i];

                        DebugTree(child);
                    }
                }
            }
			#endif
        }
    }
}
