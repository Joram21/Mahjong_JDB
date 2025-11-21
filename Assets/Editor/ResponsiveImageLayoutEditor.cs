// Custom inspector helper (optional - for better editor experience)
#if UNITY_EDITOR
using UnityEngine;

[UnityEditor.CustomEditor(typeof(ResponsiveImageLayout))]
public class ResponsiveImageLayoutEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ResponsiveImageLayout script = (ResponsiveImageLayout)target;

        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("Quick Actions", UnityEditor.EditorStyles.boldLabel);

        if (GUILayout.Button("Apply Current Layout"))
        {
            script.ForceApplyLayout();
        }

        UnityEditor.EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Test Portrait"))
        {
            script.TestPortraitLayout();
        }
        if (GUILayout.Button("Test Tablet"))
        {
            script.TestTabletLayout();
        }
        if (GUILayout.Button("Test Landscape"))
        {
            script.TestLandscapeLayout();
        }
        UnityEditor.EditorGUILayout.EndHorizontal();
    }
}
#endif