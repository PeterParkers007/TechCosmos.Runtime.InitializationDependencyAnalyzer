using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TechCosmos.InitializeSortSystem.Runtime
{
    public static class InitializationFactory
    {
        private static Dictionary<string, Type> _initializationTypes = new Dictionary<string, Type>();
        private static bool _initialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            if (_initialized) return;

            ScanAndRegisterInitializations();
            _initialized = true;
        }

        private static void ScanAndRegisterInitializations()
        {
            _initializationTypes.Clear();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                // 跳过系统程序集提升性能
                if (assembly.FullName.StartsWith("System.") ||
                    assembly.FullName.StartsWith("Unity.") ||
                    assembly.FullName.StartsWith("UnityEngine.") ||
                    assembly.FullName.StartsWith("UnityEditor."))
                    continue;

                try
                {
                    var abilityTypes = assembly.GetTypes()
                        .Where(t =>
                            t.IsClass &&
                            !t.IsAbstract &&
                            typeof(IInitialiation).IsAssignableFrom(t) &&
                            t.IsDefined(typeof(InitializeAttribute), false));

                    foreach (var type in abilityTypes)
                    {
                        var attribute = type.GetCustomAttribute<InitializeAttribute>();
                        if (attribute != null && !_initializationTypes.ContainsKey(attribute.InitializationId))
                        {
                            _initializationTypes[attribute.InitializationId] = type;
                            Debug.Log($"自动注册初始化项: {attribute.InitializationId} -> {type.Name}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"扫描程序集 {assembly.FullName} 时出错: {e.Message}");
                }
            }

            // 注册内置初始化（确保覆盖）
            RegisterBuiltinInitializations();
        }

        private static void RegisterBuiltinInitializations()
        {
            //// 内置能力手动注册，确保优先级
            //_initializationTypes["Move"] = typeof(MoveAbility);
            //_initializationTypes["Attack"] = typeof(AttackAbility);
        }

        public static IInitialiation CreateInitialization(string InitializationId)
        {
            if (!_initialized) Initialize();

            if (_initializationTypes.TryGetValue(InitializationId, out var type))
            {
                try
                {
                    return (IInitialiation)Activator.CreateInstance(type);
                }
                catch (Exception e)
                {
                    Debug.LogError($"注册初始化项 {InitializationId} 失败: {e.Message}");
                    return null;
                }
            }

            Debug.LogWarning($"未找到可初始化项: {InitializationId}");
            return null;
        }
        // 新增方法：创建并注册所有系统
        public static void CreateAndRegisterAllSystems()
        {
            if (!_initialized) Initialize();

            foreach (var kvp in _initializationTypes)
            {
                try
                {
                    IInitialiation initializaInstance = (IInitialiation)Activator.CreateInstance(kvp.Value);
                    InitializationManager.Instance.RegisterInitialization(initializaInstance);
                    Debug.Log($"✅ 注册初始化项: {kvp.Key}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"注册初始化项 {kvp.Key} 失败: {e.Message}");
                }
            }
        }
        // 调试用：获取所有已注册的能力
        public static IEnumerable<string> GetRegisteredInitializationIds()
        {
            if (!_initialized) Initialize();
            return _initializationTypes.Keys;
        }
    }
}