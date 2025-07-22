using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RobotFramework.Core
{
    /// <summary>
    /// Parameter server - similar to ROS parameter server, used for storing and managing global configuration parameters
    /// </summary>
    public class ParameterServer
    {
        private static readonly Lazy<ParameterServer> _instance = new(() => new ParameterServer());
        public static ParameterServer Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, object> _parameters;

        private ParameterServer()
        {
            _parameters = new ConcurrentDictionary<string, object>();
            LoadDefaultParameters();
        }

        /// <summary>
        /// Set parameter
        /// </summary>
        public void SetParameter<T>(string name, T value)
        {
            _parameters.AddOrUpdate(name, value, (key, oldValue) => value);
            Console.WriteLine($"[ParameterServer] Set parameter: {name} = {value}");
        }

        /// <summary>
        /// Get parameter
        /// </summary>
        public T GetParameter<T>(string name, T defaultValue = default)
        {
            if (_parameters.TryGetValue(name, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Check if parameter exists
        /// </summary>
        public bool HasParameter(string name)
        {
            return _parameters.ContainsKey(name);
        }

        /// <summary>
        /// Delete parameter
        /// </summary>
        public bool DeleteParameter(string name)
        {
            return _parameters.TryRemove(name, out _);
        }

        /// <summary>
        /// Get all parameter names
        /// </summary>
        public IEnumerable<string> GetParameterNames()
        {
            return _parameters.Keys;
        }

        /// <summary>
        /// Load default parameters
        /// </summary>
        private void LoadDefaultParameters()
        {
            // System parameters
            SetParameter("/robot/name", "RobotFramework");
            SetParameter("/robot/version", "1.0.0");
            
            // Camera parameters
            SetParameter("/camera/fps", 30);
            SetParameter("/camera/width", 640);
            SetParameter("/camera/height", 480);
            
            // Emotion detection parameters
            SetParameter("/emotion/confidence_threshold", 0.7);
            SetParameter("/emotion/detection_interval", 100); // ms
            
            // Robot control parameters
            SetParameter("/robot/max_speed", 1.0);
            SetParameter("/robot/acceleration", 0.5);
            SetParameter("/robot/response_delay", 500); // ms
        }
    }
}