using UnityEngine;
using UnityEditor;
using Maskhot.Testing;

namespace Maskhot.Editor
{
    [CustomEditor(typeof(GameManagerTester))]
    public class GameManagerTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw default inspector fields
            DrawDefaultInspector();

            GameManagerTester tester = (GameManagerTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Actions", EditorStyles.boldLabel);

            // Only enable buttons in play mode
            bool wasEnabled = GUI.enabled;
            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Log Current State"))
            {
                tester.LogCurrentState();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Session Tests", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Test Start Session"))
            {
                tester.TestStartSession();
            }

            if (GUILayout.Button("Test Begin Quest"))
            {
                tester.TestBeginQuest();
            }

            if (GUILayout.Button("Test Complete Quest (Simulate Decisions)"))
            {
                tester.TestCompleteQuest();
            }

            if (GUILayout.Button("Test Full Session Flow"))
            {
                tester.TestFullSessionFlow();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("State Tests", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Test State Transitions"))
            {
                tester.TestStateTransitions();
            }

            if (GUILayout.Button("Test Reset Session"))
            {
                tester.TestResetSession();
            }

            GUI.enabled = wasEnabled;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to run tests.", MessageType.Info);
            }
        }
    }
}
