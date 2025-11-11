using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
namespace TechCosmos.InitializeSortSystem.Runtime
{
    public class InitializationManager : MonoBehaviour
    {
        public static InitializationManager Instance { get; private set; }
        private static List<InitializeData> _pendingInitializations = new();
        private static bool _executionStarted = false;
        public static bool _isInitialized = false;

        public void RegisterInitialization(IInitialiation initialiation)
        {
            if (_executionStarted)
            {
                Debug.LogWarning("[Initialization] 初始化已开始，无法注册新方法");
                return;
            }

            if (_pendingInitializations.Any(x => x.InitializeAction == initialiation.initializeData.InitializeAction))
                return;

            _pendingInitializations.Add(new InitializeData(
                initialiation.initializeData.InitializeAction,
                initialiation.initializeData.SortLevel));
        }

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            ExcutaInitializations();
        }

        private void ExcutaInitializations()
        {
            _executionStarted = true;

            // 按优先级执行
            foreach (var data in _pendingInitializations.OrderByDescending(x => x.SortLevel))
            {
                try
                {
                    data.InitializeAction?.Invoke();
                    Debug.Log($"[Initialization] 执行成功: {data.InitializeAction.Method.Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Initialization] 执行失败: {ex.Message}");
                }
            }

            _pendingInitializations.Clear();
        }

        private void OnDisable()
        {
            _isInitialized = false;
        }
    }
}
