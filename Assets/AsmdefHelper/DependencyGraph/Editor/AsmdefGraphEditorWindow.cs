using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine.UIElements;

namespace AsmdefHelper.DependencyGraph.Editor {
    public class AsmdefGraphEditorWindow : EditorWindow {
        private AsmdefGraphView _asmdefGraphView;
        [MenuItem("Window/Asmdef Helper/Open DependencyGraph", priority = 2000)]
        public static void Open() {
            GetWindow<AsmdefGraphEditorWindow>("Asmdef Dependency");
        }

        void OnEnable() {
            // .asmdefをすべて取得
            var asmdefs = CompilationPipeline.GetAssemblies();
            var allDependencies = new List<AsmdefDependency>();
            foreach (var asmdef in asmdefs) {
                allDependencies.Add(
                    new AsmdefDependency(
                        asmdef.name,
                        asmdef.assemblyReferences?.Select(x => x.name) ?? new string[0])
                    );
            }
            // viewの作成
            _asmdefGraphView = new AsmdefGraphView(allDependencies) {
                style = { flexGrow = 1 }
            };
            rootVisualElement.Add(_asmdefGraphView);
            rootVisualElement.pickingMode = PickingMode.Position;  // ピッキングモード変更
            rootVisualElement.AddManipulator(new ContextualMenuManipulator(OnContextMenuPopulate));
        }

        void Update() {
            _asmdefGraphView.Sort(1);
        }

        void OnContextMenuPopulate(ContextualMenuPopulateEvent evt) {
            evt.menu.AppendAction(
                "Sort", // 項目名
                _ => _asmdefGraphView.Sort(300), // 選択時の挙動
                DropdownMenuAction.AlwaysEnabled // 選択可能かどうか
            );
        }
    }
}
