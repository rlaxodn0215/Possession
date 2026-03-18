#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace cowsins
{
    [CustomEditor(typeof(WeaponIdentification))]
    public class WeaponIdentificationInspector : Editor
    {

        private string[] tabs = { "Basic", "Attachments" };
        private int currentTab = 0;

        private WeaponIdentification wID;
        private bool hasCameraChild = false;
        private bool hasLightChild = false;
        private Animator[] animatorChildren;
        private bool animatorsFoldout = true;
        private bool customizationFoldout = false;

        private void OnEnable()
        {
            wID = (WeaponIdentification)target;
            hasCameraChild = wID.GetComponentInChildren<Camera>(true) != null;
            hasLightChild = wID.GetComponentInChildren<Light>(true) != null;
            animatorChildren = wID.GetComponentsInChildren<Animator>(true);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Texture2D myTexture = Resources.Load<Texture2D>("CustomEditor/weaponIdentification_CustomEditor") as Texture2D;
            GUILayout.Label(myTexture);

            if (hasCameraChild)
            {
                EditorGUILayout.HelpBox("A Camera has been found in this Weapon Prefab. This is not allowed.", MessageType.Error);
                GUILayout.Space(10);
            }
            if (hasLightChild)
            {
                EditorGUILayout.HelpBox("A Light Source has been found in this Weapon Prefab. Ignore this message if this is intentional.", MessageType.Warning);
                GUILayout.Space(10);
            }
            if (animatorChildren != null && animatorChildren.Length > 0)
            {
                EditorGUILayout.HelpBox($"Found {animatorChildren.Length} Animator(s) in children.", MessageType.Info);
                GUILayout.Space(10);
            }

            currentTab = GUILayout.Toolbar(currentTab, tabs);

            if (currentTab >= 0 || currentTab < tabs.Length)
            {
                switch (tabs[currentTab])
                {
                    case "Basic":
                        EditorGUILayout.Space(20f);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("weapon"));

                        EditorGUILayout.PropertyField(serializedObject.FindProperty("FirePoint"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("aimPoint"));
                        EditorGUILayout.Space(10f);
                        EditorGUILayout.LabelField("You can leave �headBone� unassigned if your camera does not move during your Weapon Animations.", EditorStyles.helpBox);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("headBone"));
                        if (animatorChildren != null && animatorChildren.Length > 0)
                        {
                            animatorsFoldout = EditorGUILayout.Foldout(animatorsFoldout, "Animators List", true);

                            if (animatorsFoldout)
                            {
                                EditorGUI.indentLevel++;

                                foreach (Animator animator in animatorChildren)
                                {
                                    EditorGUILayout.BeginHorizontal();

                                    EditorGUI.BeginDisabledGroup(true);
                                    // non-selectable
                                    EditorGUILayout.ObjectField(animator.gameObject.name, animator, typeof(Animator), true);
                                    EditorGUI.EndDisabledGroup();

                                    if (GUILayout.Button("See", GUILayout.Width(40)))
                                    {
                                        Selection.activeObject = animator.gameObject;
                                        EditorGUIUtility.PingObject(animator.gameObject);
                                    }

                                    EditorGUILayout.EndHorizontal();
                                }

                                EditorGUI.indentLevel--;
                            }
                        }

                        EditorGUILayout.Space(10f);
                        customizationFoldout = EditorGUILayout.Foldout(customizationFoldout, "Additional Customization", true);

                        if (customizationFoldout)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.HelpBox("Shell Eject Point is optional. If left empty, Fire Point will be used.", MessageType.Info);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("shellEjectPoint"));
                            EditorGUI.indentLevel--;
                        }
                        break;
                    case "Attachments":
                        EditorGUILayout.Space(5f);

                        GUILayout.BeginHorizontal();
                        CowsinsEditorWindowUtilities.DrawLinkCard(Resources.Load<Texture2D>("CustomEditor/CowsinsManager/documentationIcon"), "Documentation", "https://cowsinss-organization.gitbook.io/fps-engine-documentation/how-to-use/working-with-attachments", .77f, .4f);
                        GUILayout.FlexibleSpace();
                        CowsinsEditorWindowUtilities.DrawLinkCard(Resources.Load<Texture2D>("CustomEditor/CowsinsManager/tutorialsIcon"), "Tutorial", "https://www.cowsins.com/videos/1094068445", .77f, .4f);
                        GUILayout.FlexibleSpace();
                        CowsinsEditorWindowUtilities.DrawLinkCard(Resources.Load<Texture2D>("CustomEditor/CowsinsManager/supportIcon"), "Support", "https://discord.gg/759gSeTT9m", .77f, .4f);
                        GUILayout.Space(10);
                        GUILayout.EndHorizontal();

                        EditorGUILayout.Space(20f);
                        EditorGUILayout.HelpBox("Original / Default Attachments: Iron Sights, Original Magazines, etc...", MessageType.Info);
                        EditorGUILayout.Space(5f);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultAttachments").FindPropertyRelative("defaultAttachmentsList"));

                        EditorGUILayout.Space(10f);
                        EditorGUILayout.HelpBox("Define All Available Attachments for this Weapon, including Default Attachments.", MessageType.Info);
                        EditorGUILayout.Space(5f);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("compatibleAttachments").FindPropertyRelative("compatibleAttachmentsList"));

                        EditorGUILayout.Space(10f);
                        AutomaticAttachmentButton();

                        // Attachment State Visualization during the game
                        if (Application.isPlaying)
                        {
                            EditorGUILayout.Space(20f);
                            DrawRuntimeAttachmentState();
                        }

                        break;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AutomaticAttachmentButton()
        {
            if (GUILayout.Button("Automatically Assign Attachments", GUILayout.Height(35)))
            {
                var attachments = wID.GetComponentsInChildren<Attachment>(true);

                // Create a Temporary Dictionary for the found attachments and map them to their corresponding Attachment Type
                var groupedAttachments = new Dictionary<AttachmentType, List<Attachment>>();

                foreach (var attachment in attachments)
                {
                    var atcId = attachment.attachmentIdentifier;
                    if (atcId == null)
                    {
                        Debug.LogError($"<color=red>[COWSINS]</color> Attachment Identifier is null in {attachment}. Please, assign an attachment Identifier.", attachment);
                        continue;
                    }
                    var type = attachment.attachmentIdentifier.attachmentType;

                    if (!groupedAttachments.ContainsKey(type))
                        groupedAttachments[type] = new List<Attachment>();

                    groupedAttachments[type].Add(attachment);
                }

                // Gather Compatible Attachments 
                SerializedProperty compatibleListProp = serializedObject.FindProperty("compatibleAttachments")
                    .FindPropertyRelative("compatibleAttachmentsList");

                // Clear Compatible Attachments
                compatibleListProp.ClearArray();

                // Repopulate Compatible Attachments based on the Temporary Dictionary we just created
                int index = 0;
                foreach (var kvp in groupedAttachments)
                {
                    compatibleListProp.InsertArrayElementAtIndex(index);
                    SerializedProperty entryProp = compatibleListProp.GetArrayElementAtIndex(index);

                    entryProp.FindPropertyRelative("type").enumValueIndex = (int)kvp.Key;

                    var attachmentsArray = entryProp.FindPropertyRelative("attachments");
                    attachmentsArray.ClearArray();

                    for (int i = 0; i < kvp.Value.Count; i++)
                    {
                        attachmentsArray.InsertArrayElementAtIndex(i);
                        attachmentsArray.GetArrayElementAtIndex(i).objectReferenceValue = kvp.Value[i];
                    }

                    index++;
                }

                // Save the Changes
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(wID);

            }
        }

        // Shows current, default, and compatible attachments during runtime
        private void DrawRuntimeAttachmentState()
        {
            if (wID.AttachmentState == null) return;

            var state = wID.AttachmentState;

            // Calculate summary
            int totalSlots = System.Enum.GetValues(typeof(AttachmentType)).Length;
            int equippedCount = state.GetCurrentCount();
            int defaultCount = 0;
            int customCount = 0;

            foreach (AttachmentType type in System.Enum.GetValues(typeof(AttachmentType)))
            {
                var attachmentState = state.GetState(type);
                if (attachmentState == AttachmentState.Default) defaultCount++;
                else if (attachmentState == AttachmentState.Custom) customCount++;
            }

            int emptyCount = totalSlots - equippedCount;
            string summaryText = $"Attachments: {equippedCount}/{totalSlots} equipped ({defaultCount} default, {customCount} custom, {emptyCount} empty)";

            EditorGUILayout.LabelField("Runtime Attachment State", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(summaryText, MessageType.Info);

            EditorGUILayout.Space(5f);

            // Draw each attachment type
            foreach (AttachmentType type in System.Enum.GetValues(typeof(AttachmentType)))
            {
                DrawAttachmentTypeState(state, type);
            }

            EditorGUILayout.Space(10f);

            Repaint();
        }

        private void DrawAttachmentTypeState(AttachmentStateManager state, AttachmentType type)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var attachmentState = state.GetState(type);
            var current = state.GetCurrent(type);
            var compatibleCount = state.GetCompatibleCount(type);

            Color stateColor = attachmentState switch
            {
                AttachmentState.None => Color.gray,
                AttachmentState.Default => new Color(0.5f, 0.8f, 1f),
                AttachmentState.Custom => new Color(0.5f, 1f, 0.5f),
                _ => Color.white
            };

            var originalColor = GUI.color;
            GUI.color = stateColor;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"  {type}", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField($"{attachmentState}", GUILayout.Width(70));

            GUI.color = originalColor;

            // Show current attachment
            if (current != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(current, typeof(Attachment), true);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.LabelField($"Empty ({compatibleCount} compatible)", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

    }
}
#endif