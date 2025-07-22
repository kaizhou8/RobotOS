using System;
using System.Threading;
using System.Threading.Tasks;

namespace RobotFramework.Core
{
    /// <summary>
    /// Base node implementation
    /// </summary>
    public abstract class BaseNode : INode
    {
        private readonly MessageBus _messageBus;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task? _nodeTask;

        public string NodeName { get; }
        public bool IsRunning { get; private set; }

        protected BaseNode(string nodeName)
        {
            NodeName = nodeName;
            _messageBus = MessageBus.Instance;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Start the node
        /// </summary>
        public virtual async Task StartAsync()
        {
            if (IsRunning)
            {
                Console.WriteLine($"[{NodeName}] Node is already running");
                return;
            }

            try
            {
                Console.WriteLine($"[{NodeName}] Starting node...");
                
                await OnStartAsync();
                
                _nodeTask = RunAsync(_cancellationTokenSource.Token);
                IsRunning = true;
                
                Console.WriteLine($"[{NodeName}] Node started");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{NodeName}] Failed to start: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stop the node
        /// </summary>
        public virtual async Task StopAsync()
        {
            if (!IsRunning)
            {
                Console.WriteLine($"[{NodeName}] Node is not running");
                return;
            }

            Console.WriteLine($"[{NodeName}] Stopping node...");
            
            _cancellationTokenSource.Cancel();
            
            if (_nodeTask != null)
            {
                try
                {
                    await _nodeTask;
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation
                }
            }
            
            await OnStopAsync();
            IsRunning = false;
            
            Console.WriteLine($"[{NodeName}] Node stopped");
        }

        /// <summary>
        /// Subscribe to a topic
        /// </summary>
        protected void Subscribe<T>(string topic, Action<T> handler) where T : BaseMessage
        {
            _messageBus.Subscribe(topic, handler);
        }

        /// <summary>
        /// Publish a message
        /// </summary>
        protected void Publish<T>(string topic, T message) where T : BaseMessage
        {
            _messageBus.Publish(topic, message);
        }

        /// <summary>
        /// Initialization logic when node starts
        /// </summary>
        protected virtual Task OnStartAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Cleanup logic when node stops
        /// </summary>
        protected virtual Task OnStopAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Node main loop - must be implemented by subclasses
        /// </summary>
        protected abstract Task RunAsync(CancellationToken cancellationToken);
    }
}