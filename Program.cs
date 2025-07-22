using System;
using System.Threading.Tasks;
using RobotFramework.Core;
using RobotFramework.Nodes;
using RobotFramework.Messages;

namespace RobotFramework
{
    /// <summary>
    /// Robot Framework Example Program - Demonstrates complete ROS-style functionality
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Robot Framework...");
            Console.WriteLine("=== Robot Framework v2.0 - ROS-style Robot Operating System ===");

            // Initialize parameter server
            InitializeParameters();

            // Create robot system
            var robotSystem = new RobotSystem();

            // Create core sensor nodes
            var cameraNode = new CameraNode("Camera", 30); // 30 FPS
            var lidarNode = new LidarNode("Lidar");
            var imuNode = new ImuNode("IMU");
            var odometryNode = new OdometryNode("Odometry");

            // Create perception and control nodes
            var emotionNode = new EmotionDetectionNode("EmotionDetector");
            var robotNode = new RobotControlNode("RobotController");

            // Create navigation nodes
            var navigationNode = new NavigationNode("Navigation");
            var pathPlannerNode = new PathPlannerNode("PathPlanner");

            // Create monitoring and diagnostic nodes
            var systemMonitorNode = new SystemMonitorNode("SystemMonitor");
            var networkMonitorNode = new NetworkMonitorNode("NetworkMonitor");
            var healthCheckNode = new HealthCheckNode("HealthCheck");

            // Add all nodes to system
            robotSystem.AddNode(cameraNode);
            robotSystem.AddNode(lidarNode);
            robotSystem.AddNode(imuNode);
            robotSystem.AddNode(odometryNode);
            robotSystem.AddNode(emotionNode);
            robotSystem.AddNode(robotNode);
            robotSystem.AddNode(navigationNode);
            robotSystem.AddNode(pathPlannerNode);
            robotSystem.AddNode(systemMonitorNode);
            robotSystem.AddNode(networkMonitorNode);
            robotSystem.AddNode(healthCheckNode);

            try
            {
                // Start system
                await robotSystem.StartAsync();

                Console.WriteLine();
                Console.WriteLine("=== System Started Successfully ===");
                Console.WriteLine("Active Nodes:");
                Console.WriteLine("- Sensors: CameraNode, LidarNode, ImuNode, OdometryNode");
                Console.WriteLine("- Perception: EmotionDetectionNode");
                Console.WriteLine("- Control: RobotControlNode");
                Console.WriteLine("- Navigation: NavigationNode, PathPlannerNode");
                Console.WriteLine("- Monitoring: SystemMonitorNode, NetworkMonitorNode, HealthCheckNode");
                Console.WriteLine();
                Console.WriteLine("=== Available Features ===");
                Console.WriteLine("✓ Parameter Server - Global configuration management");
                Console.WriteLine("✓ Service Calls - Synchronous request/response communication");
                Console.WriteLine("✓ Action Server - Long-running task management");
                Console.WriteLine("✓ TF Transform System - Coordinate frame management");
                Console.WriteLine("✓ Hierarchical Logging System - Structured logging");
                Console.WriteLine("✓ Sensor Fusion - Multi-sensor data processing");
                Console.WriteLine("✓ Path Planning - A* algorithm navigation");
                Console.WriteLine("✓ System Monitoring - Real-time health checks");
                Console.WriteLine("✓ Network Diagnostics - Communication quality monitoring");
                Console.WriteLine();
                Console.WriteLine("System running... Press any key to view status, press 'q' to exit");
                Console.WriteLine();

                // Main loop
                while (true)
                {
                    var key = Console.ReadKey(true);
                    
                    if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                    {
                        break;
                    }
                    else if (key.KeyChar == 's' || key.KeyChar == 'S')
                    {
                        ShowSystemStatus(robotSystem);
                    }
                    else if (key.KeyChar == 't' || key.KeyChar == 'T')
                    {
                        await TestManualAction(robotNode);
                    }
                    else
                    {
                        Console.WriteLine("Commands:");
                        Console.WriteLine("  's' - Show system status");
                        Console.WriteLine("  't' - Test manual action");
                        Console.WriteLine("  'q' - Exit system");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"System error: {ex.Message}");
            }
            finally
            {
                // Stop system
                await robotSystem.StopAsync();
                Console.WriteLine("System exited safely");
            }
        }

        /// <summary>
        /// Initialize system parameters
        /// </summary>
        private static void InitializeParameters()
        {
            var paramServer = ParameterServer.Instance;

            // Sensor parameters
            paramServer.SetParameter("/camera/fps", 30);
            paramServer.SetParameter("/camera/resolution_width", 640);
            paramServer.SetParameter("/camera/resolution_height", 480);
            
            paramServer.SetParameter("/lidar/max_range", 10.0f);
            paramServer.SetParameter("/lidar/scan_points", 360);
            paramServer.SetParameter("/lidar/scan_interval", 100);
            
            paramServer.SetParameter("/imu/publish_rate", 50);
            paramServer.SetParameter("/odom/publish_rate", 50);

            // Navigation parameters
            paramServer.SetParameter("/navigation/goal_tolerance", 0.2f);
            paramServer.SetParameter("/navigation/max_linear_speed", 1.0f);
            paramServer.SetParameter("/navigation/max_angular_speed", 1.0f);
            paramServer.SetParameter("/navigation/control_rate", 100);

            // Path planning parameters
            paramServer.SetParameter("/path_planner/grid_width", 100);
            paramServer.SetParameter("/path_planner/grid_height", 100);
            paramServer.SetParameter("/path_planner/resolution", 0.1f);
            paramServer.SetParameter("/path_planner/map_publish_rate", 1000);

            // Monitoring parameters
            paramServer.SetParameter("/monitor/check_interval", 5000);
            paramServer.SetParameter("/network_monitor/report_interval", 10000);
            paramServer.SetParameter("/health_check/interval", 30000);

            Console.WriteLine("Parameter server initialized");
        }

        static void ShowSystemStatus(RobotSystem robotSystem)
        {
            var status = robotSystem.GetStatus();
            
            Console.WriteLine();
            Console.WriteLine("=== System Status ===");
            Console.WriteLine($"System running status: {(status.IsRunning ? "Running" : "Stopped")}");
            Console.WriteLine($"Total nodes: {status.TotalNodes}");
            Console.WriteLine($"Running nodes: {status.RunningNodes}");
            Console.WriteLine("Active topics:");
            
            foreach (var topic in status.Topics)
            {
                Console.WriteLine($"  - {topic}");
            }
            
            Console.WriteLine("Node list:");
            foreach (var node in robotSystem.GetNodes())
            {
                Console.WriteLine($"  - {node.NodeName}: {(node.IsRunning ? "Running" : "Stopped")}");
            }
            Console.WriteLine();
        }

        static async Task TestManualAction(RobotControlNode robotNode)
        {
            Console.WriteLine();
            Console.WriteLine("=== Manual Action Test ===");
            Console.WriteLine("Available actions: wave, hug, dance, nod, shake_head");
            Console.Write("Please enter action name: ");
            
            var action = Console.ReadLine();
            
            if (!string.IsNullOrWhiteSpace(action))
            {
                var actionMessage = new RobotActionMessage(action.Trim(), null, "Manual");
                robotNode.Publish("/robot/action", actionMessage);
                Console.WriteLine($"Action command sent: {action}");
            }
            Console.WriteLine();
        }
    }
}