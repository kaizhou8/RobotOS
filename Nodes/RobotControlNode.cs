using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RobotFramework.Core;
using RobotFramework.Messages;

namespace RobotFramework.Nodes
{
    /// <summary>
    /// Robot control node - handles robot movement and action execution
    /// </summary>
    public class RobotControlNode : BaseNode
    {
        private readonly Dictionary<string, RobotAction> _availableActions;
        private RobotAction? _currentAction;
        private readonly object _actionLock = new();

        public RobotControlNode() : base("RobotControlNode")
        {
            _availableActions = new Dictionary<string, RobotAction>
            {
                { "wave", new RobotAction("wave", "Wave hand", 3000) },
                { "nod", new RobotAction("nod", "Nod head", 2000) },
                { "dance", new RobotAction("dance", "Dance movement", 5000) },
                { "greet", new RobotAction("greet", "Greeting gesture", 2500) },
                { "bow", new RobotAction("bow", "Bow gesture", 3000) }
            };
        }

        protected override void OnStart()
        {
            this.LogInfo("Robot control node starting...");
            
            // Subscribe to emotion detection results
            Subscribe<EmotionMessage>("/emotion/detected", OnEmotionDetected);
            
            // Subscribe to velocity commands
            Subscribe<TwistMessage>("/cmd_vel", OnVelocityCommand);
            
            // Register action server
            ActionServer.Instance.RegisterAction<RobotActionGoal, RobotActionResult, RobotActionFeedback>(
                "robot_action", ExecuteRobotAction);
            
            this.LogInfo("Robot control node started successfully");
        }

        protected override void OnStop()
        {
            this.LogInfo("Robot control node stopping...");
            
            // Cancel current action
            lock (_actionLock)
            {
                _currentAction = null;
            }
            
            // Unsubscribe from all topics
            Unsubscribe("/emotion/detected");
            Unsubscribe("/cmd_vel");
            
            // Unregister action server
            ActionServer.Instance.UnregisterAction("robot_action");
            
            this.LogInfo("Robot control node stopped");
        }

        private void OnEmotionDetected(EmotionMessage emotionMessage)
        {
            try
            {
                if (!emotionMessage.FaceDetected)
                    return;

                this.LogDebug($"Received emotion: {emotionMessage.Emotion} (confidence: {emotionMessage.Confidence:F2})");

                // React to detected emotion
                var reactionAction = GetEmotionReaction(emotionMessage.Emotion);
                if (reactionAction != null)
                {
                    this.LogInfo($"Reacting to emotion '{emotionMessage.Emotion}' with action '{reactionAction}'");
                    
                    // Send action goal
                    var goal = new RobotActionGoal
                    {
                        ActionName = reactionAction,
                        Parameters = new Dictionary<string, object>
                        {
                            { "emotion", emotionMessage.Emotion },
                            { "confidence", emotionMessage.Confidence }
                        }
                    };
                    
                    ActionServer.Instance.SendGoal("robot_action", goal);
                }
            }
            catch (Exception ex)
            {
                this.LogError($"Error processing emotion: {ex.Message}");
            }
        }

        private void OnVelocityCommand(TwistMessage twistMessage)
        {
            try
            {
                this.LogDebug($"Received velocity command - Linear: ({twistMessage.Linear.X:F2}, {twistMessage.Linear.Y:F2}, {twistMessage.Linear.Z:F2}), " +
                             $"Angular: ({twistMessage.Angular.X:F2}, {twistMessage.Angular.Y:F2}, {twistMessage.Angular.Z:F2})");

                // Execute movement command
                ExecuteMovement(twistMessage);
            }
            catch (Exception ex)
            {
                this.LogError($"Error executing velocity command: {ex.Message}");
            }
        }

        private async Task<RobotActionResult> ExecuteRobotAction(RobotActionGoal goal, 
            Action<RobotActionFeedback> feedbackCallback, CancellationToken cancellationToken)
        {
            try
            {
                this.LogInfo($"Executing robot action: {goal.ActionName}");

                if (!_availableActions.TryGetValue(goal.ActionName, out var action))
                {
                    var errorResult = new RobotActionResult
                    {
                        Success = false,
                        Message = $"Unknown action: {goal.ActionName}",
                        ExecutionTime = 0
                    };
                    return errorResult;
                }

                lock (_actionLock)
                {
                    _currentAction = action;
                }

                var startTime = DateTime.UtcNow;
                var duration = action.Duration;
                var steps = 10;
                var stepDuration = duration / steps;

                // Execute action with feedback
                for (int i = 0; i < steps && !cancellationToken.IsCancellationRequested; i++)
                {
                    var progress = (float)(i + 1) / steps;
                    var feedback = new RobotActionFeedback
                    {
                        Progress = progress,
                        CurrentStep = $"Executing {action.Name} - Step {i + 1}/{steps}",
                        EstimatedTimeRemaining = (int)((steps - i - 1) * stepDuration)
                    };

                    feedbackCallback(feedback);
                    this.LogDebug($"Action progress: {progress:P0}");

                    await Task.Delay(stepDuration, cancellationToken);
                }

                lock (_actionLock)
                {
                    _currentAction = null;
                }

                var executionTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                
                var result = new RobotActionResult
                {
                    Success = !cancellationToken.IsCancellationRequested,
                    Message = cancellationToken.IsCancellationRequested ? "Action cancelled" : "Action completed successfully",
                    ExecutionTime = executionTime
                };

                this.LogInfo($"Robot action '{goal.ActionName}' completed in {executionTime}ms");
                return result;
            }
            catch (Exception ex)
            {
                this.LogError($"Error executing robot action: {ex.Message}");
                return new RobotActionResult
                {
                    Success = false,
                    Message = $"Action execution failed: {ex.Message}",
                    ExecutionTime = 0
                };
            }
        }

        private void ExecuteMovement(TwistMessage twist)
        {
            // Simulate robot movement
            this.LogDebug($"Executing movement - Linear velocity: {twist.Linear.X:F2} m/s, Angular velocity: {twist.Angular.Z:F2} rad/s");
            
            // In a real robot, this would control motors, wheels, etc.
            // Here we just simulate the movement
            
            // Publish movement status
            var statusMessage = new SystemStatusMessage
            {
                Header = new Header
                {
                    Timestamp = DateTime.UtcNow,
                    FrameId = "base_link"
                },
                Level = 0, // INFO
                Name = "movement",
                Message = $"Moving with linear velocity {twist.Linear.X:F2} m/s, angular velocity {twist.Angular.Z:F2} rad/s",
                HardwareId = "robot_base",
                Values = new Dictionary<string, string>
                {
                    { "linear_x", twist.Linear.X.ToString("F2") },
                    { "linear_y", twist.Linear.Y.ToString("F2") },
                    { "angular_z", twist.Angular.Z.ToString("F2") }
                }
            };
            
            Publish("/diagnostics", statusMessage);
        }

        private string? GetEmotionReaction(string emotion)
        {
            return emotion.ToLower() switch
            {
                "happy" => "wave",
                "sad" => "nod",
                "angry" => "bow",
                "surprised" => "greet",
                "neutral" => null,
                "fear" => "nod",
                "disgust" => null,
                _ => null
            };
        }

        public Dictionary<string, RobotAction> GetAvailableActions()
        {
            return new Dictionary<string, RobotAction>(_availableActions);
        }

        public RobotAction? GetCurrentAction()
        {
            lock (_actionLock)
            {
                return _currentAction;
            }
        }
    }
}