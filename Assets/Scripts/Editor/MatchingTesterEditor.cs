using UnityEngine;
using UnityEditor;
using Maskhot.Testing;

namespace Maskhot.Editor
{
    [CustomEditor(typeof(MatchingTester))]
    public class MatchingTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MatchingTester tester = (MatchingTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Actions", EditorStyles.boldLabel);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Test Specific Match"))
            {
                tester.TestSpecificMatch();
            }

            if (GUILayout.Button("Test All Assigned (Candidates x Clients)"))
            {
                tester.TestAllAssigned();
            }

            if (GUILayout.Button("Test All (from Resources)"))
            {
                tester.TestAllFromResources();
            }

            if (GUILayout.Button("Quick Summary (from Resources)"))
            {
                tester.QuickSummary();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Algorithm", EditorStyles.boldLabel);

            if (GUILayout.Button("Cycle Algorithm Mode"))
            {
                tester.CycleAlgorithmMode();
            }

            GUI.enabled = wasEnabled;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to run tests.", MessageType.Info);
            }
        }
    }
}
