using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using RobotFramework.Core;
using RobotFramework.Messages;

namespace RobotFramework.Nodes
{
    /// <summary>
    /// Navigation node - implements simple navigation functionality
    /// </summary>
    public class NavigationNode : BaseNode
    {
        private Vector3 _currentGoal;
        private Vector3 _currentPosition;
        private bool _hasGoal;
        private readonly float _goalTolerance;
        private readonly float _maxLinearSpeed;
        private readonly float _maxAngularSpeed;

        public NavigationNode(string name = "NavigationNode") : base(name)
        {
            _goalTolerance = ParameterServer.Instance.GetParameter("/navigation/goal_tolerance", 0.2f);
            _maxLinearSpeed = ParameterServer.Instance.GetParameter("/navigation/max_linear_speed", 1.0f);
            _maxAngularSpeed = ParameterServer.Instance.GetParameter("/navigation/max_angular_speed", 1.0f);
            _hasGoal = false;
        }

        protected override Task OnStartAsync()
        {
            this.LogInfo("Navigation node started");
            
            // Subscribe to target position
            Subscribe<GoalMessage>("/move_base_simple/goal", OnGoalReceived);
            
            // Subscribe to odometry data
            Subscribe<OdometryMessage>("/odom", OnOdometryReceived);
            
            return Task.CompletedTask;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var controlInterval = ParameterServer.Instance.GetParameter("/navigation/control_rate", 100); // 10Hz
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_hasGoal)
                    {
                        var twist = CalculateVelocityCommand();
                        Publish("/cmd_vel", twist);
                        
                        // Check if target is reached
                        if (IsGoalReached())
                        {
                            this.LogInfo("Target reached!");
                            _hasGoal = false;
                            
                            // Stop robot
                            var stopTwist = new TwistMessage(Vector3.Zero, Vector3.Zero, Name);
                            Publish("/cmd_vel", stopTwist);
                        }
                    }

