#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TechCosmos.InitializeSortSystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace TechCosmos.InitializeSortSystem.Editor
{
    public class InitializationDependencyAnalyzer : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<SystemInfo> _systemInfos = new List<SystemInfo>();
        private bool _showPrioritySuggestions = true;
        private bool _autoApplySuggestions = false;

        [MenuItem("Tech-Cosmos/初始化依赖分析器")]
        public static void ShowWindow()
        {
            GetWindow<InitializationDependencyAnalyzer>("初始化依赖分析器");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            // 控制选项
            EditorGUILayout.BeginVertical("box");
            {
                if (GUILayout.Button("扫描依赖关系", GUILayout.Height(30)))
                {
                    ScanDependencies();
                }

                EditorGUILayout.Space();

                _showPrioritySuggestions = EditorGUILayout.Toggle("显示优先值建议", _showPrioritySuggestions);
                _autoApplySuggestions = EditorGUILayout.Toggle("自动应用建议值", _autoApplySuggestions);

                EditorGUILayout.Space();

                // 应用建议值按钮
                var canUpdateCount = _systemInfos.Count(s => s.CanUpdate && s.NeedsUpdate);
                var buttonText = canUpdateCount > 0 ?
                    $"应用建议值到字段 ({canUpdateCount} 个系统)" :
                    "应用建议值到字段";

                using (new EditorGUI.DisabledScope(canUpdateCount == 0))
                {
                    if (GUILayout.Button(buttonText, GUILayout.Height(25)))
                    {
                        ApplyAllSuggestedPriorities();
                    }
                }
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // 结果显示
            if (_systemInfos.Count > 0)
            {
                GUILayout.Label($"找到 {_systemInfos.Count} 个系统:", EditorStyles.boldLabel);

                // 统计信息
                var updatedCount = _systemInfos.Count(s => s.WasUpdated);
                var canUpdateCount = _systemInfos.Count(s => s.CanUpdate && s.NeedsUpdate);

                EditorGUILayout.BeginHorizontal();
                {
                    if (updatedCount > 0)
                    {
                        EditorGUILayout.LabelField($"已更新: {updatedCount}", GetMiniLabelStyle(Color.green));
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"已更新: {updatedCount}", EditorStyles.miniLabel);
                    }

                    if (canUpdateCount > 0)
                    {
                        EditorGUILayout.LabelField($"可更新: {canUpdateCount}", GetMiniLabelStyle(Color.yellow));
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"可更新: {canUpdateCount}", EditorStyles.miniLabel);
                    }

                    EditorGUILayout.LabelField($"总计: {_systemInfos.Count}", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                {
                    // 按建议优先级排序显示（高优先级在前）
                    foreach (var systemInfo in _systemInfos.Where(s => s != null).OrderByDescending(s => s.SuggestedPriority))
                    {
                        DrawSystemInfo(systemInfo);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSystemInfo(SystemInfo systemInfo)
        {
            if (systemInfo == null) return;

            EditorGUILayout.BeginVertical("box");
            {
                // 系统名称和ID
                EditorGUILayout.LabelField(systemInfo.SystemId ?? "未知系统ID", EditorStyles.boldLabel);

                // 类型显示
                if (systemInfo.Type != null)
                {
                    string typeInfo = $"类型: {systemInfo.Type.Name}";
                    if (systemInfo.UsesAbstractBase)
                    {
                        typeInfo += " ✓ (InitializationBehaviour)";
                    }
                    EditorGUILayout.LabelField(typeInfo);
                }

                // 字段支持状态
                EditorGUILayout.BeginHorizontal();
                {
                    if (systemInfo.UsesAbstractBase)
                    {
                        EditorGUILayout.LabelField("架构: 抽象基类", GetMiniLabelStyle(Color.green));
                    }
                    else if (systemInfo.HasPriorityField)
                    {
                        EditorGUILayout.LabelField("架构: 自定义字段", GetMiniLabelStyle(Color.blue));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("架构: 仅接口", GetMiniLabelStyle(Color.red));
                    }

                    if (systemInfo.CanUpdate)
                    {
                        EditorGUILayout.LabelField("字段: 可修改", GetMiniLabelStyle(Color.green));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("字段: 不可修改", GetMiniLabelStyle(Color.gray));
                    }
                }
                EditorGUILayout.EndHorizontal();

                // 当前优先级
                EditorGUILayout.LabelField($"当前优先级: {systemInfo.CurrentPriority}");

                // 建议优先级（如果启用）
                if (_showPrioritySuggestions)
                {
                    var style = new GUIStyle(EditorStyles.label);
                    if (systemInfo.NeedsUpdate)
                    {
                        style.normal.textColor = Color.yellow;
                        EditorGUILayout.LabelField($"建议优先级: {systemInfo.SuggestedPriority}", style);

                        if (systemInfo.CanUpdate)
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.LabelField($"提示: 设置 _priority = {systemInfo.SuggestedPriority}", EditorStyles.miniLabel);

                                if (GUILayout.Button("应用", GUILayout.Width(50)))
                                {
                                    ApplySystemPriority(systemInfo);
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        else
                        {
                            EditorGUILayout.LabelField("提示: 建议继承 InitializationBehaviour 以获得字段支持", EditorStyles.miniLabel);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"建议优先级: {systemInfo.SuggestedPriority} (已最新)");
                    }
                }

                // 更新状态
                if (systemInfo.WasUpdated)
                {
                    EditorGUILayout.LabelField("状态: ✓ 已更新", GetMiniLabelStyle(Color.green));
                }
                else if (systemInfo.NeedsUpdate && systemInfo.CanUpdate)
                {
                    EditorGUILayout.LabelField("状态: ● 需要更新", GetMiniLabelStyle(Color.yellow));
                }

                // 初始化顺序
                EditorGUILayout.LabelField($"初始化顺序: {systemInfo.InitializationOrder}");

                // 依赖信息
                if (systemInfo.Dependencies != null && systemInfo.Dependencies.Count > 0)
                {
                    EditorGUILayout.LabelField($"依赖的系统 ({systemInfo.Dependencies.Count}个):", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(string.Join(", ", systemInfo.Dependencies), EditorStyles.wordWrappedMiniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("依赖的系统: 无", EditorStyles.miniLabel);
                }

                // 被依赖信息
                if (systemInfo.Dependents != null && systemInfo.Dependents.Count > 0)
                {
                    EditorGUILayout.LabelField($"被依赖的系统 ({systemInfo.Dependents.Count}个):", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(string.Join(", ", systemInfo.Dependents), EditorStyles.wordWrappedMiniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("被依赖的系统: 无", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);
        }

        private GUIStyle GetMiniLabelStyle(Color color)
        {
            var style = new GUIStyle(EditorStyles.miniLabel);
            style.normal.textColor = color;
            return style;
        }

        private void ScanDependencies()
        {
            _systemInfos.Clear();

            // 扫描所有程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var systemTypes = new Dictionary<string, Type>();

            foreach (var assembly in assemblies)
            {
                if (assembly.FullName.StartsWith("System.") ||
                    assembly.FullName.StartsWith("Unity.") ||
                    assembly.FullName.StartsWith("UnityEngine."))
                    continue;

                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t != null &&
                                   t.IsClass &&
                                   !t.IsAbstract &&
                                   t.IsDefined(typeof(InitializeAttribute), false));

                    foreach (var type in types)
                    {
                        var attribute = type.GetCustomAttribute<InitializeAttribute>();
                        if (attribute != null &&
                            !string.IsNullOrEmpty(attribute.InitializationId) &&
                            !systemTypes.ContainsKey(attribute.InitializationId))
                        {
                            systemTypes[attribute.InitializationId] = type;
                        }
                    }
                }
                catch (ReflectionTypeLoadException e)
                {
                    Debug.LogWarning($"扫描程序集 {assembly.FullName} 时类型加载失败: {e.Message}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"扫描程序集 {assembly.FullName} 时出错: {e.Message}");
                }
            }

            // 构建系统信息
            foreach (var kvp in systemTypes)
            {
                if (kvp.Value != null && !string.IsNullOrEmpty(kvp.Key))
                {
                    var systemInfo = CreateSystemInfo(kvp.Value, kvp.Key);
                    if (systemInfo != null)
                    {
                        _systemInfos.Add(systemInfo);
                    }
                }
            }

            // 计算依赖关系图
            BuildDependencyGraph();

            // 计算建议的优先级值和初始化顺序
            CalculateSuggestedPriorities();

            // 自动应用建议值（如果启用）
            if (_autoApplySuggestions)
            {
                ApplyAllSuggestedPriorities();
            }

            Debug.Log($"依赖分析完成！共扫描到 {_systemInfos.Count} 个系统");
        }

        private SystemInfo CreateSystemInfo(Type type, string systemId)
        {
            if (type == null || string.IsNullOrEmpty(systemId))
            {
                Debug.LogError($"创建SystemInfo失败：类型为null或系统ID为空");
                return null;
            }

            var systemInfo = new SystemInfo
            {
                Type = type,
                SystemId = systemId,
                Dependencies = new HashSet<string>(),
                Dependents = new HashSet<string>(),
                CurrentPriority = 0,
                InitializationOrder = 0,
                SuggestedPriority = 0,
                UsesAbstractBase = false,
                HasPriorityField = false,
                CanUpdate = false,
                WasUpdated = false
            };

            try
            {
                // 检查架构类型和字段支持
                CheckArchitectureSupport(systemInfo, type);

                // 获取当前优先级
                systemInfo.CurrentPriority = GetCurrentPriority(systemInfo, type);

                // 解析依赖关系
                var dependsOnAttributes = type.GetCustomAttributes<DependsOnAttribute>(false);
                foreach (var attr in dependsOnAttributes)
                {
                    if (attr != null && attr.SystemIds != null)
                    {
                        foreach (var dependency in attr.SystemIds.Where(d => !string.IsNullOrEmpty(d)))
                        {
                            systemInfo.Dependencies.Add(dependency);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"创建 {type.FullName} 的SystemInfo时出错: {e.Message}");
                return null;
            }

            return systemInfo;
        }

        private void CheckArchitectureSupport(SystemInfo systemInfo, Type type)
        {
            // 检查是否继承自抽象基类
            systemInfo.UsesAbstractBase = typeof(InitializationBehaviour).IsAssignableFrom(type);

            // 检查是否有优先级字段支持
            if (systemInfo.UsesAbstractBase)
            {
                // 继承自抽象基类，肯定有字段支持
                systemInfo.HasPriorityField = true;
                systemInfo.CanUpdate = true;
            }
            else
            {
                // 检查自定义字段
                systemInfo.HasPriorityField = CheckForPriorityField(type);
                systemInfo.CanUpdate = systemInfo.HasPriorityField;
            }
        }

        private bool CheckForPriorityField(Type type)
        {
            // 检查是否有_priority字段或其他常见字段名
            var fieldNames = new[] { "_priority", "priority", "m_priority", "initPriority", "_initPriority" };
            foreach (var fieldName in fieldNames)
            {
                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(int))
                {
                    return true;
                }
            }
            return false;
        }

        private int GetCurrentPriority(SystemInfo systemInfo, Type type)
        {
            if (!typeof(IInitialization).IsAssignableFrom(type))
            {
                return 0;
            }

            // 对于有字段支持的类，我们可以尝试获取字段的当前值
            if (systemInfo.HasPriorityField)
            {
                return GetPriorityFromField(type);
            }

            // 对于没有字段支持的类，返回默认值
            return 0;
        }

        private int GetPriorityFromField(Type type)
        {
            // 查找场景中的现有实例来获取当前值
            var existingInstances = FindObjectsOfType(type);
            if (existingInstances != null && existingInstances.Length > 0)
            {
                var instance = existingInstances[0];
                var priorityField = GetPriorityField(instance.GetType());
                if (priorityField != null)
                {
                    return (int)priorityField.GetValue(instance);
                }
            }

            return 0;
        }

        private FieldInfo GetPriorityField(Type type)
        {
            var fieldNames = new[] { "_priority", "priority", "m_priority", "initPriority", "_initPriority" };
            foreach (var fieldName in fieldNames)
            {
                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(int))
                {
                    return field;
                }
            }
            return null;
        }

        private void BuildDependencyGraph()
        {
            // 构建依赖图
            foreach (var system in _systemInfos.Where(s => s != null))
            {
                if (system.Dependencies == null) continue;

                foreach (var dependency in system.Dependencies)
                {
                    if (string.IsNullOrEmpty(dependency)) continue;

                    var dependentSystem = _systemInfos.FirstOrDefault(s => s != null && s.SystemId == dependency);
                    if (dependentSystem != null)
                    {
                        dependentSystem.Dependents.Add(system.SystemId);
                    }
                    else
                    {
                        Debug.LogWarning($"系统 {system.SystemId} 依赖的 {dependency} 不存在");
                    }
                }
            }
        }

        private void CalculateSuggestedPriorities()
        {
            // 被依赖的系统需要更高的优先级（先初始化）
            var sortedSystems = TopologicalSort();

            // 分配建议优先级（先初始化的系统获得高优先级）
            int basePriority = 1000;
            int priorityStep = 10;

            foreach (var system in sortedSystems.Where(s => s != null))
            {
                system.SuggestedPriority = basePriority;
                basePriority -= priorityStep;
            }

            // 分配初始化顺序
            int order = 1;
            foreach (var system in sortedSystems.Where(s => s != null))
            {
                system.InitializationOrder = order++;
            }

            Debug.Log($"优先级计算完成：最高优先级 {sortedSystems.First().SuggestedPriority}，最低优先级 {sortedSystems.Last().SuggestedPriority}");
        }

        private List<SystemInfo> TopologicalSort()
        {
            var result = new List<SystemInfo>();
            var visited = new HashSet<string>();
            var tempMark = new HashSet<string>();

            // 找出所有没有依赖的系统（这些应该最先初始化）
            var noDependencySystems = _systemInfos.Where(s => s != null &&
                (s.Dependencies == null || s.Dependencies.Count == 0)).ToList();

            // 从没有依赖的系统开始遍历
            foreach (var system in noDependencySystems)
            {
                if (!visited.Contains(system.SystemId))
                {
                    TopologicalVisit(system, visited, tempMark, result);
                }
            }

            // 处理可能有循环依赖的剩余系统
            foreach (var system in _systemInfos.Where(s => s != null))
            {
                if (!visited.Contains(system.SystemId))
                {
                    TopologicalVisit(system, visited, tempMark, result);
                }
            }

            return result;
        }

        private void TopologicalVisit(SystemInfo system, HashSet<string> visited, HashSet<string> tempMark, List<SystemInfo> result)
        {
            if (system == null || string.IsNullOrEmpty(system.SystemId)) return;

            if (tempMark.Contains(system.SystemId))
            {
                Debug.LogError($"发现循环依赖！系统: {system.SystemId}");
                return;
            }

            if (!visited.Contains(system.SystemId))
            {
                tempMark.Add(system.SystemId);

                // 先访问所有依赖这个系统的系统（被依赖的系统应该先初始化）
                if (system.Dependents != null)
                {
                    foreach (var dependentId in system.Dependents)
                    {
                        if (string.IsNullOrEmpty(dependentId)) continue;

                        var dependentSystem = _systemInfos.FirstOrDefault(s => s != null && s.SystemId == dependentId);
                        if (dependentSystem != null && !visited.Contains(dependentSystem.SystemId))
                        {
                            TopologicalVisit(dependentSystem, visited, tempMark, result);
                        }
                    }
                }

                tempMark.Remove(system.SystemId);
                visited.Add(system.SystemId);

                // 被依赖的系统应该排在前面（先初始化）
                result.Insert(0, system);
            }
        }

        private void ApplyAllSuggestedPriorities()
        {
            int updatedCount = 0;

            foreach (var systemInfo in _systemInfos.Where(s => s.CanUpdate && s.NeedsUpdate))
            {
                if (ApplySystemPriority(systemInfo))
                {
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                Debug.Log($"成功更新了 {updatedCount} 个系统的优先级字段");
                // 刷新场景视图，让修改立即生效
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }
            else
            {
                Debug.Log("没有需要更新的系统");
            }
        }

        private bool ApplySystemPriority(SystemInfo systemInfo)
        {
            if (systemInfo == null || !systemInfo.CanUpdate || !systemInfo.NeedsUpdate)
            {
                return false;
            }

            try
            {
                // 查找场景中的实例
                var instances = FindObjectsOfType(systemInfo.Type);
                if (instances == null || instances.Length == 0)
                {
                    Debug.LogWarning($"场景中未找到 {systemInfo.SystemId} 的实例，无法更新字段值");
                    return false;
                }

                var priorityField = GetPriorityField(systemInfo.Type);
                if (priorityField == null)
                {
                    Debug.LogWarning($"未找到 {systemInfo.SystemId} 的优先级字段");
                    return false;
                }

                bool anyUpdated = false;
                foreach (var instance in instances)
                {
                    int currentValue = (int)priorityField.GetValue(instance);
                    if (currentValue != systemInfo.SuggestedPriority)
                    {
                        priorityField.SetValue(instance, systemInfo.SuggestedPriority);
                        anyUpdated = true;

                        // 标记为脏对象，确保修改被保存
                        EditorUtility.SetDirty(instance);
                    }
                }

                if (anyUpdated)
                {
                    systemInfo.WasUpdated = true;
                    systemInfo.CurrentPriority = systemInfo.SuggestedPriority;
                    Debug.Log($"已更新 {systemInfo.SystemId} 的优先级字段为 {systemInfo.SuggestedPriority}");
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"更新 {systemInfo.SystemId} 的优先级字段时出错: {e.Message}");
            }

            return false;
        }
    }

    [System.Serializable]
    public class SystemInfo
    {
        public Type Type;
        public string SystemId;
        public int CurrentPriority;
        public int SuggestedPriority;
        public int InitializationOrder;
        public HashSet<string> Dependencies;
        public HashSet<string> Dependents;

        // 架构支持信息
        public bool UsesAbstractBase;
        public bool HasPriorityField;
        public bool CanUpdate;
        public bool WasUpdated;

        public bool NeedsUpdate => CurrentPriority != SuggestedPriority;
    }
}
#endif