﻿/*  This file is part of the "Simple Waypoint System" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace SWS
{
    /// <summary>
    /// Custom inspector for splineMove.
    /// <summary>
    [CustomEditor(typeof(splineMove))]
    public class splineMoveEditor : moveEditor
    {
        //called whenever the inspector gui gets rendered
        public override void OnInspectorGUI()
        {
            //this pulls the relative variables from unity runtime and stores them in the object
            m_Object.Update();
            //DrawDefaultInspector();

            //draw custom variable properties by using serialized properties
            EditorGUILayout.PropertyField(m_Object.FindProperty("pathContainer"));
            EditorGUILayout.PropertyField(m_Object.FindProperty("onStart"));
            EditorGUILayout.PropertyField(m_Object.FindProperty("moveToPath"));
            EditorGUILayout.PropertyField(m_Object.FindProperty("reverse"));
            EditorGUILayout.PropertyField(m_Object.FindProperty("local"));

            EditorGUILayout.PropertyField(m_Object.FindProperty("startPoint"));
            EditorGUILayout.PropertyField(m_Object.FindProperty("sizeToAdd"));
            EditorGUILayout.PropertyField(m_Object.FindProperty("speed"));

            SerializedProperty timeValue = m_Object.FindProperty("timeValue");
            EditorGUILayout.PropertyField(timeValue);
            if (timeValue.enumValueIndex == 0)
            {
                SerializedProperty easeType = m_Object.FindProperty("easeType");
                EditorGUILayout.PropertyField(easeType);
                if (easeType.enumValueIndex == 31)
                    EditorGUILayout.PropertyField(m_Object.FindProperty("animEaseType"));
            }

            SerializedProperty loopType = m_Object.FindProperty("loopType");
            EditorGUILayout.PropertyField(loopType);
            if (loopType.enumValueIndex == 1)
                EditorGUILayout.PropertyField(m_Object.FindProperty("closeLoop"));
            else
                m_Object.FindProperty("closeLoop").boolValue = false;

            EditorGUILayout.PropertyField(m_Object.FindProperty("pathType"));
            SerializedProperty orientToPath = m_Object.FindProperty("pathMode");
            EditorGUILayout.PropertyField(orientToPath);
            if (orientToPath.enumValueIndex != 0)
            {
                SerializedProperty lookAtPoint = m_Object.FindProperty("_lookAtPoint");
                EditorGUILayout.PropertyField(lookAtPoint);

                if (lookAtPoint.boolValue)
                    EditorGUILayout.PropertyField(m_Object.FindProperty("_lookAtTarget"));
                else
                    EditorGUILayout.PropertyField(m_Object.FindProperty("lookAhead"));

                EditorGUILayout.PropertyField(m_Object.FindProperty("lockRotation"));
            }

            EditorGUILayout.PropertyField(m_Object.FindProperty("lockPosition"));

            //get Path Manager component
            var path = GetPathTransform();
            EditorGUILayout.Space();
            EditorGUIUtility.LookLikeControls();
            GUILayout.Label("Settings:", EditorStyles.boldLabel);

            //check whether a Path Manager component is set, if not display a label
            if (path == null)
            {
                GUILayout.Label("No path set.");
                m_List.arraySize = 0;
            }
            else
            {
                //draw message options
                EventSettings();
            }

            //we push our modified variables back to our serialized object
            m_Object.ApplyModifiedProperties();
        }


        //if this path is selected, display small info boxes above all waypoint positions
        void OnSceneGUI()
        {
            //get Path Manager component
            var path = GetPathTransform();

            //do not execute further code if we have no path defined
            if (path == null) return;

            //get waypoints array of Path Manager
            var waypoints = path.waypoints;

            //begin GUI block
            Handles.BeginGUI();
            //loop through waypoint array
            for (int i = 0; i < waypoints.Length; i++)
            {
                //translate waypoint vector3 position in world space into a position on the screen
                var guiPoint = HandleUtility.WorldToGUIPoint(waypoints[i].transform.position);
                //create rectangle with that positions and do some offset
                var rect = new Rect(guiPoint.x - 50.0f, guiPoint.y - 60, 100, 20);
                //draw box at rect position with current waypoint name
                GUI.Box(rect, "Waypoint: " + i);
            }
            Handles.EndGUI(); //end GUI block
        }
    }
}
