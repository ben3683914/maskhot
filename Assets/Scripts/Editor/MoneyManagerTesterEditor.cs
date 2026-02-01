using UnityEngine;
using UnityEditor;
using Maskhot.Testing;

namespace Maskhot.Editor
{
    [CustomEditor(typeof(MoneyManagerTester))]
    public class MoneyManagerTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MoneyManagerTester tester = (MoneyManagerTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Actions", EditorStyles.boldLabel);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = Application.isPlaying;

            // Core tests
            EditorGUILayout.LabelField("Balance Tests", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Test Initial State"))
            {
                tester.TestInitialState();
            }

            if (GUILayout.Button("Test CanAfford"))
            {
                tester.TestCanAfford();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Transaction Tests", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Test Spend"))
            {
                tester.TestSpend();
            }

            if (GUILayout.Button("Test Spend (Insufficient Funds)"))
            {
                tester.TestSpendInsufficientFunds();
            }

            if (GUILayout.Button("Test AddMoney"))
            {
                tester.TestAddMoney();
            }

            if (GUILayout.Button("Test ResetBalance"))
            {
                tester.TestResetBalance();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Utilities", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Log Current State"))
            {
                tester.LogCurrentState();
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
