using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RobotFramework.Core
{
    /// <summary>
    /// Service request base class - similar to ROS service request
    /// </summary>
    public abstract class ServiceRequest
    {
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Service response base class - similar to ROS service response
    /// </summary>
    public abstract class ServiceResponse
    {
        public string RequestId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Service handler delegate
    /// </summary>
    public delegate Task<TResponse> ServiceHandler<TRequest, TResponse>(TRequest request)
        where TRequest : ServiceRequest
        where TResponse : ServiceResponse;

    /// <summary>
    /// Service manager - similar to ROS service mechanism
    /// </summary>
    public class ServiceManager
    {
        private static readonly Lazy<ServiceManager> _instance = new(() => new ServiceManager());
        public static ServiceManager Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, object> _services;

        private ServiceManager()
        {
            _services = new ConcurrentDictionary<string, object>();
        }

        /// <summary>
        /// Register service
        /// </summary>
        public void RegisterService<TRequest, TResponse>(string serviceName, ServiceHandler<TRequest, TResponse> handler)
            where TRequest : ServiceRequest
            where TResponse : ServiceResponse
        {
            _services.AddOrUpdate(serviceName, handler, (key, oldValue) => handler);
            Console.WriteLine($"[ServiceManager] Service registered: {serviceName}");
        }

        /// <summary>
        /// Call service
        /// </summary>
        public async Task<TResponse> CallServiceAsync<TRequest, TResponse>(string serviceName, TRequest request)
            where TRequest : ServiceRequest
            where TResponse : ServiceResponse
        {
            if (_services.TryGetValue(serviceName, out var handler) && 
                handler is ServiceHandler<TRequest, TResponse> typedHandler)
            {
                try
                {
                    Console.WriteLine($"[ServiceManager] Calling service: {serviceName}");
                    return await typedHandler(request);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ServiceManager] Service call failed: {serviceName}, Error: {ex.Message}");
                    throw;
                }
            }

            throw new InvalidOperationException($"Service not found: {serviceName}");
        }

        /// <summary>
        /// Unregister service
        /// </summary>
        public bool UnregisterService(string serviceName)
        {
            var result = _services.TryRemove(serviceName, out _);
            if (result)
            {
                Console.WriteLine($"[ServiceManager] Service unregistered: {serviceName}");
            }
            return result;
        }

        /// <summary>
        /// Get all service names
        /// </summary>
        public string[] GetServiceNames()
        {
            return _services.Keys.ToArray();
        }
    }
}