using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RobotFramework.Core;

namespace RobotFramework.Core
{
    /// <summary>
    /// Robot system manager - responsible for managing the lifecycle of all nodes
    /// </summary>
    public class RobotSystem
    {
        private readonly List<INode> _nodes;
        private readonly MessageBus _messageBus;
        
        public bool IsRunning { get; private set; }

        public RobotSystem()
        {
            _nodes = new List<INode>();
            _messageBus = MessageBus.Instance;
        }

        /// <summary>
        /// Add a node to the system
        /// </summary>
        public void AddNode(INode node)
        {
            if (_nodes.Any(n => n.NodeName == node.NodeName))
            {
                throw new InvalidOperationException($"Node with name '{node.NodeName}' already exists");
            }

            _nodes.Add(node);
            Console.WriteLine($"[RobotSystem] Added node: {node.NodeName}");
        }

        /// <summary>
        /// Remove a node
        /// </summary>
        public void RemoveNode(INode node)
        {
            _nodes.Remove(node);
            Console.WriteLine($"[RobotSystem] Removed node: {node.NodeName}");
        }

        /// <summary>
        /// Start all nodes
        /// </summary>
        public async Task StartAsync()
        {
            if (IsRunning)
            {
                Console.WriteLine("[RobotSystem] System is already running");
                return;
            }

            Console.WriteLine("[RobotSystem] Starting robot system...");

            foreach (var node in _nodes)
            {
                try
                {
                    await node.StartAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RobotSystem] Failed to start node {node.NodeName}: {ex.Message}");
                }
            }

            IsRunning = true;
            Console.WriteLine("[RobotSystem] Robot system startup complete");
        }

        /// <summary>
        /// Stop all nodes
        /// </summary>
        public async Task StopAsync()
        {
            if (!IsRunning)
            {
                Console.WriteLine("[RobotSystem] System is not running");
                return;
            }

            Console.WriteLine("[RobotSystem] Stopping robot system...");

            foreach (var node in _nodes)
            {
                try
                {
                    await node.StopAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RobotSystem] Failed to stop node {node.NodeName}: {ex.Message}");
                }
            }

            IsRunning = false;
            Console.WriteLine("[RobotSystem] Robot system stopped");
        }

        /// <summary>
        /// Get system status
        /// </summary>
        public SystemStatus GetStatus()
        {
            return new SystemStatus
            {
                IsRunning = IsRunning,
                TotalNodes = _nodes.Count,
                RunningNodes = _nodes.Count(n => n.IsRunning),
                Topics = _messageBus.GetTopics().ToList()
            };
        }

        /// <summary>
        /// Get all nodes
        /// </summary>
        public IEnumerable<INode> GetNodes()
        {
            return _nodes.AsReadOnly();
        }
    }

    /// <summary>
    /// System status information
    /// </summary>
    public class SystemStatus
    {
        public bool IsRunning { get; set; }
        public int TotalNodes { get; set; }
        public int RunningNodes { get; set; }
        public List<string> Topics { get; set; } = new();
    }
}