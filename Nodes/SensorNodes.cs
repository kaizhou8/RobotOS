using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using RobotFramework.Core;
using RobotFramework.Messages;

namespace RobotFramework.Nodes
{
    /// <summary>
    /// Lidar sensor node - simulates laser range finder data
    /// </summary>
    public class LidarNode : BaseNode
    {
        private Timer? _scanTimer;
        private int _scanCount = 0;
        private readonly Random _random = new();

        public LidarNode() : base("LidarNode")
        {
        }

        protected override void OnStart()
        {
            this.LogInfo("Lidar node starting...");
            
            // Start lidar scanning timer (10 Hz)
            _scanTimer = new Timer(PublishScan, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
            
            this.LogInfo("Lidar node started successfully");
        }

        protected override void OnStop()
        {
            this.LogInfo("Lidar node stopping...");
            
            _scanTimer?.Dispose();
            _scanTimer = null;
            
            this.LogInfo("Lidar node stopped");
        }

        private void PublishScan(object? state)
        {
            try
            {
                var scanMessage = new LaserScanMessage
                {
                    Header = new Header
                    {
                        Timestamp = DateTime.UtcNow,
                        FrameId = "laser_frame"
                    },
                    AngleMin = -3.14159f, // -180 degrees
                    AngleMax = 3.14159f,  // 180 degrees
                    AngleIncrement = 0.01745f, // 1 degree
                    TimeIncrement = 0.0001f,
                    ScanTime = 0.1f,
                    RangeMin = 0.1f,
                    RangeMax = 10.0f,
                    Ranges = GenerateSimulatedRanges(),
                    Intensities = GenerateSimulatedIntensities()
                };

                Publish("/scan", scanMessage);
                
                _scanCount++;
                
                if (_scanCount % 50 == 0) // Log every 5 seconds
                {
                    this.LogDebug($"Published scan {_scanCount}");
                }
            }
            catch (Exception ex)
            {
                this.LogError($"Error publishing lidar scan: {ex.Message}");
            }
        }

        private float[] GenerateSimulatedRanges()
        {
            var numRanges = 360; // 360 degree scan
            var ranges = new float[numRanges];
            
            for (int i = 0; i < numRanges; i++)
            {
                // Simulate obstacles at various distances
                ranges[i] = 1.0f + (float)(_random.NextDouble() * 8.0); // 1-9 meters
            }
            
            return ranges;
        }

        private float[] GenerateSimulatedIntensities()
        {
            var numRanges = 360;
            var intensities = new float[numRanges];
            
            for (int i = 0; i < numRanges; i++)
            {
                intensities[i] = (float)_random.NextDouble(); // 0-1 intensity
            }
            
            return intensities;
        }
    }

    /// <summary>
    /// IMU sensor node - simulates inertial measurement unit data
    /// </summary>
    public class ImuNode : BaseNode
    {
        private Timer? _imuTimer;
        private int _imuCount = 0;
        private readonly Random _random = new();

        public ImuNode() : base("ImuNode")
        {
        }

        protected override void OnStart()
        {
            this.LogInfo("IMU node starting...");
            
            // Start IMU data publishing timer (100 Hz)
            _imuTimer = new Timer(PublishImuData, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(10));
            
            this.LogInfo("IMU node started successfully");
        }

        protected override void OnStop()
        {
            this.LogInfo("IMU node stopping...");
            
            _imuTimer?.Dispose();
            _imuTimer = null;
            
            this.LogInfo("IMU node stopped");
        }

