using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RobotFramework.Core
{
    /// <summary>
    /// Action goal base class - similar to ROS action goal
    /// </summary>
    public abstract class ActionGoal
    {
        public string GoalId { get; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Action result base class - similar to ROS action result
    /// </summary>
    public abstract class ActionResult
    {
        public string GoalId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Action feedback base class - similar to ROS action feedback
    /// </summary>
    public abstract class ActionFeedback
    {
        public string GoalId { get; set; } = string.Empty;
        public float Progress { get; set; } // 0.0 to 1.0
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Action status enumeration
    /// </summary>
    public enum ActionStatus
    {
        Pending,    // Waiting to start
        Active,     // Currently executing
        Succeeded,  // Completed successfully
        Aborted,    // Terminated due to error
        Rejected,   // Rejected before execution
        Preempted,  // Cancelled by new goal
        Recalled    // Cancelled before execution
    }

    /// <summary>
    /// Action execution context
    /// </summary>
    public class ActionContext
    {
        public string GoalId { get; set; } = string.Empty;
        public ActionStatus Status { get; set; } = ActionStatus.Pending;
        public CancellationTokenSource CancellationTokenSource { get; set; } = new();
        public Task? ExecutionTask { get; set; }
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Action handler delegate
    /// </summary>
    public delegate Task<ActionResult> ActionHandler(ActionGoal goal, Action<ActionFeedback> feedbackCallback, CancellationToken cancellationToken);

    /// <summary>
    /// Action server - similar to ROS action server
    /// Provides asynchronous long-running task execution with feedback
    /// </summary>
    public class ActionServer
    {
        private static ActionServer? _instance;
        private static readonly object _lock = new object();
        private readonly Dictionary<string, ActionHandler> _actions = new();
        private readonly Dictionary<string, ActionContext> _activeGoals = new();

        private ActionServer() { }

        public static ActionServer Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ActionServer();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Register action
        /// </summary>
        public bool RegisterAction(string actionName, ActionHandler handler)
        {
            lock (_lock)
            {
                if (_actions.ContainsKey(actionName))
                {
                    Console.WriteLine($"[ActionServer] Action {actionName} already exists");
                    return false;
                }

                _actions[actionName] = handler;
                Console.WriteLine($"[ActionServer] Action registered: {actionName}");
                return true;
            }
        }

        /// <summary>
        /// Send goal to action
        /// </summary>
        public async Task<string?> SendGoalAsync(string actionName, ActionGoal goal, Action<ActionFeedback>? feedbackCallback = null)
        {
            ActionHandler? handler;
            lock (_lock)
            {
                if (!_actions.TryGetValue(actionName, out handler))
                {
                    Console.WriteLine($"[ActionServer] Action not found: {actionName}");
                    return null;
                }
            }

            var context = new ActionContext
            {
                GoalId = goal.GoalId,
                Status = ActionStatus.Active
            };

            lock (_lock)
            {
                _activeGoals[goal.GoalId] = context;
            }

            Console.WriteLine($"[ActionServer] Goal sent: {actionName}, GoalId: {goal.GoalId}");

            // Execute action asynchronously
            context.ExecutionTask = Task.Run(async () =>
            {
                try
                {
                    var result = await handler(goal, feedbackCallback ?? (_ => { }), context.CancellationTokenSource.Token);
                    
                    lock (_lock)
                    {
                        if (_activeGoals.ContainsKey(goal.GoalId))
                        {
                            context.Status = result.Success ? ActionStatus.Succeeded : ActionStatus.Aborted;
                        }
                    }

                    Console.WriteLine($"[ActionServer] Goal completed: {goal.GoalId}, Success: {result.Success}");
                    return result;
                }
                catch (OperationCanceledException)
                {
                    lock (_lock)
                    {
                        if (_activeGoals.ContainsKey(goal.GoalId))
                        {
                            context.Status = ActionStatus.Preempted;
                        }
                    }
                    Console.WriteLine($"[ActionServer] Goal cancelled: {goal.GoalId}");
                    throw;
                }
                catch (Exception ex)
                {
                    lock (_lock)
                    {
                        if (_activeGoals.ContainsKey(goal.GoalId))
                        {
                            context.Status = ActionStatus.Aborted;
                        }
                    }
                    Console.WriteLine($"[ActionServer] Goal failed: {goal.GoalId}, Error: {ex.Message}");
                    throw;
                }
                finally
                {
                    lock (_lock)
                    {
                        _activeGoals.Remove(goal.GoalId);
                    }
                }
            });

            return goal.GoalId;
        }

        /// <summary>
        /// Cancel goal
        /// </summary>
        public bool CancelGoal(string goalId)
        {
            lock (_lock)
            {
                if (_activeGoals.TryGetValue(goalId, out var context))
                {
                    context.CancellationTokenSource.Cancel();
                    context.Status = ActionStatus.Preempted;
                    Console.WriteLine($"[ActionServer] Goal cancelled: {goalId}");
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Get goal status
        /// </summary>
        public ActionStatus? GetGoalStatus(string goalId)
        {
            lock (_lock)
            {
                return _activeGoals.TryGetValue(goalId, out var context) ? context.Status : null;
            }
        }

        /// <summary>
        /// Get all active goals
        /// </summary>
        public List<string> GetActiveGoals()
        {
            lock (_lock)
            {
                return new List<string>(_activeGoals.Keys);
            }
        }

        /// <summary>
        /// Unregister action
        /// </summary>
        public bool UnregisterAction(string actionName)
        {
            lock (_lock)
            {
                if (_actions.Remove(actionName))
                {
                    Console.WriteLine($"[ActionServer] Action unregistered: {actionName}");
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Get all action names
        /// </summary>
        public List<string> GetActionNames()
        {
            lock (_lock)
            {
                return new List<string>(_actions.Keys);
            }
        }
    }
}