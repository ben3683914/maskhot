using UnityEngine;
using UnityEditor;
using Maskhot.Testing;

namespace Maskhot.Editor
{
    [CustomEditor(typeof(RedactionManagerTester))]
    public class RedactionManagerTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            RedactionManagerTester tester = (RedactionManagerTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Actions", EditorStyles.boldLabel);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = Application.isPlaying;

            // Core data tests
            EditorGUILayout.LabelField("Data Tests", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Test IsUnredacted (Initial State)"))
            {
                tester.TestIsUnredacted();
            }

            if (GUILayout.Button("Test MarkUnredacted"))
            {
                tester.TestMarkUnredacted();
            }

            if (GUILayout.Button("Test MarkUnredacted Twice"))
            {
                tester.TestMarkUnredactedTwice();
            }

            if (GUILayout.Button("Test ResetAll"))
            {
                tester.TestResetAll();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Utilities", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Log Current State"))
            {
                tester.LogCurrentState();
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Populate Test Queue"))
            {
                tester.PopulateTestQueue();
            }
            if (GUILayout.Button("Clear Queue"))
            {
                tester.ClearQueue();
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
