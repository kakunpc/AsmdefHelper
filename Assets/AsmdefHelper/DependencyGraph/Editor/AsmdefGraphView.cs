using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AsmdefHelper.DependencyGraph.Editor {
    public class AsmdefGraphView : GraphView {
        private readonly Dictionary<string, AsmdefNode> asmdefNodeDict;
        private readonly AsmdefDependency[] _dependencies;

        public AsmdefGraphView(IEnumerable<AsmdefDependency> asmdefs) : base() {
            _dependencies = asmdefs.ToArray();
            // zoom可能に
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            // 背景を黒に
            Insert(0, new GridBackground());
            // ドラッグによる移動可能に
            this.AddManipulator(new SelectionDragger());
            // ノードの追加
            asmdefNodeDict = new Dictionary<string, AsmdefNode>();
            foreach (var asmdef in asmdefs) {
                var node = new AsmdefNode(asmdef.DependsFrom);
                AddElement(node);
                asmdefNodeDict.Add(node.title, node);
            }

            // 依存先にラインを追加
            foreach (var asmdef in asmdefs) {
                if (!asmdefNodeDict.TryGetValue(asmdef.DependsFrom, out var fromNode)) {
                    continue;
                }

                foreach (var dependents in asmdef.DependsTo) {
                    if (!asmdefNodeDict.TryGetValue(dependents, out var toNode)) {
                        continue;
                    }

                    var edge = fromNode.RightPort.ConnectTo(toNode.LeftPort);
                    contentContainer.Add(edge); // これが無いと線が表示されない
                }
            }
        }

        public void Sort(
            int tryCount = 300,
            float relationK = -0.01f,
            float relationNaturalLength = 300,
            float repulsivePower = 0.01f,
            float threshold = 300) {

            var max = asmdefNodeDict.Count;
            var positions = new Vector2[max];

            // 初期位置
            var keys = asmdefNodeDict.Keys.ToArray();
            for (int i = 0; i < max; i++) {
                var node = asmdefNodeDict[keys[i]];
                positions[i] = new Vector2(node.layout.x, node.layout.y);
            }

            //----- 関係性グラフを作成 ----
            var relations = new List<int>[max];
            for (int i = 0; i < max; i++) {

                var s = _dependencies[i];
                relations[i] = new List<int>();

                foreach (var transition in s.DependsTo) {
                    // ノードがつながっている場合は互いに連動する
                    var target = FindState(_dependencies, transition);
                    relations[i].Add(target);
                    if (relations[target] == null) relations[target] = new List<int>();
                    relations[target].Add(i);
                }
            }

            while (tryCount-- > 0) {
                for (int i = 0; i < max; i++) {
                    var target = positions[i];
                    var force = Vector2.zero;

                    for (int j = 0; j < max; j++) {
                        if (j == i) continue;
                        {
                            var other = positions[j];

                            // ばねの計算
                            // 接続したノード同士はばねによって引き合う
                            var isConnectedNode = relations[i].Contains(j);
                            if (isConnectedNode) {
                                var k = relationK;
                                var nl = relationNaturalLength;

                                var l = (target - other).magnitude;
                                var delta = l - nl;

                                force += -(delta * k * (other - target).normalized);
                            }

                            // 全ノードは互いに斥力が発生する
                            {
                                var l = (other - target).magnitude;
                                if (l < threshold) {
                                    force += -(other - target).normalized * ((threshold - l) * repulsivePower);
                                }
                            }
                        }
                    }


                    positions[i] = target + force * 1.0f;
                }
            }

            //-----   新しい配置に上書き  ---------
            for (int i = 0; i < max; i++) {
                var node = asmdefNodeDict[keys[i]];
                var rect = node.layout;
                rect.x = positions[i].x;
                rect.y = positions[i].y;
                node.SetPosition(rect);
            }
        }

        private static int FindState(AsmdefDependency[] states, string state) {
            for (int i = 0; i < states.Length; i++) {
                if (state == states[i].DependsFrom) {
                    return i;
                }
            }

            return -1;
        }

        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter) {
            return ports.ToList();
        }
    }
}
