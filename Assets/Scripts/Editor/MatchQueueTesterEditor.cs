using UnityEngine;
using UnityEditor;
using Maskhot.Testing;

namespace Maskhot.Editor
{
    [CustomEditor(typeof(MatchQueueTester))]
    public class MatchQueueTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw default inspector fields
            DrawDefaultInspector();

            MatchQueueTester tester = (MatchQueueTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Actions", EditorStyles.boldLabel);

            // Only enable buttons in play mode
            bool wasEnabled = GUI.enabled;
            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Test Random Population"))
            {
                tester.TestRandomPopulation();
            }

            if (GUILayout.Button("Test Quest Population"))
            {
                tester.TestQuestPopulation();
            }

            if (GUILayout.Button("Test Decision Tracking"))
            {
                tester.TestDecisionTracking();
            }

            if (GUILayout.Button("Test Query Methods"))
            {
                tester.TestQueryMethods();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Log Current State"))
            {
                tester.LogCurrentState();
            }

            if (GUILayout.Button("Clear Queue"))
            {
                tester.ClearQueue();
            }

            GUI.enabled = wasEnabled;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to run tests.", MessageType.Info);
            }
        }
    }
}
