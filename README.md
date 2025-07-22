# Robot Framework

A lightweight C#-based robot operating system inspired by ROS (Robot Operating System) design principles, providing a complete robot development and runtime environment.

## ✨ Key Features

### 🏗️ Core Architecture
- **Modular Design**: Node-based distributed architecture
- **Publish-Subscribe Communication**: Asynchronous message passing mechanism
- **Lifecycle Management**: Complete node startup, runtime, and shutdown processes
- **Type Safety**: Strong-typed message system with compile-time error checking
- **Easy Extension**: Simple node development interface

### 🔧 ROS-Style Features
- **Parameter Server**: Global configuration parameter management
- **Service Calls**: Synchronous request/response communication pattern
- **Action Server**: Management of long-running tasks
- **TF Transform System**: Coordinate frame transformation and management
- **Hierarchical Logging System**: Structured logging and management
- **Diagnostic System**: System health monitoring and fault diagnosis

### 🤖 Sensor Support
- **Camera Node**: Image acquisition and processing
- **LiDAR Node**: 2D laser scanning and point cloud generation
- **IMU Node**: Inertial measurement unit data
- **Odometry Node**: Robot position and velocity estimation

### 🧭 Navigation Features
- **Path Planning**: A* algorithm-based path planning
- **Navigation Control**: Goal-based autonomous navigation
- **Obstacle Detection**: Sensor-based environment perception
- **Map Management**: Occupancy grid map construction and maintenance

### 📊 Monitoring & Diagnostics
- **System Monitoring**: CPU, memory, thread monitoring
- **Network Monitoring**: Communication latency and frequency analysis
- **Health Checks**: Node status and system health assessment
- **Real-time Alerts**: Automatic alerts for abnormal conditions

## 🏛️ System Architecture

```
RobotFramework/
├── Core/                    # Core Components
│   ├── INode.cs            # Node interface definition
│   ├── BaseNode.cs         # Node base class implementation
│   ├── MessageBus.cs       # Message bus
│   ├── RobotSystem.cs      # Robot system management
│   ├── ParameterServer.cs  # Parameter server
│   ├── ServiceManager.cs   # Service manager
│   ├── ActionServer.cs     # Action server
│   ├── TransformBuffer.cs  # Transform buffer
│   └── Logger.cs           # Logging system
├── Messages/               # Message definitions
│   └── Messages.cs         # All message types
├── Nodes/                  # Pre-built nodes
│   ├── CameraNode.cs       # Camera node
│   ├── EmotionDetectionNode.cs # Emotion detection node
│   ├── RobotControlNode.cs # Robot control node
│   ├── SensorNodes.cs      # Sensor node collection
│   ├── NavigationNodes.cs  # Navigation node collection
│   └── DiagnosticNodes.cs  # Diagnostic node collection
├── Program.cs              # Main program entry
└── RobotFramework.csproj   # Project configuration
```

## 📨 Message System

### Basic Message Types
- `BaseMessage`: Base class for all messages
- `RobotActionMessage`: Robot action commands
- `SensorDataMessage`: Sensor data
- `EmotionMessage`: Emotion detection results
- `SystemStatusMessage`: System status information

### Extended Message Types
- `ImageMessage`: Image data transmission
- `PointCloudMessage`: Point cloud data
- `LaserScanMessage`: Laser scan data
- `ImuMessage`: IMU sensor data
- `OdometryMessage`: Odometry data
- `TwistMessage`: Velocity control commands
- `PathMessage`: Path planning results
- `GoalMessage`: Navigation goals
- `DiagnosticMessage`: Diagnostic information

## 🚀 Quick Start

### Requirements
- .NET 6.0 or higher
- Windows/Linux/macOS

### Build and Run
```bash
# Clone the project
git clone <repository-url>
cd RobotFramework

# Build the project
dotnet build

# Run the example
dotnet run
```

### Basic Usage Example

```csharp
// Create robot system
var robotSystem = new RobotSystem();

// Create sensor nodes
var cameraNode = new CameraNode();
var lidarNode = new LidarNode();
var imuNode = new ImuNode();

// Create navigation nodes
var navigationNode = new NavigationNode();
var pathPlannerNode = new PathPlannerNode();

// Add nodes to system
robotSystem.AddNode(cameraNode);
robotSystem.AddNode(lidarNode);
robotSystem.AddNode(imuNode);
robotSystem.AddNode(navigationNode);
robotSystem.AddNode(pathPlannerNode);

// Start system
await robotSystem.StartAsync();

// System running...

// Stop system
await robotSystem.StopAsync();
```

## 🔧 Parameter Configuration

Use parameter server for system configuration:

```csharp
var paramServer = ParameterServer.Instance;

// Set sensor parameters
paramServer.SetParameter("/camera/fps", 30);
paramServer.SetParameter("/lidar/max_range", 10.0f);

// Set navigation parameters
paramServer.SetParameter("/navigation/goal_tolerance", 0.2f);
paramServer.SetParameter("/navigation/max_linear_speed", 1.0f);

// Get parameters
var fps = paramServer.GetParameter("/camera/fps", 30);
```

## 🛠️ Service Calls

Implement synchronous request/response communication:

```csharp
// Register service
ServiceManager.Instance.RegisterService<MyRequest, MyResponse>(
    "/my_service", 
    HandleMyService
);

// Call service
var request = new MyRequest();
var response = await ServiceManager.Instance.CallServiceAsync<MyRequest, MyResponse>(
    "/my_service", 
    request
);
```

