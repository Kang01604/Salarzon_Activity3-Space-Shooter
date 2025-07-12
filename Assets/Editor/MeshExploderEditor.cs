// MeshExploderEditor.cs
// IMPORTANT: This script MUST be placed in a folder named "Editor".
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SBS.ME
{
    [CustomEditor(typeof(MeshExploder))]
    public class MeshExploderEditor : Editor
    {
        private void OnEnable() { }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            MeshExploder REF = (MeshExploder)target;

            List<string> excluded = new List<string>();
            
            if (REF.createGameObjects == true)
            {
                excluded.Add("gravity");
                excluded.Add("friction");
            }
            else
            {
                excluded.Add("onPartsCreated");
                excluded.Add("probabilityOfCreatingAnObject");
                excluded.Add("colliderAttached");
            }

            if (REF.explosionOrigin != ExplosionOrigin.offset)
            {
                excluded.Add("ExplosionOffset");
            }

            if (REF.useNormalsAsExplosionDirection == true)
            {
                excluded.Add("explosionOrigin");
                excluded.Add("ExplosionOffset");
                excluded.Add("normalizeDirection");
            }

            if (REF.doubleSided == false)
            {
                excluded.Add("flipSideDistance");
            }

            DrawDefaultInspectorExcept(serializedObject, excluded);

            serializedObject.ApplyModifiedProperties();
        }
        
        public static void DrawDefaultInspectorExcept(SerializedObject serializedObject, List<string> excludedProperties)
        {
            SerializedProperty iterator = serializedObject.GetIterator();

            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (!excludedProperties.Contains(iterator.name) && iterator.name != "m_Script")
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }
        }
    }
}
#endif
