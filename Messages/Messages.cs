using System;
using System.Collections.Generic;
using System.Numerics;

namespace RobotFramework.Messages
{
    /// <summary>
    /// Base message class
    /// </summary>
    public abstract class BaseMessage
    {
        public DateTime Timestamp { get; set; }
        public string SourceNode { get; set; }
        public string MessageId { get; set; }

        protected BaseMessage(string sourceNode = "")
        {
            Timestamp = DateTime.UtcNow;
            SourceNode = sourceNode;
            MessageId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Robot action message
    /// </summary>
    public class RobotActionMessage : BaseMessage
    {
        public string ActionType { get; set; }
        public object Parameters { get; set; }

        public RobotActionMessage(string actionType, object parameters = null, string sourceNode = "") 
            : base(sourceNode)
        {
            ActionType = actionType;
            Parameters = parameters;
        }
    }

    /// <summary>
    /// Sensor data message
    /// </summary>
    public class SensorDataMessage : BaseMessage
    {
        public string SensorType { get; set; }
        public object Data { get; set; }

        public SensorDataMessage(string sensorType, object data, string sourceNode = "") 
            : base(sourceNode)
        {
            SensorType = sensorType;
            Data = data;
        }
    }

    /// <summary>
    /// Emotion detection message
    /// </summary>
    public class EmotionMessage : BaseMessage
    {
        public string Emotion { get; set; }
        public float Confidence { get; set; }

        public EmotionMessage(string emotion, float confidence, string sourceNode = "") 
            : base(sourceNode)
        {
            Emotion = emotion;
            Confidence = confidence;
        }
    }

    /// <summary>
    /// System status message
    /// </summary>
    public class SystemStatusMessage : BaseMessage
    {
        public string Status { get; set; }
        public string Details { get; set; }

        public SystemStatusMessage(string status, string details = "", string sourceNode = "") 
            : base(sourceNode)
        {
            Status = status;
            Details = details;
        }
    }

    // ===== New ROS standard message types =====

    /// <summary>
    /// Image message - similar to sensor_msgs/Image
    /// </summary>
    public class ImageMessage : BaseMessage
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Encoding { get; set; } // "rgb8", "bgr8", "mono8", etc.
        public byte[] Data { get; set; }
        public string FrameId { get; set; }

        public ImageMessage(int width, int height, string encoding, byte[] data, string frameId = "camera_link", string sourceNode = "")
            : base(sourceNode)
        {
            Width = width;
            Height = height;
            Encoding = encoding;
            Data = data;
            FrameId = frameId;
        }
    }

    /// <summary>
    /// Point cloud message - similar to sensor_msgs/PointCloud2
    /// </summary>
    public class PointCloudMessage : BaseMessage
    {
        public Vector3[] Points { get; set; }
        public string FrameId { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public PointCloudMessage(Vector3[] points, string frameId = "laser_link", string sourceNode = "")
            : base(sourceNode)
        {
            Points = points;
            FrameId = frameId;
            Width = points.Length;
            Height = 1;
        }
    }

    /// <summary>
    /// Laser scan message - similar to sensor_msgs/LaserScan
    /// </summary>
    public class LaserScanMessage : BaseMessage
    {
        public float AngleMin { get; set; }
        public float AngleMax { get; set; }
        public float AngleIncrement { get; set; }
        public float TimeIncrement { get; set; }
        public float ScanTime { get; set; }
        public float RangeMin { get; set; }
        public float RangeMax { get; set; }
        public float[] Ranges { get; set; }
        public float[] Intensities { get; set; }
        public string FrameId { get; set; }

        public LaserScanMessage(float[] ranges, string frameId = "laser_link", string sourceNode = "")
            : base(sourceNode)
        {
            Ranges = ranges;
            FrameId = frameId;
            AngleMin = -3.14159f;
            AngleMax = 3.14159f;
            AngleIncrement = 6.28318f / ranges.Length;
            RangeMin = 0.1f;
            RangeMax = 10.0f;
        }
    }

