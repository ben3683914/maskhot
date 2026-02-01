using UnityEngine;
using UnityEditor;
using Maskhot.Testing;

namespace Maskhot.Editor
{
    [CustomEditor(typeof(DecisionControllerTester))]
    public class DecisionControllerTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DecisionControllerTester tester = (DecisionControllerTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Actions", EditorStyles.boldLabel);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = Application.isPlaying;

            // Decision tests
            EditorGUILayout.LabelField("Decision Tests", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Test Accept Current"))
            {
                tester.TestAcceptCurrent();
            }

            if (GUILayout.Button("Test Reject Current"))
            {
                tester.TestRejectCurrent();
            }

            if (GUILayout.Button("Test Decide All"))
            {
                tester.TestDecideAll();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Feature Tests", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Test Auto-Advance"))
            {
                tester.TestAutoAdvance();
            }

            if (GUILayout.Button("Test Statistics"))
            {
                tester.TestStatistics();
            }

            if (GUILayout.Button("Test Session Reset"))
            {
                tester.TestSessionReset();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Utilities", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Log Current State"))
            {
                tester.LogCurrentState();
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Test Quest"))
            {
                tester.StartTestQuest();
            }
            if (GUILayout.Button("Clear Setup"))
            {
                tester.ClearTestSetup();
            }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = wasEnabled;

            if (!Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Enter Play Mode to run tests.", MessageType.Info);
            }
        }
    }
}
