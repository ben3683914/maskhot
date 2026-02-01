using UnityEngine;
using UnityEditor;
using Maskhot.Testing;

namespace Maskhot.Editor
{
    [CustomEditor(typeof(QuestControllerTester))]
    public class QuestControllerTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            QuestControllerTester tester = (QuestControllerTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Actions", EditorStyles.boldLabel);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Test Event Subscription"))
            {
                tester.TestEventSubscription();
            }

            if (GUILayout.Button("Test Cached Data"))
            {
                tester.TestCachedData();
            }

            if (GUILayout.Button("Test Cache Clearing"))
            {
                tester.TestCacheClearing();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Log Current State"))
            {
                tester.LogCurrentState();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Start Test Quest"))
            {
                tester.StartTestQuest();
            }

            if (GUILayout.Button("Clear Quest"))
            {
                tester.ClearQuest();
            }

            GUI.enabled = wasEnabled;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to run tests.", MessageType.Info);
            }
        }
    }
}
