# InitializeSortSystem

一个基于C#反射的自动化初始化系统，支持通过特性标记和优先级控制，实现游戏模块的自动发现与顺序执行。

## 设计哲学

- **场景纯净**：场景中只需挂载`InitializationManager`，所有其他系统通过代码动态初始化
- **自动发现**：使用反射自动扫描并注册所有初始化类，无需手动挂载或配置
- **优先级控制**：通过数字优先级精确控制初始化执行顺序
- **零配置**：开箱即用，无需复杂设置

## 核心组件

### InitializationManager
初始化系统的总控管理器，在场景中作为唯一的启动入口。

```csharp
// 自动在场景加载前创建，无需手动放置
// 负责协调所有初始化类的发现和执行
```

### IInitialiation 接口
所有初始化类需要实现的接口。

```csharp
public interface IInitialiation
{
    void Initialize();
    InitializeData initializeData { get; set; }
}
```

### InitializeAttribute
用于标记初始化类的特性。

```csharp
[Initialize("GameConfig")]
public class GameConfigInitializer : IInitialiation
{
    public InitializeData initializeData { get; set; }
    
    public void Initialize()
    {
        // 你的初始化逻辑
    }
}
```

## 快速开始

### 1. 安装
将包文件放入你的Unity项目即可。

### 2. 创建初始化类
```csharp
[Initialize("AudioSystem")]
public class AudioSystemInitializer : IInitialiation
{
    public InitializeData initializeData { get; set; }
    
    public AudioSystemInitializer()
    {
        initializeData = new InitializeData(Initialize, 100);
    }
    
    public void Initialize()
    {
        // 初始化音频系统
        Debug.Log("音频系统初始化完成");
    }
}
```

### 3. 运行
系统会自动：
- 扫描所有带有`[Initialize]`特性的类
- 按优先级排序（数字大的先执行）
- 自动执行所有初始化方法

## 优先级说明

- **高优先级 (100+)**：核心系统（输入、配置、存档）
- **中优先级 (50-99)**：管理系统（场景、资源、UI）
- **低优先级 (1-49)**：游戏逻辑（玩家、敌人、道具）

## 技术特点

- 🚀 **零接触初始化**：无需在编辑器中进行任何拖拽配置
- 🔍 **自动发现**：运行时自动扫描程序集发现所有初始化类  
- 📊 **精确排序**：基于数字优先级的确定性执行顺序
- 🛡️ **错误容忍**：单个初始化失败不影响其他模块
- 📝 **完整日志**：详细的执行过程日志输出

## 示例

```csharp
// 高优先级 - 配置系统
[Initialize("GameConfig")]
public class GameConfigInitializer : IInitialiation
{
    public InitializeData initializeData { get; set; }
    
    public GameConfigInitializer()
    {
        initializeData = new InitializeData(Initialize, 200);
    }
    
    public void Initialize()
    {
        // 最先执行：加载游戏配置
    }
}

// 中优先级 - 音频系统  
[Initialize("AudioSystem")]
public class AudioSystemInitializer : IInitialiation
{
    public InitializeData initializeData { get; set; }
    
    public AudioSystemInitializer()
    {
        initializeData = new InitializeData(Initialize, 100);
    }
    
    public void Initialize()
    {
        // 其次执行：初始化音频
    }
}
```

## 日志输出

系统运行时会在控制台输出详细的执行信息：

```
自动注册初始化项: GameConfig -> GameConfigInitializer
自动注册初始化项: AudioSystem -> AudioSystemInitializer  
[Initialization] 执行成功: Initialize
```

## 注意事项

- 确保所有初始化类都有无参构造函数
- 优先级数字越大越先执行
- 初始化ID在同一项目中应该唯一

## 许可证

MIT