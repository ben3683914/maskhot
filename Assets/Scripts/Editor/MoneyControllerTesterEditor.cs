using UnityEngine;
using UnityEditor;
using Maskhot.Testing;

namespace Maskhot.Editor
{
    [CustomEditor(typeof(MoneyControllerTester))]
    public class MoneyControllerTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MoneyControllerTester tester = (MoneyControllerTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Actions", EditorStyles.boldLabel);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = Application.isPlaying;

            // Configuration tests
            EditorGUILayout.LabelField("Configuration", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Test Configuration"))
            {
                tester.TestConfiguration();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Unredact Tests", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Test CanAffordUnredact"))
            {
                tester.TestCanAffordUnredact();
            }

            if (GUILayout.Button("Test TrySpendForUnredact"))
            {
                tester.TestTrySpendForUnredact();
            }

            if (GUILayout.Button("Test TrySpendForUnredact (Insufficient)"))
            {
                tester.TestTrySpendForUnredactInsufficient();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("General Transaction Tests", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Test TrySpend"))
            {
                tester.TestTrySpend();
            }

            if (GUILayout.Button("Test AddMoney"))
            {
                tester.TestAddMoney();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Utilities", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Log Current State"))
            {
                tester.LogCurrentState();
            }

            if (GUILayout.Button("Reset Balance"))
            {
                tester.ResetBalance();
            }

            GUI.enabled = wasEnabled;

            if (!Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Enter Play Mode to run tests.", MessageType.Info);
            }
        }
    }
}
