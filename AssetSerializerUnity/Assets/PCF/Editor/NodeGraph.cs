using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCFFileFormat.Editor
{
    public class NodeGraph : EditorWindow
    {
        public class Node
        {
            public string type;
            public string title;
            public Rect rect;
            public UInt32 id;
            public UInt32 internalID;
            public UInt32 parent;
            public string text;
            public List<Node> Children;

            public Node()
            {
                this.Children = new List<Node>();
            }

            public Node(string title, Rect rect, UInt32 id, string text, UInt32 parent)
            {
                this.title = title;
                this.rect = rect;
                this.id = id;
                this.text = text;
                this.parent = parent;

                this.Children = new List<Node>();
            }
        }

        static Dictionary<UInt32, Node> Nodes;
        static Dictionary<string, Func<UInt32, string, UInt32, Node>> actionMap;

        static Vector2 windowSize = new Vector2(600, 500);
        static NodeGraph graphWindow = null;
        static Vector2 nodeSize = new Vector2(115, 115);
        private Vector2 scrollPos;
        private Rect scrollAreaRect = new Rect(0f, 0f, windowSize.x, windowSize.y);

        static NodeGraph()
        {
            actionMap = new Dictionary<string, Func<UInt32, string, UInt32, Node>>();

            actionMap.Add("UnityRootNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Root Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityObjectNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Object Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("SimpleObjectNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Simple Object Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityComponentNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Component Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityGameObjectNode", (UInt32 id, string text, UInt32 parent) => { return new Node("GameObject", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityMeshNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Mesh Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityTransformNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Transform Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityAudioNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Audio Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityAvatarReferenceNode", (UInt32 id, string text, UInt32 parent) => { return new Node("AvatarRef Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityMaterialNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Material Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityMaterialPointerNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Material Pointer Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityTextureNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Texture Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnitySVGNode", (UInt32 id, string text, UInt32 parent) => { return new Node("SVG Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnitySkinnedMeshNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Skinned Mesh Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityScriptNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Script Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityCameraNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Camera Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityLightNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Light Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityLightProbesNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Lightprobe Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityColliderNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Collider Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityAnimatorNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Animator Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityWeightNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Weight Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityPrimitiveNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Primitive Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityCollectionNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Collection Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityClassNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Class Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityGradientNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Gradient Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityTransformPointerNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Transform Pointer Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityPointerCollectionNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Pointer Collection Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityInternalBundleNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Internal Bundle", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityAnimationNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Animation Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityAnimationClipNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Animation Clip", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityAnimationClipPointerNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Animation Clip Pointer", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
            actionMap.Add("UnityAnimationLoaderNode", (UInt32 id, string text, UInt32 parent) => { return new Node("Animation Loader Node", new Rect(100, 10, nodeSize.x, nodeSize.y), id, text, parent); });
        }

        [MenuItem("AssetSerializer/Node Graph")]
        static void Init()
        {
            Nodes = new Dictionary<UInt32, Node>();

            graphWindow = EditorWindow.GetWindow<NodeGraph>();

            graphWindow.maxSize = windowSize * 1.5f; //Can be 50% larger than minsize.
            graphWindow.minSize = windowSize;
            graphWindow.titleContent.text = "Nodes";

            graphWindow.Show();
        }

        void CreateHierarchy()
        {
            //Clear all children before we set them.
            foreach (KeyValuePair<UInt32, Node> pair in Nodes)
                pair.Value.Children.Clear();

            foreach (KeyValuePair<UInt32, Node> pair in Nodes)
            {
                if (Nodes.ContainsKey(pair.Value.parent) && pair.Value.parent != 0)
                {
                    Nodes[pair.Value.parent].Children.Add(pair.Value);
                }
            }
        }

        Vector2 InitialNodePositions(Node node, Vector2 pos, ref int maxWidth, ref int maxHeight)
        {
            if (node != null)
            {
                node.rect.x = pos.x;
                node.rect.y = pos.y;
            }

            if (node.Children != null && node.Children.Count > 0)
            {
                pos.y += 150f;

                for (int i = 0; i < node.Children.Count; i++)
                {
                    pos = InitialNodePositions(node.Children[i], pos, ref maxWidth, ref maxHeight);
                    pos.x += 150f;

                    if (pos.x > maxWidth)
                    {
                        maxWidth = (int)pos.x;
                    }

                    if (pos.y > maxHeight)
                    {
                        maxHeight = (int)pos.y;
                    }
                }

                //Reverse the horizontal and vertical done at this level.
                pos.y -= 150f;
                pos.x -= (node.Children.Count * 150) * 0.5f;

                return pos;
            }

            return pos;
        }

        void DrawConnections()
        {
            if (Nodes.Count > 0)
            {
                Node rootNode = Nodes.Values.ElementAt(0);

                BezierDrawer(rootNode);
            }
        }

        void BezierDrawer(Node node)
        {
            if (node.Children != null && node.Children.Count > 0)
            {
                foreach (Node child in node.Children)
                {
                    DrawNodeCurve(node, child);
                    BezierDrawer(child);
                }
            }
        }

        void OnGUI()
        {
            Event evt = Event.current;

            int xArea = 100;
            int yArea = 100;

            if (evt.type == EventType.ExecuteCommand)
            {
                Node node = ParseEventString(evt.commandName);

                if (node != null && actionMap.ContainsKey(node.type))
                {
                    Nodes.Add(node.internalID, actionMap[node.type](node.id, node.text, node.parent));
                }
                else
                {
                    Debug.LogError("Sent unknown node to graph window: " + evt.commandName);
                }

                evt.Use();

                //Recreate the hierarchy.
                CreateHierarchy();

                //Place the nodes in an initial position, later we can drag them around. 
                //This is only meaningful to do after we have all the nodes in a hierarchy.
                InitialNodePositions(Nodes.Values.ElementAt(0), new Vector2((graphWindow.position.width / 2f) - (nodeSize.x / 2), 15f), ref xArea, ref yArea);

                scrollAreaRect.width = xArea;
                scrollAreaRect.height = yArea + 100;
            }

            if (Nodes == null || Nodes.Count < 1)
                return;

            scrollPos = GUI.BeginScrollView(new Rect(0, 0, position.width, position.height), scrollPos, scrollAreaRect, true, true);

            //Draw the bezier handles.
            DrawConnections();

            BeginWindows();

            int index = 0;
            foreach (KeyValuePair<UInt32, Node> pair in Nodes)
            {
                pair.Value.rect = GUI.Window(index, pair.Value.rect, DrawNodeWindow, pair.Value.title);

                index++;
            }

            EndWindows();

            GUI.EndScrollView();
        }

        //Use this method to specify whats inside each node in the graph.
        void DrawNodeWindow(int id)
        {
            GUI.Label(new Rect(10, 20, nodeSize.x, nodeSize.y), "ID: " + Nodes.Values.ElementAt(id).id);
            GUI.Label(new Rect(10, 40, nodeSize.x, nodeSize.y), "Text: " + Nodes.Values.ElementAt(id).text);

            GUI.DragWindow();
        }

        void DrawNodeCurve(Node start, Node end)
        {
            Rect startRect = start.rect;
            Rect endRect = end.rect;

            List<Vector3> edges = FindClosestEdge(startRect, endRect);

            Vector3 startPos = edges[0];
            Vector3 endPos = edges[1];
            Vector3 direction = endPos - startPos;

            Vector3 startTan = startPos + (direction.normalized * 30);
            Vector3 endTan = endPos + (-direction.normalized * 30);
            Color shadowCol = new Color(0, 0, 0, 0.06f);

            for (int i = 0; i < 3; i++)
            {
                Handles.DrawBezier(startPos, endPos, startTan, endTan, shadowCol, null, (i + 1) * 5);
            }

            Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.black, null, 1f);
        }

        List<Vector3> FindClosestEdge(Rect start, Rect end)
        {
            float widthOffset = start.width / 2f;

            List<Vector3> startEdges = new List<Vector3>()
        {
            new Vector3(start.center.x + widthOffset, start.center.y, 0),
            new Vector3(start.center.x, start.center.y - widthOffset, 0),
            new Vector3(start.center.x - widthOffset, start.center.y, 0),
            new Vector3(start.center.x, start.center.y + widthOffset, 0),
        };

            List<Vector3> endEdges = new List<Vector3>()
        {
            new Vector3(end.center.x + widthOffset, end.center.y, 0),
            new Vector3(end.center.x, end.center.y - widthOffset, 0),
            new Vector3(end.center.x - widthOffset, end.center.y, 0),
            new Vector3(end.center.x, end.center.y + widthOffset, 0),
        };

            Vector3 closestStart = startEdges[0];
            Vector3 closestEnd = endEdges[0];
            float closestLength = 10000f;

            for (int i = 0; i < 4; i++)
            {
                Vector3 startEdge = startEdges[i];

                for (int j = 0; j < 4; j++)
                {
                    Vector3 endEdge = endEdges[j];

                    float length = (endEdge - startEdge).magnitude;

                    if (length < closestLength)
                    {
                        closestLength = length;
                        closestStart = startEdge;
                        closestEnd = endEdge;
                    }
                }
            }

            return new List<Vector3>() { closestStart, closestEnd };
        }

        Node ParseEventString(string input)
        {
            JObject parsedObject = JObject.Parse(input);

            Node node = new Node();
            node.type = parsedObject.Value<string>("type");
            node.id = parsedObject.Value<UInt32>("id");
            node.internalID = parsedObject.Value<UInt32>("internalID");
            node.parent = parsedObject.Value<UInt32>("parent");
            node.text = parsedObject.Value<string>("text");

            return node;
        }

        void OnDestroy()
        {
            if (Nodes != null)
                Nodes.Clear();
        }
    }
}