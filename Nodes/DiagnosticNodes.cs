using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RobotFramework.Core;
using RobotFramework.Messages;

namespace RobotFramework.Nodes
{
    /// <summary>
    /// System monitoring node - monitors system resources and node status
    /// </summary>
    public class SystemMonitorNode : BaseNode
    {
        private readonly Dictionary<string, DateTime> _nodeLastSeen;
        private readonly Dictionary<string, int> _nodeMessageCounts;
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _memoryCounter;

        public SystemMonitorNode(string name = "SystemMonitorNode") : base(name)
        {
            _nodeLastSeen = new Dictionary<string, DateTime>();
            _nodeMessageCounts = new Dictionary<string, int>();
            
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch (Exception ex)
            {
                this.LogWarn($"Unable to initialize performance counters: {ex.Message}");
            }
        }

        protected override Task OnStartAsync()
        {
            this.LogInfo("System monitoring node started");
            
            // Subscribe to all messages to monitor node activity
            Subscribe<BaseMessage>("/+", OnAnyMessage); // Wildcard subscription
            
            return Task.CompletedTask;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var monitorInterval = ParameterServer.Instance.GetParameter("/monitor/check_interval", 5000); // 5 seconds
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Check system resources
                    var systemDiagnostic = CheckSystemResources();
                    Publish("/diagnostics/system", systemDiagnostic);
                    
                    // Check node status
                    var nodesDiagnostic = CheckNodesStatus();
                    Publish("/diagnostics/nodes", nodesDiagnostic);
                    
                    // Clean up expired node records
                    CleanupOldNodeRecords();

                    await Task.Delay(monitorInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    this.LogError($"System monitoring error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handle any message to track node activity
        /// </summary>
        private void OnAnyMessage(BaseMessage message)
        {
            var senderName = message.SenderId;
            var currentTime = DateTime.Now;
            
            _nodeLastSeen[senderName] = currentTime;
            
            if (_nodeMessageCounts.ContainsKey(senderName))
                _nodeMessageCounts[senderName]++;
            else
                _nodeMessageCounts[senderName] = 1;
        }

        /// <summary>
        /// Check system resources
        /// </summary>
        private DiagnosticMessage CheckSystemResources()
        {
            var diagnostic = new DiagnosticMessage("system_resources", Name);
            
            try
            {
                // CPU usage
                if (_cpuCounter != null)
                {
                    var cpuUsage = _cpuCounter.NextValue();
                    diagnostic.AddValue("cpu_usage_percent", cpuUsage.ToString("F2"));
                    
                    if (cpuUsage > 80)
                        diagnostic.Level = DiagnosticLevel.Warn;
                    else if (cpuUsage > 95)
                        diagnostic.Level = DiagnosticLevel.Error;
                }
                
                // Memory usage
                if (_memoryCounter != null)
                {
                    var availableMemory = _memoryCounter.NextValue();
                    diagnostic.AddValue("available_memory_mb", availableMemory.ToString("F0"));
                    
                    if (availableMemory < 500)
                        diagnostic.Level = DiagnosticLevel.Warn;
                    else if (availableMemory < 200)
                        diagnostic.Level = DiagnosticLevel.Error;
                }
                
                // Process information
                var currentProcess = Process.GetCurrentProcess();
                var workingSet = currentProcess.WorkingSet64 / (1024 * 1024); // MB
                var threadCount = currentProcess.Threads.Count;
                
                diagnostic.AddValue("process_memory_mb", workingSet.ToString());
                diagnostic.AddValue("thread_count", threadCount.ToString());
                
                diagnostic.Message = $"System resources normal - Memory: {workingSet}MB, Threads: {threadCount}";
            }
            catch (Exception ex)
            {
                diagnostic.Level = DiagnosticLevel.Error;
                diagnostic.Message = $"System resources check failed: {ex.Message}";
            }
            
            return diagnostic;
        }

        /// <summary>
        /// Check node status
        /// </summary>
        private DiagnosticMessage CheckNodesStatus()
        {
            var diagnostic = new DiagnosticMessage("nodes_status", Name);
            var currentTime = DateTime.Now;
            var timeoutThreshold = TimeSpan.FromSeconds(30); // 30 second timeout
            
            var activeNodes = 0;
            var inactiveNodes = 0;
            var totalMessages = 0;
            
            foreach (var kvp in _nodeLastSeen)
            {
                var nodeName = kvp.Key;
                var lastSeen = kvp.Value;
                var messageCount = _nodeMessageCounts.GetValueOrDefault(nodeName, 0);
                
                totalMessages += messageCount;
                
                if (currentTime - lastSeen < timeoutThreshold)
                {
                    activeNodes++;
                }
                else
                {
                    inactiveNodes++;
                    this.LogWarn($"Node {nodeName} may be offline, last activity: {lastSeen}");
                }
            }
            
            diagnostic.AddValue("active_nodes", activeNodes.ToString());
            diagnostic.AddValue("inactive_nodes", inactiveNodes.ToString());
            diagnostic.AddValue("total_messages", totalMessages.ToString());
            
            if (inactiveNodes > 0)
            {
                diagnostic.Level = DiagnosticLevel.Warn;
                diagnostic.Message = $"Detected {inactiveNodes} inactive nodes";
            }
            else
            {
                diagnostic.Message = $"All {activeNodes} nodes running normally";
            }
            
            return diagnostic;
        }

        /// <summary>
        /// Clean up expired node records
        /// </summary>
        private void CleanupOldNodeRecords()
        {
            var currentTime = DateTime.Now;
            var cleanupThreshold = TimeSpan.FromMinutes(10); // Clean up after 10 minutes
            
            var expiredNodes = _nodeLastSeen
                .Where(kvp => currentTime - kvp.Value > cleanupThreshold)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var nodeName in expiredNodes)
            {
                _nodeLastSeen.Remove(nodeName);
                _nodeMessageCounts.Remove(nodeName);
                this.LogDebug($"Cleaned up expired node record: {nodeName}");
            }
        }

        protected override Task OnStopAsync()
        {
            this.LogInfo("System monitoring node stopped");
            
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
            
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Network monitoring node - monitors network connections and communication quality
    /// </summary>
    public class NetworkMonitorNode : BaseNode
    {
        private readonly Dictionary<string, List<DateTime>> _messageTimestamps;
        private readonly Dictionary<string, TimeSpan> _averageLatencies;

        public NetworkMonitorNode(string name = "NetworkMonitorNode") : base(name)
        {
            _messageTimestamps = new Dictionary<string, List<DateTime>>();
            _averageLatencies = new Dictionary<string, TimeSpan>();
        }

        protected override Task OnStartAsync()
        {
            this.LogInfo("Network monitoring node started");
            
            // Subscribe to all messages to monitor communication
            Subscribe<BaseMessage>("/+", OnMessageReceived);
            
            return Task.CompletedTask;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var reportInterval = ParameterServer.Instance.GetParameter("/network_monitor/report_interval", 10000); // 10 seconds
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var networkDiagnostic = AnalyzeNetworkPerformance();
                    Publish("/diagnostics/network", networkDiagnostic);
                    
                    // Clean up old timestamps
                    CleanupOldTimestamps();

                    await Task.Delay(reportInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    this.LogError($"Network monitoring error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handle received messages
        /// </summary>
        private void OnMessageReceived(BaseMessage message)
        {
            var topic = message.GetType().Name;
            var currentTime = DateTime.Now;
            
            if (!_messageTimestamps.ContainsKey(topic))
                _messageTimestamps[topic] = new List<DateTime>();
            
            _messageTimestamps[topic].Add(currentTime);
            
            // Calculate latency (if message contains timestamp)
            var latency = currentTime - message.Timestamp;
            if (!_averageLatencies.ContainsKey(topic))
                _averageLatencies[topic] = latency;
            else
                _averageLatencies[topic] = TimeSpan.FromMilliseconds(
                    (_averageLatencies[topic].TotalMilliseconds + latency.TotalMilliseconds) / 2);
        }

        /// <summary>
        /// Analyze network performance
        /// </summary>
        private DiagnosticMessage AnalyzeNetworkPerformance()
        {
            var diagnostic = new DiagnosticMessage("network_performance", Name);
            var currentTime = DateTime.Now;
            var analysisWindow = TimeSpan.FromMinutes(1); // Analyze last 1 minute of data
            
            var totalMessages = 0;
            var topicCount = 0;
            var maxLatency = TimeSpan.Zero;
            var avgLatency = TimeSpan.Zero;
            
            foreach (var kvp in _messageTimestamps)
            {
                var topic = kvp.Key;
                var timestamps = kvp.Value;
                
                // Calculate message count in recent 1 minute
                var recentMessages = timestamps.Count(t => currentTime - t < analysisWindow);
                totalMessages += recentMessages;
                
                if (recentMessages > 0)
                {
                    topicCount++;
                    
                    // Calculate message frequency
                    var frequency = recentMessages / analysisWindow.TotalSeconds;
                    diagnostic.AddValue($"{topic}_frequency_hz", frequency.ToString("F2"));
                    
                    // Get latency information
                    if (_averageLatencies.ContainsKey(topic))
                    {
                        var latency = _averageLatencies[topic];
                        if (latency > maxLatency)
                            maxLatency = latency;
                        
                        avgLatency = TimeSpan.FromMilliseconds(
                            (avgLatency.TotalMilliseconds + latency.TotalMilliseconds) / 2);
                        
                        diagnostic.AddValue($"{topic}_latency_ms", latency.TotalMilliseconds.ToString("F2"));
                    }
                }
            }
            
            diagnostic.AddValue("total_messages_per_minute", totalMessages.ToString());
            diagnostic.AddValue("active_topics", topicCount.ToString());
            diagnostic.AddValue("max_latency_ms", maxLatency.TotalMilliseconds.ToString("F2"));
            diagnostic.AddValue("avg_latency_ms", avgLatency.TotalMilliseconds.ToString("F2"));
            
            // Evaluate network status
            if (maxLatency.TotalMilliseconds > 1000) // 1 second
            {
                diagnostic.Level = DiagnosticLevel.Error;
                diagnostic.Message = $"Network latency too high: {maxLatency.TotalMilliseconds:F0}ms";
            }
            else if (maxLatency.TotalMilliseconds > 500) // 500ms
            {
                diagnostic.Level = DiagnosticLevel.Warn;
                diagnostic.Message = $"Network latency high: {maxLatency.TotalMilliseconds:F0}ms";
            }
            else
            {
                diagnostic.Message = $"Network performance good - Average latency: {avgLatency.TotalMilliseconds:F0}ms";
            }
            
            return diagnostic;
        }

        /// <summary>
        /// Clean up old timestamps
        /// </summary>
        private void CleanupOldTimestamps()
        {
            var currentTime = DateTime.Now;
            var retentionPeriod = TimeSpan.FromMinutes(5); // Keep 5 minutes of data
            
            foreach (var kvp in _messageTimestamps.ToList())
            {
                var topic = kvp.Key;
                var timestamps = kvp.Value;
                
                // Remove expired timestamps
                timestamps.RemoveAll(t => currentTime - t > retentionPeriod);
                
                // If no timestamps left, remove the entire entry
                if (timestamps.Count == 0)
                {
                    _messageTimestamps.Remove(topic);
                    _averageLatencies.Remove(topic);
                }
            }
        }

        protected override Task OnStopAsync()
        {
            this.LogInfo("Network monitoring node stopped");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Health check node - periodically checks system health status
    /// </summary>
    public class HealthCheckNode : BaseNode
    {
        private readonly List<string> _criticalNodes;
        private readonly Dictionary<string, DiagnosticMessage> _lastDiagnostics;

        public HealthCheckNode(string name = "HealthCheckNode") : base(name)
        {
            _criticalNodes = new List<string>
            {
                "CameraNode",
                "LidarNode",
                "OdometryNode",
                "NavigationNode"
            };
            _lastDiagnostics = new Dictionary<string, DiagnosticMessage>();
        }

        protected override Task OnStartAsync()
        {
            this.LogInfo("Health check node started");
            
            // Subscribe to diagnostic messages
            Subscribe<DiagnosticMessage>("/diagnostics/+", OnDiagnosticReceived);
            
            return Task.CompletedTask;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var checkInterval = ParameterServer.Instance.GetParameter("/health_check/interval", 30000); // 30 seconds
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var healthReport = GenerateHealthReport();
                    Publish("/health_report", healthReport);
                    
                    // Check if alerts need to be sent
                    CheckForAlerts(healthReport);

                    await Task.Delay(checkInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    this.LogError($"Health check error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handle diagnostic messages
        /// </summary>
        private void OnDiagnosticReceived(DiagnosticMessage message)
        {
            _lastDiagnostics[message.Name] = message;
        }

        /// <summary>
        /// Generate health report
        /// </summary>
        private DiagnosticMessage GenerateHealthReport()
        {
            var healthReport = new DiagnosticMessage("system_health", Name);
            var currentTime = DateTime.Now;
            
            var okCount = 0;
            var warnCount = 0;
            var errorCount = 0;
            var missingCount = 0;
            
            // Check critical nodes
            foreach (var nodeName in _criticalNodes)
            {
                var nodeKey = $"nodes_{nodeName.ToLower()}";
                
                if (_lastDiagnostics.ContainsKey(nodeKey))
                {
                    var diagnostic = _lastDiagnostics[nodeKey];
                    var age = currentTime - diagnostic.Timestamp;
                    
                    if (age.TotalMinutes > 2) // 2 minutes without update
                    {
                        missingCount++;
                        healthReport.AddValue($"{nodeName}_status", "STALE");
                    }
                    else
                    {
                        switch (diagnostic.Level)
                        {
                            case DiagnosticLevel.Ok:
                                okCount++;
                                healthReport.AddValue($"{nodeName}_status", "OK");
                                break;
                            case DiagnosticLevel.Warn:
                                warnCount++;
                                healthReport.AddValue($"{nodeName}_status", "WARN");
                                break;
                            case DiagnosticLevel.Error:
                                errorCount++;
                                healthReport.AddValue($"{nodeName}_status", "ERROR");
                                break;
                        }
                    }
                }
                else
                {
                    missingCount++;
                    healthReport.AddValue($"{nodeName}_status", "MISSING");
                }
            }
            
            // Check system resources
            if (_lastDiagnostics.ContainsKey("system_resources"))
            {
                var sysResource = _lastDiagnostics["system_resources"];
                healthReport.AddValue("system_status", sysResource.Level.ToString());
            }
            
            // Check network performance
            if (_lastDiagnostics.ContainsKey("network_performance"))
            {
                var networkPerf = _lastDiagnostics["network_performance"];
                healthReport.AddValue("network_status", networkPerf.Level.ToString());
            }
            
            // Set overall health status
            healthReport.AddValue("ok_count", okCount.ToString());
            healthReport.AddValue("warn_count", warnCount.ToString());
            healthReport.AddValue("error_count", errorCount.ToString());
            healthReport.AddValue("missing_count", missingCount.ToString());
            
            if (errorCount > 0 || missingCount > 0)
            {
                healthReport.Level = DiagnosticLevel.Error;
                healthReport.Message = $"System health status: Error - {errorCount} errors, {missingCount} missing";
            }
            else if (warnCount > 0)
            {
                healthReport.Level = DiagnosticLevel.Warn;
                healthReport.Message = $"System health status: Warning - {warnCount} warnings";
            }
            else
            {
                healthReport.Level = DiagnosticLevel.Ok;
                healthReport.Message = $"System health status: Good - {okCount} nodes running normally";
            }
            
            return healthReport;
        }

        /// <summary>
        /// Check if alerts need to be sent
        /// </summary>
        private void CheckForAlerts(DiagnosticMessage healthReport)
        {
            if (healthReport.Level == DiagnosticLevel.Error)
            {
                this.LogError($"System health alert: {healthReport.Message}");
                
                // Additional alert mechanisms can be added here, such as sending emails, SMS, etc.
                var alertMessage = new SystemStatusMessage(
                    "CRITICAL",
                    healthReport.Message,
                    Name
                );
                Publish("/alerts/critical", alertMessage);
            }
            else if (healthReport.Level == DiagnosticLevel.Warn)
            {
                this.LogWarn($"System health warning: {healthReport.Message}");
                
                var alertMessage = new SystemStatusMessage(
                    "WARNING",
                    healthReport.Message,
                    Name
                );
                Publish("/alerts/warning", alertMessage);
            }
        }

        protected override Task OnStopAsync()
        {
            this.LogInfo("Health check node stopped");
            return Task.CompletedTask;
        }
    }
}