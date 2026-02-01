using UnityEngine;
using UnityEditor;
using Maskhot.Testing;

namespace Maskhot.Editor
{
    [CustomEditor(typeof(RandomPostTester))]
    public class RandomPostTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            RandomPostTester tester = (RandomPostTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Actions", EditorStyles.boldLabel);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Test Specific Candidate"))
            {
                tester.TestSpecificCandidate();
            }

            if (GUILayout.Button("Test All Assigned Candidates"))
            {
                tester.TestAllAssignedCandidates();
            }

            if (GUILayout.Button("Test All Candidates (via ProfileManager)"))
            {
                tester.TestAllCandidatesViaProfileManager();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Reset Post Pool"))
            {
                tester.ResetPostPool();
            }

            GUI.enabled = wasEnabled;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to run tests.", MessageType.Info);
            }
        }
    }
}
