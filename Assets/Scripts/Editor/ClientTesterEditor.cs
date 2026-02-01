using UnityEngine;
using UnityEditor;
using Maskhot.Testing;

namespace Maskhot.Editor
{
    [CustomEditor(typeof(ClientTester))]
    public class ClientTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ClientTester tester = (ClientTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Actions", EditorStyles.boldLabel);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Test Specific Client"))
            {
                tester.TestSpecificClient();
            }

            if (GUILayout.Button("Test All Assigned Clients"))
            {
                tester.TestAllAssignedClients();
            }

            if (GUILayout.Button("Test All Clients (from Resources)"))
            {
                tester.TestAllClientsFromResources();
            }

            GUI.enabled = wasEnabled;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to run tests.", MessageType.Info);
            }
        }
    }
}
