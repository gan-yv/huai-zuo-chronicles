using System.Collections.Generic;
using Demo02.Narrative;
using UnityEditor;
using UnityEngine;

namespace Demo02.Editor
{
    [CustomEditor(typeof(CaseDefinition))]
    public sealed class CaseDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty caseId;
        private SerializedProperty title;
        private SerializedProperty chapterLabel;
        private SerializedProperty culpritEvidenceId;
        private SerializedProperty contentVersion;
        private SerializedProperty evidence;
        private SerializedProperty testimonies;
        private SerializedProperty contradiction;
        private SerializedProperty stageCopy;
        private SerializedProperty conclusionCopy;
        private SerializedProperty storyFacts;
        private SerializedProperty inventoryItems;
        private SerializedProperty dialogueScripts;

        private bool showMetadata = true;
        private bool showCaseContent = true;
        private bool showStoryContent = true;
        private bool showInventory = true;
        private bool showDialogue = true;

        private void OnEnable()
        {
            caseId = serializedObject.FindProperty("caseId");
            title = serializedObject.FindProperty("title");
            chapterLabel = serializedObject.FindProperty("chapterLabel");
            culpritEvidenceId = serializedObject.FindProperty("culpritEvidenceId");
            contentVersion = serializedObject.FindProperty("contentVersion");
            evidence = serializedObject.FindProperty("evidence");
            testimonies = serializedObject.FindProperty("testimonies");
            contradiction = serializedObject.FindProperty("contradiction");
            stageCopy = serializedObject.FindProperty("stageCopy");
            conclusionCopy = serializedObject.FindProperty("conclusionCopy");
            storyFacts = serializedObject.FindProperty("storyFacts");
            inventoryItems = serializedObject.FindProperty("inventoryItems");
            dialogueScripts = serializedObject.FindProperty("dialogueScripts");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            CaseDefinition content = (CaseDefinition)target;

            EditorGUILayout.HelpBox(
                "案件内容资产：线索、证词、目标、剧情事实和对白都在这里维护。"
                + "修改后使用“校验案件内容”，构建时也会自动校验。",
                MessageType.Info
            );

            showMetadata = EditorGUILayout.Foldout(
                showMetadata,
                "案件信息",
                toggleOnLabelClick: true
            );
            if (showMetadata)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(caseId, new GUIContent("案件 ID"));
                EditorGUILayout.PropertyField(title, new GUIContent("标题"));
                EditorGUILayout.PropertyField(
                    chapterLabel,
                    new GUIContent("章节标签")
                );
                EditorGUILayout.PropertyField(
                    culpritEvidenceId,
                    new GUIContent("关键人物证据 ID")
                );
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField(
                        "内容版本",
                        contentVersion.intValue
                    );
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(8f);
            DrawSummary(content);
            EditorGUILayout.Space(8f);

            showCaseContent = EditorGUILayout.Foldout(
                showCaseContent,
                "证据、证词与目标",
                toggleOnLabelClick: true
            );
            if (showCaseContent)
            {
                DrawProperty(evidence, "证据");
                DrawProperty(testimonies, "证词");
                DrawProperty(contradiction, "矛盾规则");
                DrawProperty(stageCopy, "阶段目标与推理");
                DrawProperty(conclusionCopy, "结局文案");
            }

            showStoryContent = EditorGUILayout.Foldout(
                showStoryContent,
                "剧情事实",
                toggleOnLabelClick: true
            );
            if (showStoryContent)
            {
                DrawProperty(storyFacts, "可持久化事实");
            }

            showInventory = EditorGUILayout.Foldout(
                showInventory,
                "背包物品",
                toggleOnLabelClick: true
            );
            if (showInventory)
            {
                DrawProperty(inventoryItems, "物品定义");
            }

            showDialogue = EditorGUILayout.Foldout(
                showDialogue,
                "对白脚本",
                toggleOnLabelClick: true
            );
            if (showDialogue)
            {
                DrawProperty(dialogueScripts, "按条件匹配的对白");
            }

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space(10f);

            if (GUILayout.Button("校验案件内容", GUILayout.Height(32f)))
            {
                AssetDatabase.SaveAssets();
                CaseDefinitionSeeder.ValidateOrThrow();
            }

            List<string> errors = content.Validate();
            if (errors.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    string.Join("\n", errors),
                    MessageType.Error
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "内容校验通过，可以进入场景重建或 Windows 构建。",
                    MessageType.None
                );
            }
        }

        private static void DrawSummary(CaseDefinition content)
        {
            EditorGUILayout.LabelField("内容概览", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(
                $"证据 {content.Evidence.Count}  ·  "
                + $"证词 {content.Testimonies.Count}  ·  "
                + $"剧情事实 {content.StoryFacts.Count}  ·  "
                + $"物品 {content.InventoryItems.Count}  ·  "
                + $"对白脚本 {content.DialogueScripts.Count}"
            );
            EditorGUILayout.LabelField(
                $"内容版本 {content.ContentVersion}"
            );
            EditorGUI.indentLevel--;
        }

        private static void DrawProperty(
            SerializedProperty property,
            string label)
        {
            if (property == null)
            {
                EditorGUILayout.HelpBox(
                    $"找不到序列化字段：{label}",
                    MessageType.Warning
                );
                return;
            }

            EditorGUILayout.PropertyField(
                property,
                new GUIContent(label),
                includeChildren: true
            );
        }
    }
}