        private void PublishImuData(object? state)
        {
            try
            {
                var imuMessage = new ImuMessage
                {
                    Header = new Header
                    {
                        Timestamp = DateTime.UtcNow,
                        FrameId = "imu_frame"
                    },
                    Orientation = new Quaternion
                    {
                        X = (float)(_random.NextDouble() * 0.1 - 0.05),
                        Y = (float)(_random.NextDouble() * 0.1 - 0.05),
                        Z = (float)(_random.NextDouble() * 0.1 - 0.05),
                        W = 1.0f
                    },
                    OrientationCovariance = new float[9] { 0.01f, 0, 0, 0, 0.01f, 0, 0, 0, 0.01f },
                    AngularVelocity = new Vector3
                    {
                        X = (float)(_random.NextDouble() * 0.2 - 0.1),
                        Y = (float)(_random.NextDouble() * 0.2 - 0.1),
                        Z = (float)(_random.NextDouble() * 0.2 - 0.1)
                    },
                    AngularVelocityCovariance = new float[9] { 0.001f, 0, 0, 0, 0.001f, 0, 0, 0, 0.001f },
                    LinearAcceleration = new Vector3
                    {
                        X = (float)(_random.NextDouble() * 0.5 - 0.25),
                        Y = (float)(_random.NextDouble() * 0.5 - 0.25),
                        Z = 9.81f + (float)(_random.NextDouble() * 0.2 - 0.1) // Gravity + noise
                    },
                    LinearAccelerationCovariance = new float[9] { 0.01f, 0, 0, 0, 0.01f, 0, 0, 0, 0.01f }
                };

                Publish("/imu/data", imuMessage);
                
                _imuCount++;
                
                if (_imuCount % 1000 == 0) // Log every 10 seconds
                {
                    this.LogDebug($"Published IMU data {_imuCount}");
                }
            }
            catch (Exception ex)
            {
                this.LogError($"Error publishing IMU data: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Odometry node - simulates robot odometry and publishes TF transforms
    /// </summary>
    public class OdometryNode : BaseNode
    {
        private Timer? _odomTimer;
        private int _odomCount = 0;
        private readonly Random _random = new();
        
        // Robot pose
        private float _x = 0.0f;
        private float _y = 0.0f;
        private float _theta = 0.0f;
        
        // Current velocity
        private float _linearVel = 0.0f;
        private float _angularVel = 0.0f;
        
        private DateTime _lastTime = DateTime.UtcNow;

        public OdometryNode() : base("OdometryNode")
        {
        }

        protected override void OnStart()
        {
            this.LogInfo("Odometry node starting...");
            
            // Subscribe to velocity commands
            Subscribe<TwistMessage>("/cmd_vel", OnVelocityCommand);
            
            // Start odometry publishing timer (50 Hz)
            _odomTimer = new Timer(PublishOdometry, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(20));
            
            this.LogInfo("Odometry node started successfully");
        }

        protected override void OnStop()
        {
            this.LogInfo("Odometry node stopping...");
            
            _odomTimer?.Dispose();
            _odomTimer = null;
            
            Unsubscribe("/cmd_vel");
            
            this.LogInfo("Odometry node stopped");
        }

        private void OnVelocityCommand(TwistMessage twistMessage)
        {
            _linearVel = twistMessage.Linear.X;
            _angularVel = twistMessage.Angular.Z;
        }

        private void PublishOdometry(object? state)
        {
            try
            {
                var currentTime = DateTime.UtcNow;
                var dt = (float)(currentTime - _lastTime).TotalSeconds;
                _lastTime = currentTime;

                // Update robot pose based on velocity
                UpdatePose(dt);

                // Create odometry message
                var odomMessage = new OdometryMessage
                {
                    Header = new Header
                    {
                        Timestamp = currentTime,
                        FrameId = "odom"
                    },
                    ChildFrameId = "base_link",
                    Pose = new PoseWithCovariance
                    {
                        Pose = new Pose
                        {
                            Position = new Vector3 { X = _x, Y = _y, Z = 0.0f },
                            Orientation = new Quaternion
                            {
                                X = 0.0f,
                                Y = 0.0f,
                                Z = (float)Math.Sin(_theta / 2.0),
                                W = (float)Math.Cos(_theta / 2.0)
                            }
                        },
                        Covariance = new float[36] // 6x6 covariance matrix
                    },
                    Twist = new TwistWithCovariance
                    {
                        Twist = new Twist
                        {
                            Linear = new Vector3 { X = _linearVel, Y = 0.0f, Z = 0.0f },
                            Angular = new Vector3 { X = 0.0f, Y = 0.0f, Z = _angularVel }
                        },
                        Covariance = new float[36] // 6x6 covariance matrix
                    }
                };

                // Set covariance values
                SetCovarianceValues(odomMessage);

                // Publish odometry
                Publish("/odom", odomMessage);

                // Publish TF transform
                PublishTransform(currentTime);
                
                _odomCount++;
                
                if (_odomCount % 250 == 0) // Log every 5 seconds
                {
                    this.LogDebug($"Published odometry {_odomCount} - Position: ({_x:F2}, {_y:F2}), Theta: {_theta:F2}");
                }
            }
            catch (Exception ex)
            {
                this.LogError($"Error publishing odometry: {ex.Message}");
            }
        }

        private void UpdatePose(float dt)
        {
            // Simple differential drive model
            var dx = _linearVel * (float)Math.Cos(_theta) * dt;
            var dy = _linearVel * (float)Math.Sin(_theta) * dt;
            var dtheta = _angularVel * dt;

            _x += dx;
            _y += dy;
            _theta += dtheta;

            // Normalize theta to [-pi, pi]
            while (_theta > Math.PI) _theta -= 2.0f * (float)Math.PI;
            while (_theta < -Math.PI) _theta += 2.0f * (float)Math.PI;
        }

        private void SetCovarianceValues(OdometryMessage odomMessage)
        {
            // Set pose covariance (6x6 matrix: x, y, z, roll, pitch, yaw)
            odomMessage.Pose.Covariance[0] = 0.01f;  // x
            odomMessage.Pose.Covariance[7] = 0.01f;  // y
            odomMessage.Pose.Covariance[35] = 0.01f; // yaw

            // Set twist covariance (6x6 matrix: vx, vy, vz, vroll, vpitch, vyaw)
            odomMessage.Twist.Covariance[0] = 0.01f;  // vx
            odomMessage.Twist.Covariance[35] = 0.01f; // vyaw
        }

        private void PublishTransform(DateTime timestamp)
        {
            // Publish TF transform from odom to base_link
            var transform = new Transform
            {
                Translation = new Vector3 { X = _x, Y = _y, Z = 0.0f },
                Rotation = new Quaternion
                {
                    X = 0.0f,
                    Y = 0.0f,
                    Z = (float)Math.Sin(_theta / 2.0),
                    W = (float)Math.Cos(_theta / 2.0)
                }
            };

            TransformBuffer.Instance.SetTransform("odom", "base_link", transform, timestamp);
        }
    }
}