using System;
using System.Threading.Tasks;

namespace RobotFramework.Core
{
    /// <summary>
    /// Robot node interface - similar to Node concept in ROS
    /// </summary>
    public interface INode
    {
        string NodeName { get; }
        bool IsRunning { get; }
        
        Task StartAsync();
        Task StopAsync();
        void Subscribe<T>(string topic, Action<T> callback) where T : class;
        void Publish<T>(string topic, T message) where T : class;
    }
}