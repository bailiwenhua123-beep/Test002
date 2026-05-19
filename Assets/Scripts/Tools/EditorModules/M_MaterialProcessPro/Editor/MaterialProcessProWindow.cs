using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace M_MaterialProcessPro
{
    /// <summary>
    /// 新版模型材质处理窗口。
    /// 主要解决两个工作流：
    /// 1. 按模型源材质名，把项目中的真实材质 Remap 到 FBX/模型导入器。
    /// 2. 按“材质名 + 贴图后缀”的规则，把贴图批量赋给材质。
    /// </summary>
    public class MaterialProcessProWindow : EditorWindow
    {
        private const int MaxPreviewRows = 300;

        private static readonly string[] MainTabNames = { "模型赋材质", "材质贴图", "使用说明" };
        private static readonly string[] TargetModeNames = { "当前选择", "指定文件夹" };
        private static readonly string[] TexturePresetNames = { "普通/内置", "URP", "HDRP", "自定义" };

        private enum TexturePreset
        {
            BuiltIn = 0,
            URP = 1,
            HDRP = 2,
            Custom = 3
        }

        [Serializable]
        private class TextureRule
        {
            public bool enabled;
            public string ruleName;
            public string propertyName;
            public string suffixes;
            public bool normalMap;

            public TextureRule(bool enabled, string ruleName, string propertyName, string suffixes, bool normalMap)
            {
                this.enabled = enabled;
                this.ruleName = ruleName;
                this.propertyName = propertyName;
                this.suffixes = suffixes;
                this.normalMap = normalMap;
            }
        }

        private class MaterialRecord
        {
            public string name;
            public string path;
            public Material material;
        }

        private class TextureRecord
        {
            public string name;
            public string path;
            public Texture texture;
        }

        private class ModelPreviewRow
        {
            public string modelPath;
            public string sourceMaterialName;
            public Material targetMaterial;
            public bool matched;
            public string message;
        }

        private class TexturePreviewRow
        {
            public string materialName;
            public string ruleName;
            public string propertyName;
            public Texture targetTexture;
            public bool canApply;
            public string message;
        }

        private int currentTab;
        private int modelTargetMode = 1;
        private int textureTargetMode = 1;
        private Vector2 scrollPosition;

        private DefaultAsset materialLibraryFolder;
        private DefaultAsset modelFolder;
        private DefaultAsset textureFolder;
        private DefaultAsset textureMaterialFolder;

        private bool includeSubFolders = true;
        private bool ignoreCase = true;
        private bool autoSetNormalTextureType = true;
        private TexturePreset texturePreset = TexturePreset.URP;
        private List<TextureRule> textureRules = new List<TextureRule>();

        private readonly Dictionary<string, Material> materialMap = new Dictionary<string, Material>();
        private readonly Dictionary<string, Texture> textureMap = new Dictionary<string, Texture>();
        private readonly List<string> duplicateMaterialNames = new List<string>();
        private readonly List<string> duplicateTextureNames = new List<string>();
        private readonly List<ModelPreviewRow> modelPreviewRows = new List<ModelPreviewRow>();
        private readonly List<TexturePreviewRow> texturePreviewRows = new List<TexturePreviewRow>();

        private string statusMessage = "";
        private MessageType statusType = MessageType.None;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle sectionTitleStyle;
        private GUIStyle smallLabelStyle;

        [MenuItem("模型工具/材质赋值工具 Pro")]
        private static void OpenWindow()
        {
            MaterialProcessProWindow window = GetWindow<MaterialProcessProWindow>("材质赋值工具 Pro");
            window.minSize = new Vector2(860f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            // 第一次打开窗口时给一套默认规则，避免用户必须先手动配置才能使用。
            if (textureRules == null || textureRules.Count == 0)
            {
                ResetTextureRules();
            }
        }

        private void OnGUI()
        {
            InitStyles();

            DrawHeader();
            DrawSharedOptions();

            currentTab = GUILayout.Toolbar(currentTab, MainTabNames, GUILayout.Height(28f));
            EditorGUILayout.Space(6f);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            switch (currentTab)
            {
                case 0:
                    DrawModelMaterialPage();
                    break;
                case 1:
                    DrawTextureMaterialPage();
                    break;
                default:
                    DrawHelpPage();
                    break;
            }
            EditorGUILayout.EndScrollView();
        }

        private void InitStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft
            };

            subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };

            sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };

            smallLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true
            };
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("材质赋值工具 Pro", titleStyle);
            EditorGUILayout.LabelField("先扫描、再预览、最后执行。所有批处理都会先按名称匹配，减少误操作。", subtitleStyle);
            EditorGUILayout.EndVertical();
            DrawStatus();
        }

        private void DrawSharedOptions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("通用匹配设置", sectionTitleStyle);
            EditorGUILayout.BeginHorizontal();
            includeSubFolders = EditorGUILayout.ToggleLeft("包含子文件夹", includeSubFolders, GUILayout.Width(130f));
            ignoreCase = EditorGUILayout.ToggleLeft("忽略大小写", ignoreCase, GUILayout.Width(130f));
            autoSetNormalTextureType = EditorGUILayout.ToggleLeft("法线贴图自动设为 NormalMap", autoSetNormalTextureType, GUILayout.Width(220f));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }
        }

        private void DrawModelMaterialPage()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("1. 材质库", sectionTitleStyle);
            materialLibraryFolder = DrawFolderField("材质库文件夹", materialLibraryFolder, "扫描此目录下的 .mat 和可被识别为 Material 的子资源。");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("扫描材质库", GUILayout.Height(28f), GUILayout.Width(120f)))
            {
                ScanMaterialLibrary();
            }
            DrawCounter("已加载材质", materialMap.Count);
            DrawCounter("重名材质", duplicateMaterialNames.Count);
            EditorGUILayout.EndHorizontal();

            DrawDuplicateList("材质重名提示", duplicateMaterialNames);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("2. 目标模型", sectionTitleStyle);
            modelTargetMode = GUILayout.Toolbar(modelTargetMode, TargetModeNames, GUILayout.Width(240f));
            if (modelTargetMode == 1)
            {
                modelFolder = DrawFolderField("模型文件夹", modelFolder, "扫描此目录下由 ModelImporter 管理的模型资源，例如 FBX。");
            }
            else
            {
                EditorGUILayout.HelpBox("当前选择模式会读取 Project 面板里选中的模型资源；如果选中的是文件夹，会读取该文件夹内的模型。", MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("预览匹配", GUILayout.Height(32f), GUILayout.Width(120f)))
            {
                BuildModelPreview();
            }
            if (GUILayout.Button("执行模型赋材质", GUILayout.Height(32f), GUILayout.Width(150f)))
            {
                ApplyMaterialRemapToModels();
            }
            DrawCounter("预览条目", modelPreviewRows.Count);
            DrawCounter("可匹配", modelPreviewRows.Count(row => row.matched));
            DrawCounter("缺失", modelPreviewRows.Count(row => !row.matched));
            EditorGUILayout.EndHorizontal();

            DrawModelPreviewRows();
            EditorGUILayout.EndVertical();
        }

        private void DrawTextureMaterialPage()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("1. 贴图库", sectionTitleStyle);
            textureFolder = DrawFolderField("贴图文件夹", textureFolder, "扫描此目录下的 Texture 资源。");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("扫描贴图库", GUILayout.Height(28f), GUILayout.Width(120f)))
            {
                ScanTextureLibrary();
            }
            DrawCounter("已加载贴图", textureMap.Count);
            DrawCounter("重名贴图", duplicateTextureNames.Count);
            EditorGUILayout.EndHorizontal();

            DrawDuplicateList("贴图重名提示", duplicateTextureNames);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("2. 贴图规则", sectionTitleStyle);
            EditorGUILayout.LabelField("匹配方式：材质名 + 贴图后缀。多个后缀用分号、逗号或竖线分隔。", smallLabelStyle);

            EditorGUI.BeginChangeCheck();
            int presetIndex = GUILayout.Toolbar((int)texturePreset, TexturePresetNames, GUILayout.Width(360f));
            if (EditorGUI.EndChangeCheck())
            {
                texturePreset = (TexturePreset)presetIndex;
                ResetTextureRules();
            }

            DrawTextureRules();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("3. 目标材质", sectionTitleStyle);
            textureTargetMode = GUILayout.Toolbar(textureTargetMode, TargetModeNames, GUILayout.Width(240f));
            if (textureTargetMode == 1)
            {
                textureMaterialFolder = DrawFolderField("目标材质文件夹", textureMaterialFolder, "需要被赋贴图的材质所在目录。");
            }
            else
            {
                EditorGUILayout.HelpBox("当前选择模式会读取 Project 面板里选中的材质资源；如果选中的是文件夹，会读取该文件夹内的材质。", MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("预览贴图赋值", GUILayout.Height(32f), GUILayout.Width(130f)))
            {
                BuildTexturePreview();
            }
            if (GUILayout.Button("执行贴图赋值", GUILayout.Height(32f), GUILayout.Width(130f)))
            {
                ApplyTexturesToMaterials();
            }
            DrawCounter("预览条目", texturePreviewRows.Count);
            DrawCounter("可赋值", texturePreviewRows.Count(row => row.canApply));
            DrawCounter("跳过", texturePreviewRows.Count(row => !row.canApply));
            EditorGUILayout.EndHorizontal();

            DrawTexturePreviewRows();
            EditorGUILayout.EndVertical();
        }

        private void DrawHelpPage()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("模型赋材质流程", sectionTitleStyle);
            EditorGUILayout.LabelField("1. 把项目里的材质放在一个材质库目录中，材质名需要和模型源材质名一致。", subtitleStyle);
            EditorGUILayout.LabelField("2. 点击“扫描材质库”，再选择模型来源，点击“预览匹配”。", subtitleStyle);
            EditorGUILayout.LabelField("3. 确认可匹配条目正常后，点击“执行模型赋材质”。工具会写入 ModelImporter 的 Remap 设置并重新导入模型。", subtitleStyle);
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("材质贴图流程", sectionTitleStyle);
            EditorGUILayout.LabelField("1. 贴图命名按“材质名 + 后缀”，例如材质 Pump_01 对应贴图 Pump_01_BaseMap。", subtitleStyle);
            EditorGUILayout.LabelField("2. 选择普通/URP/HDRP 预设后，可以继续改 Shader 属性名和贴图后缀。", subtitleStyle);
            EditorGUILayout.LabelField("3. 先预览，确认 Shader 属性存在且贴图匹配后，再执行批量赋值。", subtitleStyle);
            EditorGUILayout.EndVertical();
        }

        private DefaultAsset DrawFolderField(string label, DefaultAsset currentValue, string tooltip)
        {
            EditorGUILayout.BeginHorizontal();
            currentValue = EditorGUILayout.ObjectField(new GUIContent(label, tooltip), currentValue, typeof(DefaultAsset), false) as DefaultAsset;

            if (GUILayout.Button("用当前选择", GUILayout.Width(96f)))
            {
                DefaultAsset selectedFolder = GetSelectedFolderAsset();
                if (selectedFolder != null)
                {
                    currentValue = selectedFolder;
                }
                else
                {
                    SetStatus("当前 Project 选择不是有效文件夹。", MessageType.Warning);
                }
            }
            EditorGUILayout.EndHorizontal();

            string path = GetFolderPath(currentValue);
            if (!string.IsNullOrEmpty(path))
            {
                EditorGUILayout.LabelField("路径：" + path, smallLabelStyle);
            }
            return currentValue;
        }

        private void DrawCounter(string label, int value)
        {
            EditorGUILayout.LabelField(label + "：" + value, GUILayout.Width(120f));
        }

        private void DrawDuplicateList(string title, List<string> duplicates)
        {
            if (duplicates.Count == 0)
            {
                return;
            }

            int showCount = Mathf.Min(duplicates.Count, 8);
            string content = string.Join("\n", duplicates.Take(showCount).ToArray());
            if (duplicates.Count > showCount)
            {
                content += "\n...";
            }
            EditorGUILayout.HelpBox(title + "：\n" + content, MessageType.Warning);
        }

        private void DrawTextureRules()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("启用", GUILayout.Width(42f));
            EditorGUILayout.LabelField("规则名", GUILayout.Width(100f));
            EditorGUILayout.LabelField("Shader属性", GUILayout.Width(145f));
            EditorGUILayout.LabelField("贴图后缀", GUILayout.MinWidth(220f));
            EditorGUILayout.LabelField("法线", GUILayout.Width(46f));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < textureRules.Count; i++)
            {
                TextureRule rule = textureRules[i];
                EditorGUILayout.BeginHorizontal();
                rule.enabled = EditorGUILayout.Toggle(rule.enabled, GUILayout.Width(42f));
                rule.ruleName = EditorGUILayout.TextField(rule.ruleName, GUILayout.Width(100f));
                rule.propertyName = EditorGUILayout.TextField(rule.propertyName, GUILayout.Width(145f));
                rule.suffixes = EditorGUILayout.TextField(rule.suffixes, GUILayout.MinWidth(220f));
                rule.normalMap = EditorGUILayout.Toggle(rule.normalMap, GUILayout.Width(46f));
                if (GUILayout.Button("删", GUILayout.Width(34f)))
                {
                    textureRules.RemoveAt(i);
                    texturePreset = TexturePreset.Custom;
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("新增规则", GUILayout.Width(90f)))
            {
                textureRules.Add(new TextureRule(true, "自定义", "_BaseMap", "_BaseMap", false));
                texturePreset = TexturePreset.Custom;
            }
            if (GUILayout.Button("重置当前预设", GUILayout.Width(110f)))
            {
                ResetTextureRules();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawModelPreviewRows()
        {
            if (modelPreviewRows.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无模型匹配预览。", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("模型匹配预览", sectionTitleStyle);
            DrawPreviewLimitHint(modelPreviewRows.Count);

            int count = Mathf.Min(modelPreviewRows.Count, MaxPreviewRows);
            for (int i = 0; i < count; i++)
            {
                ModelPreviewRow row = modelPreviewRows[i];
                Color oldColor = GUI.color;
                GUI.color = row.matched ? new Color(0.75f, 1f, 0.75f) : new Color(1f, 0.78f, 0.72f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.color = oldColor;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(row.matched ? "匹配" : "缺失", GUILayout.Width(46f));
                EditorGUILayout.LabelField(row.sourceMaterialName, GUILayout.Width(180f));
                EditorGUILayout.ObjectField(row.targetMaterial, typeof(Material), false, GUILayout.Width(220f));
                EditorGUILayout.LabelField(row.message, smallLabelStyle);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField(row.modelPath, smallLabelStyle);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawTexturePreviewRows()
        {
            if (texturePreviewRows.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无贴图赋值预览。", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("贴图赋值预览", sectionTitleStyle);
            DrawPreviewLimitHint(texturePreviewRows.Count);

            int count = Mathf.Min(texturePreviewRows.Count, MaxPreviewRows);
            for (int i = 0; i < count; i++)
            {
                TexturePreviewRow row = texturePreviewRows[i];
                Color oldColor = GUI.color;
                GUI.color = row.canApply ? new Color(0.75f, 1f, 0.75f) : new Color(1f, 0.78f, 0.72f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.color = oldColor;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(row.canApply ? "可赋值" : "跳过", GUILayout.Width(56f));
                EditorGUILayout.LabelField(row.materialName, GUILayout.Width(160f));
                EditorGUILayout.LabelField(row.ruleName + " / " + row.propertyName, GUILayout.Width(180f));
                EditorGUILayout.ObjectField(row.targetTexture, typeof(Texture), false, GUILayout.Width(220f));
                EditorGUILayout.LabelField(row.message, smallLabelStyle);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawPreviewLimitHint(int totalCount)
        {
            if (totalCount > MaxPreviewRows)
            {
                EditorGUILayout.HelpBox("预览条目较多，窗口只显示前 " + MaxPreviewRows + " 条，执行时仍会处理全部条目。", MessageType.Info);
            }
        }

        private void ScanMaterialLibrary()
        {
            materialMap.Clear();
            duplicateMaterialNames.Clear();

            string folderPath;
            if (!TryGetFolderPath(materialLibraryFolder, "材质库文件夹", out folderPath))
            {
                return;
            }

            List<MaterialRecord> records = LoadMaterialsFromFolder(folderPath);
            foreach (MaterialRecord record in records)
            {
                string key = BuildNameKey(record.name);
                if (materialMap.ContainsKey(key))
                {
                    duplicateMaterialNames.Add(record.name + "  |  " + record.path);
                    continue;
                }
                materialMap.Add(key, record.material);
            }

            SetStatus("材质库扫描完成，加载材质 " + materialMap.Count + " 个。", MessageType.Info);
        }

        private void ScanTextureLibrary()
        {
            textureMap.Clear();
            duplicateTextureNames.Clear();

            string folderPath;
            if (!TryGetFolderPath(textureFolder, "贴图文件夹", out folderPath))
            {
                return;
            }

            List<TextureRecord> records = LoadTexturesFromFolder(folderPath);
            foreach (TextureRecord record in records)
            {
                string key = BuildNameKey(record.name);
                if (textureMap.ContainsKey(key))
                {
                    duplicateTextureNames.Add(record.name + "  |  " + record.path);
                    continue;
                }
                textureMap.Add(key, record.texture);
            }

            SetStatus("贴图库扫描完成，加载贴图 " + textureMap.Count + " 张。", MessageType.Info);
        }

        private void BuildModelPreview()
        {
            ScanMaterialLibrary();
            modelPreviewRows.Clear();

            List<string> modelPaths = CollectModelPaths();
            if (modelPaths.Count == 0)
            {
                SetStatus("没有找到可处理的模型资源。", MessageType.Warning);
                return;
            }

            foreach (string modelPath in modelPaths)
            {
                ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                if (importer == null)
                {
                    continue;
                }

                List<AssetImporter.SourceAssetIdentifier> sourceMaterials = GetSourceMaterialIdentifiers(importer, modelPath);
                if (sourceMaterials.Count == 0)
                {
                    modelPreviewRows.Add(new ModelPreviewRow
                    {
                        modelPath = modelPath,
                        sourceMaterialName = "(未读取到源材质)",
                        matched = false,
                        message = "模型导入器没有暴露源材质信息"
                    });
                    continue;
                }

                foreach (AssetImporter.SourceAssetIdentifier sourceMaterial in sourceMaterials)
                {
                    Material targetMaterial;
                    bool matched = materialMap.TryGetValue(BuildNameKey(sourceMaterial.name), out targetMaterial);
                    modelPreviewRows.Add(new ModelPreviewRow
                    {
                        modelPath = modelPath,
                        sourceMaterialName = sourceMaterial.name,
                        targetMaterial = targetMaterial,
                        matched = matched,
                        message = matched ? "将写入 Remap" : "材质库中未找到同名材质"
                    });
                }
            }

            SetStatus("模型匹配预览完成。", MessageType.Info);
        }

        private void ApplyMaterialRemapToModels()
        {
            BuildModelPreview();
            if (modelPreviewRows.Count == 0)
            {
                return;
            }

            int changedRemaps = 0;
            int changedModels = 0;
            List<string> modelPaths = modelPreviewRows.Select(row => row.modelPath).Distinct().ToList();

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < modelPaths.Count; i++)
                {
                    string modelPath = modelPaths[i];
                    EditorUtility.DisplayProgressBar("模型赋材质", modelPath, (float)i / Mathf.Max(1, modelPaths.Count));

                    ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                    if (importer == null)
                    {
                        continue;
                    }

                    Dictionary<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> externalMap = importer.GetExternalObjectMap();
                    bool modelChanged = false;

                    List<AssetImporter.SourceAssetIdentifier> sourceMaterials = GetSourceMaterialIdentifiers(importer, modelPath);
                    foreach (AssetImporter.SourceAssetIdentifier sourceMaterial in sourceMaterials)
                    {
                        Material targetMaterial;
                        if (!materialMap.TryGetValue(BuildNameKey(sourceMaterial.name), out targetMaterial))
                        {
                            continue;
                        }

                        UnityEngine.Object currentRemap;
                        bool alreadyMapped = externalMap.TryGetValue(sourceMaterial, out currentRemap) && currentRemap == targetMaterial;
                        if (alreadyMapped)
                        {
                            continue;
                        }

                        // 只对能匹配到同名材质的源材质写入 Remap，避免把无关材质塞进模型导入器。
                        importer.AddRemap(sourceMaterial, targetMaterial);
                        modelChanged = true;
                        changedRemaps++;
                    }

                    if (modelChanged)
                    {
                        changedModels++;
                        importer.SaveAndReimport();
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            SetStatus("模型赋材质完成：修改模型 " + changedModels + " 个，写入 Remap " + changedRemaps + " 条。", MessageType.Info);
        }

        private void BuildTexturePreview()
        {
            ScanTextureLibrary();
            texturePreviewRows.Clear();

            List<Material> targetMaterials = CollectTextureTargetMaterials();
            if (targetMaterials.Count == 0)
            {
                SetStatus("没有找到需要赋贴图的材质。", MessageType.Warning);
                return;
            }

            foreach (Material material in targetMaterials)
            {
                foreach (TextureRule rule in textureRules)
                {
                    if (!rule.enabled)
                    {
                        continue;
                    }

                    Texture texture = FindTextureForRule(material, rule);
                    bool hasProperty = !string.IsNullOrEmpty(rule.propertyName) && material.HasProperty(rule.propertyName);
                    bool canApply = texture != null && hasProperty;

                    string message;
                    if (texture == null)
                    {
                        message = "未找到匹配贴图";
                    }
                    else if (!hasProperty)
                    {
                        message = "当前 Shader 没有这个属性";
                    }
                    else
                    {
                        message = "可写入材质";
                    }

                    texturePreviewRows.Add(new TexturePreviewRow
                    {
                        materialName = material.name,
                        ruleName = rule.ruleName,
                        propertyName = rule.propertyName,
                        targetTexture = texture,
                        canApply = canApply,
                        message = message
                    });
                }
            }

            SetStatus("贴图赋值预览完成。", MessageType.Info);
        }

        private void ApplyTexturesToMaterials()
        {
            BuildTexturePreview();

            List<Material> targetMaterials = CollectTextureTargetMaterials();
            if (targetMaterials.Count == 0)
            {
                return;
            }

            int changedCount = 0;
            int skippedCount = 0;

            try
            {
                for (int i = 0; i < targetMaterials.Count; i++)
                {
                    Material material = targetMaterials[i];
                    EditorUtility.DisplayProgressBar("材质贴图赋值", material.name, (float)i / Mathf.Max(1, targetMaterials.Count));

                    foreach (TextureRule rule in textureRules)
                    {
                        if (!rule.enabled)
                        {
                            continue;
                        }

                        Texture texture = FindTextureForRule(material, rule);
                        if (texture == null || string.IsNullOrEmpty(rule.propertyName) || !material.HasProperty(rule.propertyName))
                        {
                            skippedCount++;
                            continue;
                        }

                        if (rule.normalMap && autoSetNormalTextureType)
                        {
                            texture = EnsureNormalTexture(texture);
                        }

                        Undo.RecordObject(material, "批量赋贴图");
                        Texture oldTexture = material.GetTexture(rule.propertyName);
                        if (oldTexture != texture)
                        {
                            material.SetTexture(rule.propertyName, texture);
                            EnableRelatedKeyword(material, rule);
                            EditorUtility.SetDirty(material);
                            changedCount++;
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SetStatus("贴图赋值完成：写入 " + changedCount + " 项，跳过 " + skippedCount + " 项。", MessageType.Info);
        }

        private List<MaterialRecord> LoadMaterialsFromFolder(string folderPath)
        {
            List<MaterialRecord> records = new List<MaterialRecord>();
            HashSet<string> scannedPaths = new HashSet<string>();
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsPathInSearchScope(assetPath, folderPath) || !scannedPaths.Add(assetPath))
                {
                    continue;
                }

                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (UnityEngine.Object asset in assets)
                {
                    Material material = asset as Material;
                    if (material == null)
                    {
                        continue;
                    }

                    records.Add(new MaterialRecord
                    {
                        name = material.name,
                        path = assetPath,
                        material = material
                    });
                }
            }

            records.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
            return records;
        }

        private List<TextureRecord> LoadTexturesFromFolder(string folderPath)
        {
            List<TextureRecord> records = new List<TextureRecord>();
            HashSet<string> scannedPaths = new HashSet<string>();
            string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { folderPath });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsPathInSearchScope(assetPath, folderPath) || !scannedPaths.Add(assetPath))
                {
                    continue;
                }

                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (UnityEngine.Object asset in assets)
                {
                    Texture texture = asset as Texture;
                    if (texture == null)
                    {
                        continue;
                    }

                    records.Add(new TextureRecord
                    {
                        name = texture.name,
                        path = assetPath,
                        texture = texture
                    });
                }
            }

            records.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
            return records;
        }

        private List<string> CollectModelPaths()
        {
            HashSet<string> paths = new HashSet<string>();

            if (modelTargetMode == 1)
            {
                string folderPath;
                if (!TryGetFolderPath(modelFolder, "模型文件夹", out folderPath))
                {
                    return paths.ToList();
                }

                AddModelPathsFromFolder(folderPath, paths);
            }
            else
            {
                foreach (UnityEngine.Object selectedObject in Selection.objects)
                {
                    string assetPath = AssetDatabase.GetAssetPath(selectedObject);
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        continue;
                    }

                    if (AssetDatabase.IsValidFolder(assetPath))
                    {
                        AddModelPathsFromFolder(assetPath, paths);
                    }
                    else if (AssetImporter.GetAtPath(assetPath) is ModelImporter)
                    {
                        paths.Add(assetPath);
                    }
                }
            }

            List<string> result = paths.ToList();
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private void AddModelPathsFromFolder(string folderPath, HashSet<string> paths)
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (IsPathInSearchScope(assetPath, folderPath) && AssetImporter.GetAtPath(assetPath) is ModelImporter)
                {
                    paths.Add(assetPath);
                }
            }
        }

        private List<Material> CollectTextureTargetMaterials()
        {
            HashSet<Material> materials = new HashSet<Material>();

            if (textureTargetMode == 1)
            {
                string folderPath;
                if (!TryGetFolderPath(textureMaterialFolder, "目标材质文件夹", out folderPath))
                {
                    return materials.ToList();
                }

                foreach (MaterialRecord record in LoadMaterialsFromFolder(folderPath))
                {
                    materials.Add(record.material);
                }
            }
            else
            {
                foreach (UnityEngine.Object selectedObject in Selection.objects)
                {
                    string assetPath = AssetDatabase.GetAssetPath(selectedObject);
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        continue;
                    }

                    if (AssetDatabase.IsValidFolder(assetPath))
                    {
                        foreach (MaterialRecord record in LoadMaterialsFromFolder(assetPath))
                        {
                            materials.Add(record.material);
                        }
                    }
                    else
                    {
                        Material material = selectedObject as Material;
                        if (material == null)
                        {
                            material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                        }
                        if (material != null)
                        {
                            materials.Add(material);
                        }
                    }
                }
            }

            List<Material> result = materials.ToList();
            result.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        private List<AssetImporter.SourceAssetIdentifier> GetSourceMaterialIdentifiers(ModelImporter importer, string modelPath)
        {
            List<AssetImporter.SourceAssetIdentifier> identifiers = new List<AssetImporter.SourceAssetIdentifier>();

            // Unity 版本差异较多，这里用反射读取 sourceMaterials，避免因为 API 暴露差异导致工具无法编译。
            PropertyInfo propertyInfo = typeof(ModelImporter).GetProperty("sourceMaterials", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (propertyInfo != null)
            {
                object value = propertyInfo.GetValue(importer, null);
                AssetImporter.SourceAssetIdentifier[] sourceMaterials = value as AssetImporter.SourceAssetIdentifier[];
                if (sourceMaterials != null)
                {
                    identifiers.AddRange(sourceMaterials.Where(item => item.type == typeof(Material) && !string.IsNullOrEmpty(item.name)));
                }
            }

            if (identifiers.Count > 0)
            {
                return RemoveDuplicateIdentifiers(identifiers);
            }

            // 反射读取不到时，退回到模型子资源中的材质名；这比把材质库全部写入每个模型更可控。
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
            foreach (UnityEngine.Object asset in assets)
            {
                Material material = asset as Material;
                if (material == null)
                {
                    continue;
                }

                identifiers.Add(new AssetImporter.SourceAssetIdentifier
                {
                    name = material.name,
                    type = typeof(Material)
                });
            }

            return RemoveDuplicateIdentifiers(identifiers);
        }

        private List<AssetImporter.SourceAssetIdentifier> RemoveDuplicateIdentifiers(List<AssetImporter.SourceAssetIdentifier> identifiers)
        {
            List<AssetImporter.SourceAssetIdentifier> result = new List<AssetImporter.SourceAssetIdentifier>();
            HashSet<string> names = new HashSet<string>();
            foreach (AssetImporter.SourceAssetIdentifier identifier in identifiers)
            {
                string key = BuildNameKey(identifier.name);
                if (names.Add(key))
                {
                    result.Add(identifier);
                }
            }
            return result;
        }

        private Texture FindTextureForRule(Material material, TextureRule rule)
        {
            string[] suffixes = SplitSuffixes(rule.suffixes);
            foreach (string suffix in suffixes)
            {
                Texture texture;
                string textureName = material.name + suffix;
                if (textureMap.TryGetValue(BuildNameKey(textureName), out texture))
                {
                    return texture;
                }
            }
            return null;
        }

        private Texture EnsureNormalTexture(Texture texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType == TextureImporterType.NormalMap)
            {
                return texture;
            }

            // 法线贴图必须使用 NormalMap 导入类型，否则 Unity 的法线采样会不正确。
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();

            Texture reloadedTexture = AssetDatabase.LoadAssetAtPath<Texture>(path);
            return reloadedTexture != null ? reloadedTexture : texture;
        }

        private void EnableRelatedKeyword(Material material, TextureRule rule)
        {
            if (rule.normalMap)
            {
                material.EnableKeyword("_NORMALMAP");
                if (material.HasProperty("_BumpScale"))
                {
                    material.SetFloat("_BumpScale", 1f);
                }
            }

            if (rule.propertyName == "_MetallicGlossMap")
            {
                material.EnableKeyword("_METALLICGLOSSMAP");
            }
            else if (rule.propertyName == "_SpecGlossMap")
            {
                material.EnableKeyword("_SPECGLOSSMAP");
            }
        }

        private void ResetTextureRules()
        {
            textureRules = new List<TextureRule>();
            switch (texturePreset)
            {
                case TexturePreset.BuiltIn:
                    textureRules.Add(new TextureRule(true, "主贴图", "_MainTex", "_MainTex;_Albedo;_BaseColor;_BaseMap", false));
                    textureRules.Add(new TextureRule(true, "金属度", "_MetallicGlossMap", "_MetallicGlossMap;_Metallic", false));
                    textureRules.Add(new TextureRule(true, "法线", "_BumpMap", "_BumpMap;_Normal;_NormalMap", true));
                    break;
                case TexturePreset.URP:
                    textureRules.Add(new TextureRule(true, "基础色", "_BaseMap", "_BaseMap;_BaseColor;_Albedo", false));
                    textureRules.Add(new TextureRule(true, "金属度", "_MetallicGlossMap", "_MetallicGlossMap;_Metallic", false));
                    textureRules.Add(new TextureRule(true, "高光", "_SpecGlossMap", "_SpecGlossMap;_Specular", false));
                    textureRules.Add(new TextureRule(true, "法线", "_BumpMap", "_BumpMap;_Normal;_NormalMap", true));
                    break;
                case TexturePreset.HDRP:
                    textureRules.Add(new TextureRule(true, "基础色", "_BaseColorMap", "_BaseColorMap;_BaseMap;_BaseColor", false));
                    textureRules.Add(new TextureRule(true, "遮罩", "_MaskMap", "_MaskMap;_Mask", false));
                    textureRules.Add(new TextureRule(true, "法线", "_NormalMap", "_NormalMap;_Normal;_BumpMap", true));
                    break;
                case TexturePreset.Custom:
                    textureRules.Add(new TextureRule(true, "自定义", "_BaseMap", "_BaseMap", false));
                    break;
            }
        }

        private string[] SplitSuffixes(string suffixes)
        {
            if (string.IsNullOrEmpty(suffixes))
            {
                return new[] { "" };
            }

            string[] result = suffixes
                .Split(new[] { ';', ',', '，', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrEmpty(item))
                .ToArray();

            return result.Length > 0 ? result : new[] { "" };
        }

        private bool TryGetFolderPath(DefaultAsset folderAsset, string fieldName, out string folderPath)
        {
            folderPath = GetFolderPath(folderAsset);
            if (!string.IsNullOrEmpty(folderPath))
            {
                return true;
            }

            SetStatus("请先设置有效的" + fieldName + "。", MessageType.Warning);
            return false;
        }

        private string GetFolderPath(DefaultAsset folderAsset)
        {
            if (folderAsset == null)
            {
                return "";
            }

            string path = AssetDatabase.GetAssetPath(folderAsset);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
            {
                return "";
            }

            return NormalizeAssetPath(path);
        }

        private DefaultAsset GetSelectedFolderAsset()
        {
            foreach (UnityEngine.Object selectedObject in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(selectedObject);
                if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
                {
                    continue;
                }

                return AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
            }

            return null;
        }

        private bool IsPathInSearchScope(string assetPath, string folderPath)
        {
            assetPath = NormalizeAssetPath(assetPath);
            folderPath = NormalizeAssetPath(folderPath).TrimEnd('/');

            if (!assetPath.StartsWith(folderPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (includeSubFolders)
            {
                return true;
            }

            string assetDirectory = NormalizeAssetPath(Path.GetDirectoryName(assetPath));
            return string.Equals(assetDirectory, folderPath, StringComparison.OrdinalIgnoreCase);
        }

        private string NormalizeAssetPath(string path)
        {
            return string.IsNullOrEmpty(path) ? "" : path.Replace('\\', '/');
        }

        private string BuildNameKey(string name)
        {
            string key = string.IsNullOrEmpty(name) ? "" : name.Trim();
            return ignoreCase ? key.ToLowerInvariant() : key;
        }

        private void SetStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
        }
    }
}
