using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LoadPositionConfig))]
public class LoadPositionEditor : Editor
{
    private void OnEnable() {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable() {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView) {
        LoadPositionConfig loadPostionConfig = target as LoadPositionConfig;
        if (loadPostionConfig.range) {
            Handles.color = Color.cyan;
            Vector3[] inner = loadPostionConfig.innerBound.GetBorder();
            Vector3[] outer = loadPostionConfig.outerBound.GetBorder();
            for (int i = 0; i < 4; i++) {
                Handles.DrawLine(inner[i], inner[(i + 1) % 4]);
                Handles.DrawLine(outer[i], outer[(i + 1) % 4]);
            }
        } else {
            int count = loadPostionConfig.loads.Count;
            for (int i = 0; i < count; i++) {
                Handles.color = Color.yellow;
                Handles.DrawSolidDisc(loadPostionConfig.loads[i].position, Vector3.up, 0.5f);
                Handles.DrawLine(loadPostionConfig.loads[i].position, loadPostionConfig.loads[i].position + Quaternion.Euler(loadPostionConfig.loads[i].rotation) * new Vector3(0, 0, 1));
            }
        }
    }
}
