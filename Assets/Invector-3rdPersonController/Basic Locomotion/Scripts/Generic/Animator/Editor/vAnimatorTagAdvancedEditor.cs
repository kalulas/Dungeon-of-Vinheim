using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Invector.vEventSystems
{
    [CustomEditor(typeof(vAnimatorTagAdvanced))]
    public class vAnimatorTagAdvancedEditor : Editor
    {
        public vAnimatorTagAdvanced animatorTag;
        public GUISkin skin;

        private void OnEnable()
        {
            animatorTag = target as vAnimatorTagAdvanced;
        }

        public override void OnInspectorGUI()
        {
            if (!skin) skin = Resources.Load("vSkin") as GUISkin;
            serializedObject.Update();
            GUILayout.BeginVertical(skin.box);
            EditorStyles.helpBox.richText = true;
            EditorGUILayout.HelpBox("Useful Tags:\n " +
                "<b>CustomAction </b> - <i> Lock's position and rotation to use RootMotion instead</i> \n " +
                "<b>LockMovement </b> - <i> Use to lock the character movement </i> \n " +
                "<b>LockRotation </b> - <i> Use to lock the character rotation</i> \n " +
                "<b>IgnoreHeadtrack </b> - <i> Ignore the Headtrack and follows the animation</i> \n " +
                "<b>IgnoreIK </b> - <i> Ignore IK while this animation is playing</i> \n " +
                "<b>Attack </b> - <i> Use for Melee Attacks </i>"
                , MessageType.Info);
            var tags = serializedObject.FindProperty("tags");
            if (GUILayout.Button("Add Tag", skin.button, GUILayout.ExpandWidth(true)))
            {
                tags.arraySize++;
                tags.GetArrayElementAtIndex(tags.arraySize - 1).FindPropertyRelative("tagName").stringValue = "New Tag";
                tags.GetArrayElementAtIndex(tags.arraySize - 1).FindPropertyRelative("normalizedTime").vector2Value = new Vector2(0,1);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("debug"));

            for (int i = 0; i < tags.arraySize; i++)
            {
                if (!DrawTag(tags, i)) break;
            }
            GUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }

        public bool DrawTag(SerializedProperty list, int index)
        {
            GUILayout.BeginHorizontal(skin.box);
            GUILayout.BeginVertical();
            var tagToDraw = list.GetArrayElementAtIndex(index);
            var tagName = tagToDraw.FindPropertyRelative("tagName");
            var enumTagType = tagToDraw.FindPropertyRelative("tagType");
            var normalizedTime = tagToDraw.FindPropertyRelative("normalizedTime");
            EditorGUILayout.PropertyField(tagName, GUIContent.none, GUILayout.Height(15));
            EditorGUILayout.PropertyField(enumTagType,  GUILayout.Height(15));
            Vector2 minMax = normalizedTime.vector2Value;

            var _enumTagType = (vAnimatorTagAdvanced.vAnimatorEventTriggerType)(enumTagType.enumValueIndex);
            switch(_enumTagType)
            {
                case vAnimatorTagAdvanced.vAnimatorEventTriggerType.AllByNormalizedTime:                   
                    
                    GUILayout.Label("Enter in " + minMax.x.ToString("0.00") + " - Exit in "+minMax.y.ToString("0.00"));
                    GUILayout.BeginHorizontal();
                    minMax.x = EditorGUILayout.FloatField(minMax.x,GUILayout.MaxWidth(40));
                    EditorGUILayout.MinMaxSlider(ref  minMax.x, ref minMax.y, 0, 1f);
                    minMax.y = EditorGUILayout.FloatField(minMax.y, GUILayout.MaxWidth(40));
                    GUILayout.EndHorizontal();
                    if(GUI.changed)
                    {
                        minMax.x =(float) System.Math.Round(minMax.x, 2);
                        minMax.y = (float)System.Math.Round(minMax.y, 2);
                        normalizedTime.vector2Value = minMax;
                    }
                    break;
               
                case vAnimatorTagAdvanced.vAnimatorEventTriggerType.EnterStateExitByNormalized:
                   
                    GUILayout.Label("Exit in " + minMax.y.ToString("0.00"));
                    minMax.y = EditorGUILayout.Slider(minMax.y, 0, 1f);
                    if (GUI.changed)
                    {
                        minMax.x = 0;
                        minMax.y = (float)System.Math.Round(minMax.y, 2);
                        normalizedTime.vector2Value = minMax;
                    }
                    break;
                case vAnimatorTagAdvanced.vAnimatorEventTriggerType.EnterByNormalizedExitState:

                    GUILayout.Label("Enter in " + minMax.x.ToString("0.00"));
                    minMax.x = EditorGUILayout.Slider(minMax.x, 0, 1f);
                    if (GUI.changed)
                    {
                        minMax.x = (float)System.Math.Round(minMax.x, 2);
                        minMax.y = 0;
                        normalizedTime.vector2Value = minMax;
                    }
                    break;
            }

            GUILayout.EndVertical();
            if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                list.DeleteArrayElementAtIndex(index);
                return false;
            }

            GUILayout.EndHorizontal();
            return true;
        }
    }
}