                    await Task.Delay(controlInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    this.LogError($"Navigation control error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handle target position
        /// </summary>
        private void OnGoalReceived(GoalMessage message)
        {
            _currentGoal = message.Position;
            _hasGoal = true;
            this.LogInfo($"Received new target: {_currentGoal}");
        }

        /// <summary>
        /// Handle odometry data
        /// </summary>
        private void OnOdometryReceived(OdometryMessage message)
        {
            _currentPosition = message.Position;
        }

        /// <summary>
        /// Calculate velocity command
        /// </summary>
        private TwistMessage CalculateVelocityCommand()
        {
            var goalVector = _currentGoal - _currentPosition;
            var distance = goalVector.Length();
            
            if (distance < _goalTolerance)
            {
                return new TwistMessage(Vector3.Zero, Vector3.Zero, Name);
            }

            // Simple proportional control
            var direction = Vector3.Normalize(goalVector);
            var linearSpeed = Math.Min(_maxLinearSpeed, distance * 2.0f); // Proportional control
            
            var linearVelocity = direction * linearSpeed;
            
            // Simplified: only consider planar motion, ignore angular velocity control
            var angularVelocity = Vector3.Zero;
            
            return new TwistMessage(linearVelocity, angularVelocity, Name);
        }

        /// <summary>
        /// Check if target is reached
        /// </summary>
        private bool IsGoalReached()
        {
            var distance = Vector3.Distance(_currentPosition, _currentGoal);
            return distance < _goalTolerance;
        }

        protected override Task OnStopAsync()
        {
            this.LogInfo("Navigation node stopped");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Path planning node - implements A* path planning algorithm
    /// </summary>
    public class PathPlannerNode : BaseNode
    {
        private readonly Random _random = new();
        private readonly Dictionary<string, Vector3> _waypoints = new();

        public PathPlannerNode() : base("PathPlannerNode")
        {
            InitializeWaypoints();
        }

        protected override void OnStart()
        {
            this.LogInfo("Path planner node starting...");
            
            // Subscribe to goal requests
            Subscribe<GoalMessage>("/move_base_simple/goal", OnGoalReceived);
            
            this.LogInfo("Path planner node started successfully");
        }

        protected override void OnStop()
        {
            this.LogInfo("Path planner node stopping...");
            
            Unsubscribe("/move_base_simple/goal");
            
            this.LogInfo("Path planner node stopped");
        }

        private void InitializeWaypoints()
        {
            // Initialize some predefined waypoints
            _waypoints["home"] = new Vector3(0, 0, 0);
            _waypoints["kitchen"] = new Vector3(5, 3, 0);
            _waypoints["living_room"] = new Vector3(-2, 4, 0);
            _waypoints["bedroom"] = new Vector3(3, -2, 0);
            _waypoints["office"] = new Vector3(-4, -1, 0);
        }

        private void OnGoalReceived(GoalMessage goalMessage)
        {
            this.LogInfo($"Received navigation goal: ({goalMessage.TargetPose.Position.X:F2}, {goalMessage.TargetPose.Position.Y:F2})");
            
            try
            {
                // Get current robot position from TF
                var currentTransform = TransformBuffer.Instance.LookupTransform("map", "base_link", DateTime.UtcNow);
                var currentPosition = currentTransform?.Translation ?? Vector3.Zero;
                
                // Plan path
                var path = PlanPath(currentPosition, goalMessage.TargetPose.Position);
                
                if (path != null && path.Length > 0)
                {
                    // Create path message
                    var pathMessage = new PathMessage
                    {
                        Header = new Header
                        {
                            Timestamp = DateTime.UtcNow,
                            FrameId = "map"
                        },
                        Poses = path.Select(pos => new PoseStamped
                        {
                            Header = new Header
                            {
                                Timestamp = DateTime.UtcNow,
                                FrameId = "map"
                            },
                            Pose = new Pose
                            {
                                Position = pos,
                                Orientation = new Quaternion { X = 0, Y = 0, Z = 0, W = 1 }
                            }
                        }).ToArray()
                    };
                    
                    // Publish planned path
                    Publish("/planned_path", pathMessage);
                    
                    this.LogInfo($"Path planned successfully with {path.Length} waypoints");
                    
                    // Start path execution
                    ExecutePath(path);
                }
                else
                {
                    this.LogWarning("Failed to plan path to target");
                }
            }
            catch (Exception ex)
            {
                this.LogError($"Error processing navigation goal: {ex.Message}");
            }
        }

        private Vector3[] PlanPath(Vector3 start, Vector3 goal)
        {
            // Simple path planning - direct line with some waypoints
            var path = new List<Vector3>();
            
            // Add start position
            path.Add(start);
            
            // Calculate direction and distance
            var direction = goal - start;
            var distance = direction.Length();
            
            if (distance > 0.1f)
            {
                direction = Vector3.Normalize(direction);
                
                // Add intermediate waypoints every meter
                var numWaypoints = (int)(distance / 1.0f);
                for (int i = 1; i <= numWaypoints; i++)
                {
                    var waypoint = start + direction * (i * 1.0f);
                    // Add some randomness to avoid obstacles
                    waypoint.X += (float)(_random.NextDouble() - 0.5) * 0.2f;
                    waypoint.Y += (float)(_random.NextDouble() - 0.5) * 0.2f;
                    path.Add(waypoint);
                }
            }
            
            // Add goal position
            path.Add(goal);
            
            return path.ToArray();
        }

        private async void ExecutePath(Vector3[] path)
        {
            this.LogInfo("Starting path execution...");
            
            for (int i = 0; i < path.Length - 1; i++)
            {
                var currentWaypoint = path[i];
                var nextWaypoint = path[i + 1];
                
                // Calculate velocity command to reach next waypoint
                var direction = nextWaypoint - currentWaypoint;
                var distance = direction.Length();
                
                if (distance > 0.1f)
                {
                    direction = Vector3.Normalize(direction);
                    
                    // Create velocity command
                    var twistMessage = new TwistMessage
                    {
                        Linear = new Vector3
                        {
                            X = Math.Min(0.5f, distance), // Max 0.5 m/s
                            Y = 0,
                            Z = 0
                        },
                        Angular = new Vector3
                        {
                            X = 0,
                            Y = 0,
                            Z = 0 // No rotation for now
                        }
                    };
                    
                    // Publish velocity command
                    Publish("/cmd_vel", twistMessage);
                    
                    this.LogDebug($"Moving to waypoint {i + 1}/{path.Length - 1}");
                    
                    // Wait for movement
                    await Task.Delay(2000);
                }
            }
            
            // Stop robot
            var stopMessage = new TwistMessage
            {
                Linear = Vector3.Zero,
                Angular = Vector3.Zero
            };
            Publish("/cmd_vel", stopMessage);
            
            this.LogInfo("Path execution completed");
        }
    }

    /// <summary>
    /// Map server node - provides static map data
    /// </summary>
    public class MapServerNode : BaseNode
    {
        private readonly int _mapWidth = 100;
        private readonly int _mapHeight = 100;
        private readonly float _mapResolution = 0.1f; // 10cm per pixel
        private byte[] _mapData;

        public MapServerNode() : base("MapServerNode")
        {
            GenerateMap();
        }

        protected override void OnStart()
        {
            this.LogInfo("Map server node starting...");
            
            // Publish map periodically
            var mapTimer = new Timer(PublishMap, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            
            this.LogInfo("Map server node started successfully");
        }

        protected override void OnStop()
        {
            this.LogInfo("Map server node stopped");
        }

        private void GenerateMap()
        {
            _mapData = new byte[_mapWidth * _mapHeight];
            var random = new Random();
            
            // Generate simple map with obstacles
            for (int y = 0; y < _mapHeight; y++)
            {
                for (int x = 0; x < _mapWidth; x++)
                {
                    var index = y * _mapWidth + x;
                    
                    // Borders are obstacles
                    if (x == 0 || x == _mapWidth - 1 || y == 0 || y == _mapHeight - 1)
                    {
                        _mapData[index] = 100; // Occupied
                    }
                    // Random obstacles
                    else if (random.NextDouble() < 0.1) // 10% obstacle probability
                    {
                        _mapData[index] = 100; // Occupied
                    }
                    else
                    {
                        _mapData[index] = 0; // Free space
                    }
                }
            }
            
            this.LogInfo($"Generated map: {_mapWidth}x{_mapHeight} with resolution {_mapResolution}m/pixel");
        }

        private void PublishMap(object? state)
        {
            try
            {
                var mapMessage = new OccupancyGridMessage
                {
                    Header = new Header
                    {
                        Timestamp = DateTime.UtcNow,
                        FrameId = "map"
                    },
                    Info = new MapMetaData
                    {
                        MapLoadTime = DateTime.UtcNow,
                        Resolution = _mapResolution,
                        Width = (uint)_mapWidth,
                        Height = (uint)_mapHeight,
                        Origin = new Pose
                        {
                            Position = new Vector3 { X = -5.0f, Y = -5.0f, Z = 0.0f },
                            Orientation = new Quaternion { X = 0, Y = 0, Z = 0, W = 1 }
                        }
                    },
                    Data = _mapData
                };
                
                Publish("/map", mapMessage);
                
                this.LogDebug("Published map data");
            }
            catch (Exception ex)
            {
                this.LogError($"Error publishing map: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// A* algorithm node
    /// </summary>
    internal class AStarNode
    {
        public int X { get; }
        public int Y { get; }
        public float G { get; set; } // Actual cost from start to current point
        public float H { get; } // Heuristic cost from current point to end
        public float F => G + H; // Total cost

        public AStarNode(int x, int y, float g, float h)
        {
            X = x;
            Y = y;
            G = g;
            H = h;
        }
    }
}