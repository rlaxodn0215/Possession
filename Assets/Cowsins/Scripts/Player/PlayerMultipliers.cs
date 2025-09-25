using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace cowsins
{
    public class PlayerMultipliers : MonoBehaviour
    {
        [ReadOnly] public float damageMultiplier;
        [ReadOnly] public float healMultiplier;
        [ReadOnly] public float playerWeightMultiplier;

        private void Awake()
        {
            damageMultiplier = 1;
            healMultiplier = 1;
            playerWeightMultiplier = 1;
        }

    }
}

#if UNITY_EDITOR
namespace cowsins
{
    [CustomEditor(typeof(PlayerMultipliers))]
    public class PlayerMultipliersEditor : Editor
    {
        private bool showDebugInfo;
        override public void OnInspectorGUI()
        {
            serializedObject.Update();

            if (showDebugInfo)
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorGUILayout.HelpBox("The game is not running. Some values may not be applied until play mode.", MessageType.Warning);
                }
                DrawDefaultInspector();
            }
            if (GUILayout.Button(showDebugInfo ? "Hide Debug Information" : "Show Debug Information")) showDebugInfo = !showDebugInfo;
        }
    }
}
#endif
