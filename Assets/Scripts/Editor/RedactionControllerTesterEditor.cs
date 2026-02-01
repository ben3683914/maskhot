using UnityEngine;
using UnityEditor;
using Maskhot.Testing;

namespace Maskhot.Editor
{
    [CustomEditor(typeof(RedactionControllerTester))]
    public class RedactionControllerTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            RedactionControllerTester tester = (RedactionControllerTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Actions", EditorStyles.boldLabel);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = Application.isPlaying;

            // Redaction status tests
            EditorGUILayout.LabelField("Redaction Status Tests", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Test Guaranteed Posts (Never Redacted)"))
            {
                tester.TestGuaranteedPosts();
            }

            if (GUILayout.Button("Test Random Posts (Start Redacted)"))
            {
                tester.TestRandomPostsRedacted();
            }

            if (GUILayout.Button("Test GetDisplayText"))
            {
                tester.TestGetDisplayText();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Unredact Tests", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Test TryUnredact"))
            {
                tester.TestTryUnredact();
            }

            if (GUILayout.Button("Test Counts"))
            {
                tester.TestCounts();
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
