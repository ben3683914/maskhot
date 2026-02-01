using UnityEngine;
using UnityEditor;
using Maskhot.Testing;

namespace Maskhot.Editor
{
    [CustomEditor(typeof(QuestManagerTester))]
    public class QuestManagerTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw default inspector fields
            DrawDefaultInspector();

            QuestManagerTester tester = (QuestManagerTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Actions", EditorStyles.boldLabel);

            // Only enable buttons in play mode
            bool wasEnabled = GUI.enabled;
            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Test Client Loading"))
            {
                tester.TestClientLoading();
            }

            if (GUILayout.Button("Test Start Quest"))
            {
                tester.TestStartQuest();
            }

            if (GUILayout.Button("Test Quest Lifecycle"))
            {
                tester.TestQuestLifecycle();
            }

            if (GUILayout.Button("Test Quest with Queue Population"))
            {
                tester.TestQuestWithQueuePopulation();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Log Current State"))
            {
                tester.LogCurrentState();
            }

            if (GUILayout.Button("Clear Current Quest"))
            {
                tester.ClearCurrentQuest();
            }

            GUI.enabled = wasEnabled;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to run tests.", MessageType.Info);
            }
        }
    }
}
