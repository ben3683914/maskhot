using UnityEngine;
using UnityEditor;

namespace Maskhot.Editor
{
    [CustomEditor(typeof(ProfileTester))]
    public class ProfileTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ProfileTester tester = (ProfileTester)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Test Actions", EditorStyles.boldLabel);

            // This test can run in edit mode too since it just reads data
            if (GUILayout.Button("Test All Profiles"))
            {
                tester.TestAllProfiles();
            }
        }
    }
}
