# InitializeSortSystem

基于C#反射的自动化初始化系统，支持通过特性标记和优先级控制，实现游戏模块的自动发现与顺序执行。

## 🎯 设计理念

- **场景纯净**：场景中只需挂载`InitializationManager`，所有系统自动初始化
- **自动发现**：运行时反射扫描所有初始化类，零配置使用
- **精确排序**：数字优先级控制执行顺序，彻底解决Unity生命周期顺序随机问题
- **混合架构**：同时支持MonoBehaviour组件和普通类的初始化

## 🚀 快速开始

### 1. 基础使用

```csharp
[Initialize("AudioSystem")]
public class AudioManager : MonoBehaviour, IInitialization
{
    public int Priority => 100;
    
    public void Initialize()
    {
        Debug.Log("音频系统初始化完成");
    }
}
```

### 2. 优先级控制

```csharp
[Initialize("GameConfig")]
public class GameConfigManager : IInitialization
{
    public int Priority => 200; // 数字越大越先执行
    
    public void Initialize()
    {
        Debug.Log("游戏配置初始化 - 最高优先级");
    }
}

[Initialize("UISystem")]  
public class UIManager : MonoBehaviour, IInitialization
{
    public int Priority => 50; // 较低优先级
    
    public void Initialize()
    {
        Debug.Log("UI系统初始化 - 较低优先级");
    }
}
```

## 📁 核心组件

### InitializationManager
初始化系统的总控管理器，自动在场景加载前创建。

```csharp
// 自动创建，无需手动放置
// 负责协调所有初始化类的发现和执行
```

### IInitialization 接口
所有初始化类必须实现的接口。

```csharp
public interface IInitialization
{
    void Initialize();    // 初始化方法
    int Priority { get; } // 执行优先级
}
```

### InitializeAttribute
标记初始化类的特性。

```csharp
[Initialize("SystemID")]
public class YourSystem : IInitialization
{
    // 实现接口...
}
```

### InitializationFactory
反射扫描和自动注册的核心工厂类。

## ⚙️ 技术特性

### 🎯 自动发现机制
- 运行时扫描所有程序集
- 自动识别 `[Initialize]` 标记的类
- 支持MonoBehaviour组件和普通类

### 📊 优先级系统
```csharp
// 推荐优先级范围
public int Priority => 300; // 核心系统 (输入、配置)
public int Priority => 200; // 管理系统 (场景、资源)  
public int Priority => 100; // 游戏逻辑 (玩家、敌人)
public int Priority => 50;  // UI系统 (界面、HUD)
```

### 🔄 混合初始化支持

**MonoBehaviour组件**
```csharp
[Initialize("PlayerSystem")]
public class Player : MonoBehaviour, IInitialization
{
    public int Priority => 150;
    
    public void Initialize()
    {
        // 使用场景中现有的Player组件实例
        this.HP = 100;
    }
}
```

**普通类系统**
```csharp
[Initialize("DataSystem")]
public class DataManager : IInitialization
{
    public int Priority => 250;
    
    public void Initialize()
    {
        // 动态创建实例并初始化
        LoadConfig();
    }
}
```

## 🛠️ 安装使用

1. 将包文件放入Unity项目
2. 在场景中创建`InitializationManager`（可选，系统会自动创建）
3. 为需要初始化的类添加`[Initialize]`特性和`IInitialization`接口
4. 运行游戏，系统自动完成所有初始化

## 🔍 执行流程

```
游戏启动
    ↓
自动创建 InitializationManager
    ↓  
反射扫描所有 [Initialize] 类
    ↓
按 Priority 排序初始化队列
    ↓
顺序执行所有 Initialize() 方法
    ↓
初始化完成，开始游戏逻辑
```

## 📝 日志输出

系统提供详细的执行日志：

```
[Initialization] 预注册完成，共 5 个系统
[Initialization] 注册场景组件: AudioManager (优先级: 100)
[Initialization] 注册普通类: GameConfigManager (优先级: 200)
[Initialization] 开始执行 5 个系统
[Initialization] 所有系统执行完成
```

## ⚠️ 注意事项

- `InitializationId` 在同一项目中应该唯一
- MonoBehaviour组件必须在场景中存在才会被初始化
- 普通类会自动创建实例，需要有无参构造函数
- 单个初始化失败不会影响其他系统

## 🎯 最佳实践

### 推荐模式
```csharp
// 1. 核心系统 - 高优先级
[Initialize("InputSystem")]
public class InputManager : IInitialization
{
    public int Priority => 300;
    public void Initialize() => SetupInput();
}

// 2. 游戏管理器 - 中优先级  
[Initialize("GameManager")]
public class GameManager : MonoBehaviour, IInitialization
{
    public int Priority => 200;
    public void Initialize() => InitGame();
}

// 3. UI系统 - 低优先级
[Initialize("UIManager")]
public class UIManager : MonoBehaviour, IInitialization  
{
    public int Priority => 100;
    public void Initialize() => SetupUI();
}
```

### 错误处理
```csharp
public void Initialize()
{
    try
    {
        // 初始化逻辑
    }
    catch (Exception e)
    {
        Debug.LogError($"初始化失败: {e.Message}");
        // 系统会继续执行其他初始化
    }
}
```

## 🔮 扩展建议

- 可扩展`InitializeAttribute`添加分组功能
- 可添加初始化依赖关系检查
- 可集成性能分析工具监控初始化耗时

## 📄 许可证

MIT License

---

*最后更新: 2025年11月11日*  
*适用于 Unity 2019.4+ 和 .NET 4.x*