## 🎯 Action Server

Manage long-running tasks:

```csharp
// Register action
ActionServer.Instance.RegisterAction<NavigationGoal, NavigationResult, NavigationFeedback>(
    "/navigate", 
    HandleNavigationAction
);

// Send action goal
var goal = new NavigationGoal();
var actionId = ActionServer.Instance.SendGoal("/navigate", goal);
```

## 🗺️ Coordinate Transforms

Manage robot coordinate frames:

```csharp
// Set transform
var transform = new Transform(position, rotation, "parent_frame", "child_frame");
TransformBuffer.Instance.SetTransform(transform);

// Lookup transform
var transform = TransformBuffer.Instance.LookupTransform("source_frame", "target_frame");

// Transform point
var transformedPoint = TransformBuffer.Instance.TransformPoint(point, "target_frame");
```

## 📝 Logging System

Structured logging:

```csharp
// Use in nodes
this.LogInfo("Node started successfully");
this.LogWarn("Abnormal condition detected");
this.LogError("Error occurred");

// Direct usage
Logger.Instance.Info("System information");
Logger.Instance.Error("Error information");
```

## 📊 Topic Naming Conventions

| Topic Type | Naming Convention | Example |
|-----------|------------------|---------|
| Sensor Data | `/sensor_type/data` | `/camera/image`, `/lidar/scan` |
| Control Commands | `/cmd_type` | `/cmd_vel`, `/cmd_action` |
| Status Information | `/status/component` | `/status/battery`, `/status/motors` |
| Diagnostic Information | `/diagnostics/component` | `/diagnostics/system`, `/diagnostics/network` |
| Navigation Related | `/navigation/type` | `/navigation/goal`, `/navigation/path` |

## 🔍 System Monitoring

### Real-time Monitoring Features
- **System Resource Monitoring**: CPU usage, memory consumption, thread count
- **Network Performance Monitoring**: Message latency, communication frequency, topic activity
- **Node Health Checks**: Node status, response time, error rate
- **Automatic Alert System**: Anomaly detection and real-time notifications

### Diagnostic Information
```csharp
// System resource diagnostics
/diagnostics/system_resources

// Network performance diagnostics
/diagnostics/network_performance

// Node status diagnostics
/diagnostics/nodes_status

// Health report
/health_report
```

## 🧩 Extension Guide

### Creating Custom Nodes

```csharp
public class MyCustomNode : BaseNode
{
    public MyCustomNode(string name = "MyCustomNode") : base(name) { }

    protected override Task OnStartAsync()
    {
        this.LogInfo("Custom node started");
        
        // Subscribe to messages
        Subscribe<SensorDataMessage>("/sensor/data", OnSensorData);
        
        return Task.CompletedTask;
    }

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Node main loop logic
            var message = new MyMessage("Hello", Name);
            Publish("/my_topic", message);
            
            await Task.Delay(1000, cancellationToken);
        }
    }

    private void OnSensorData(SensorDataMessage message)
    {
        this.LogInfo($"Received sensor data: {message.Data}");
        // Process sensor data
    }

    protected override Task OnStopAsync()
    {
        this.LogInfo("Custom node stopped");
        return Task.CompletedTask;
    }
}
```

### Creating Custom Messages

```csharp
public class MyCustomMessage : BaseMessage
{
    public string CustomData { get; }
    public int CustomValue { get; }

    public MyCustomMessage(string data, int value, string senderId) 
        : base(senderId)
    {
        CustomData = data;
        CustomValue = value;
    }
}
```

## 📈 Comparison with ROS

| Feature | Robot Framework | ROS |
|---------|----------------|-----|
| Programming Language | C# | C++/Python |
| Message Passing | In-memory message bus | Network communication |
| Type Safety | Compile-time checking | Runtime checking |
| Deployment Complexity | Single executable | Distributed nodes |
| Learning Curve | Lower | Higher |
| Performance | High-performance memory communication | Network overhead |
| Ecosystem | Emerging | Mature and rich |
| Cross-platform | .NET supported platforms | Linux-dominated |
| Parameter Server | ✅ Built-in support | ✅ Built-in support |
| Service Calls | ✅ Built-in support | ✅ Built-in support |
| Action Server | ✅ Built-in support | ✅ Built-in support |
| TF Transforms | ✅ Built-in support | ✅ Built-in support |
| Diagnostic System | ✅ Built-in support | ✅ Built-in support |

## 🚧 Future Plans

### Short-term Goals
- [ ] Visualization tools development
- [ ] More sensor drivers
- [ ] Machine learning integration
- [ ] Performance optimization

### Medium-term Goals
- [ ] Distributed deployment support
- [ ] Web management interface
- [ ] Plugin system
- [ ] Simulation environment integration

### Long-term Goals
- [ ] Cloud robotics support
- [ ] Edge computing optimization
- [ ] AI/ML workflows
- [ ] Industrial-grade reliability

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details.

## 📞 Contact

- Project Homepage: [GitHub Repository](https://github.com/your-username/robot-framework)
- Issue Reports: [Issues](https://github.com/your-username/robot-framework/issues)
- Discussions: [Discussions](https://github.com/your-username/robot-framework/discussions)

---

**Robot Framework** - Making robot development simpler and more efficient! 🤖✨