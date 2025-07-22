using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RobotFramework.Core
{
    /// <summary>
    /// Message bus - responsible for message passing between nodes
    /// </summary>
    public class MessageBus
    {
        private static readonly Lazy<MessageBus> _instance = new(() => new MessageBus());
        public static MessageBus Instance => _instance.Value;

        private readonly Dictionary<string, List<object>> _subscribers;
        private readonly object _lock = new object();

        private MessageBus()
        {
            _subscribers = new Dictionary<string, List<object>>();
        }

        /// <summary>
        /// Subscribe to a topic
        /// </summary>
        public void Subscribe<T>(string topic, Action<T> callback) where T : BaseMessage
        {
            lock (_lock)
            {
                if (!_subscribers.ContainsKey(topic))
                {
                    _subscribers[topic] = new List<object>();
                }

                _subscribers[topic].Add(callback);
            }

            Console.WriteLine($"[MessageBus] Subscribed to topic: {topic}");
        }

        /// <summary>
        /// Publish a message to a topic
        /// </summary>
        public void Publish<T>(string topic, T message) where T : BaseMessage
        {
            List<object>? callbacks = null;

            lock (_lock)
            {
                if (_subscribers.ContainsKey(topic))
                {
                    callbacks = new List<object>(_subscribers[topic]);
                }
            }

            if (callbacks != null)
            {
                foreach (var callback in callbacks)
                {
                    if (callback is Action<T> typedCallback)
                    {
                        typedCallback(message);
                    }
                }
            }

            Console.WriteLine($"[MessageBus] Published message to topic: {topic}");
        }

        /// <summary>
        /// Unsubscribe
        /// </summary>
        public void Unsubscribe<T>(string topic, Action<T> callback) where T : BaseMessage
        {
            lock (_lock)
            {
                if (_subscribers.ContainsKey(topic))
                {
                    _subscribers[topic].Remove(callback);
                    if (_subscribers[topic].Count == 0)
                    {
                        _subscribers.Remove(topic);
                    }
                }
            }
        }

        /// <summary>
        /// Get all topics
        /// </summary>
        public IEnumerable<string> GetTopics()
        {
            lock (_lock)
            {
                return _subscribers.Keys.ToList();
            }
        }
    }
}