    /// <summary>
    /// IMU message - similar to sensor_msgs/Imu
    /// </summary>
    public class ImuMessage : BaseMessage
    {
        public Quaternion Orientation { get; set; }
        public Vector3 AngularVelocity { get; set; }
        public Vector3 LinearAcceleration { get; set; }
        public string FrameId { get; set; }

        public ImuMessage(Quaternion orientation, Vector3 angularVelocity, Vector3 linearAcceleration, string frameId = "imu_link", string sourceNode = "")
            : base(sourceNode)
        {
            Orientation = orientation;
            AngularVelocity = angularVelocity;
            LinearAcceleration = linearAcceleration;
            FrameId = frameId;
        }
    }

    /// <summary>
    /// Odometry message - similar to nav_msgs/Odometry
    /// </summary>
    public class OdometryMessage : BaseMessage
    {
        public Vector3 Position { get; set; }
        public Quaternion Orientation { get; set; }
        public Vector3 LinearVelocity { get; set; }
        public Vector3 AngularVelocity { get; set; }
        public string FrameId { get; set; }
        public string ChildFrameId { get; set; }

        public OdometryMessage(Vector3 position, Quaternion orientation, string frameId = "odom", string childFrameId = "base_link", string sourceNode = "")
            : base(sourceNode)
        {
            Position = position;
            Orientation = orientation;
            FrameId = frameId;
            ChildFrameId = childFrameId;
            LinearVelocity = Vector3.Zero;
            AngularVelocity = Vector3.Zero;
        }
    }

    /// <summary>
    /// Twist message - similar to geometry_msgs/Twist
    /// </summary>
    public class TwistMessage : BaseMessage
    {
        public Vector3 Linear { get; set; }
        public Vector3 Angular { get; set; }

        public TwistMessage(Vector3 linear, Vector3 angular, string sourceNode = "")
            : base(sourceNode)
        {
            Linear = linear;
            Angular = angular;
        }

        public TwistMessage(float linearX = 0, float linearY = 0, float linearZ = 0,
                           float angularX = 0, float angularY = 0, float angularZ = 0, string sourceNode = "")
            : base(sourceNode)
        {
            Linear = new Vector3(linearX, linearY, linearZ);
            Angular = new Vector3(angularX, angularY, angularZ);
        }
    }

    /// <summary>
    /// Path message - similar to nav_msgs/Path
    /// </summary>
    public class PathMessage : BaseMessage
    {
        public Vector3[] Poses { get; set; }
        public string FrameId { get; set; }

        public PathMessage(Vector3[] poses, string frameId = "map", string sourceNode = "")
            : base(sourceNode)
        {
            Poses = poses;
            FrameId = frameId;
        }
    }

    /// <summary>
    /// Goal message - similar to geometry_msgs/PoseStamped
    /// </summary>
    public class GoalMessage : BaseMessage
    {
        public Vector3 Position { get; set; }
        public Quaternion Orientation { get; set; }
        public string FrameId { get; set; }

        public GoalMessage(Vector3 position, Quaternion orientation, string frameId = "map", string sourceNode = "")
            : base(sourceNode)
        {
            Position = position;
            Orientation = orientation;
            FrameId = frameId;
        }
    }

    /// <summary>
    /// Diagnostic message - similar to diagnostic_msgs/DiagnosticArray
    /// </summary>
    public class DiagnosticMessage : BaseMessage
    {
        public string Level { get; set; } // "OK", "WARN", "ERROR", "STALE"
        public string Name { get; set; }
        public string Message { get; set; }
        public string HardwareId { get; set; }
        public Dictionary<string, string> Values { get; set; }

        public DiagnosticMessage(string level, string name, string message, string hardwareId = "", string sourceNode = "")
            : base(sourceNode)
        {
            Level = level;
            Name = name;
            Message = message;
            HardwareId = hardwareId;
            Values = new Dictionary<string, string>();
        }
    }
}