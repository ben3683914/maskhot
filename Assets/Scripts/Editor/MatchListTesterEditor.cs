using UnityEngine;
using UnityEditor;
using Maskhot.Testing;

namespace Maskhot.Editor
{
    [CustomEditor(typeof(MatchListTester))]
    public class MatchListTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw default inspector fields
            DrawDefaultInspector();

            MatchListTester tester = (MatchListTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Actions", EditorStyles.boldLabel);

            // Only enable buttons in play mode
            bool wasEnabled = GUI.enabled;
            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Test Selection By Index"))
            {
                tester.TestSelectionByIndex();
            }

            if (GUILayout.Button("Test Navigation"))
            {
                tester.TestNavigation();
            }

            if (GUILayout.Button("Test Select Next Pending"))
            {
                tester.TestSelectNextPending();
            }

            if (GUILayout.Button("Test Current Posts"))
            {
                tester.TestCurrentPosts();
            }

            if (GUILayout.Button("Test Clear Selection"))
            {
                tester.TestClearSelection();
            }

            if (GUILayout.Button("Test Queue Properties"))
            {
                tester.TestQueueProperties();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Log Current State"))
            {
                tester.LogCurrentState();
            }

            GUI.enabled = wasEnabled;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to run tests.", MessageType.Info);
            }
        }
    }
}
