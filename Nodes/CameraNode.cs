using System;
using System.Threading;
using System.Threading.Tasks;
using RobotFramework.Core;
using RobotFramework.Messages;

namespace RobotFramework.Nodes
{
    /// <summary>
    /// Camera node - simulates camera data capture and publishing
    /// </summary>
    public class CameraNode : BaseNode
    {
        private Timer? _captureTimer;
        private int _frameCount = 0;

        public CameraNode() : base("CameraNode")
        {
        }

        protected override void OnStart()
        {
            this.LogInfo("Camera node starting...");
            
            // Start camera capture timer
            _captureTimer = new Timer(CaptureFrame, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(33)); // 30 FPS
            
            this.LogInfo("Camera node started successfully");
        }

        protected override void OnStop()
        {
            this.LogInfo("Camera node stopping...");
            
            _captureTimer?.Dispose();
            _captureTimer = null;
            
            this.LogInfo("Camera node stopped");
        }

        private void CaptureFrame(object? state)
        {
            try
            {
                // Simulate camera data capture
                var imageMessage = new ImageMessage
                {
                    Header = new Header
                    {
                        Timestamp = DateTime.UtcNow,
                        FrameId = "camera_frame"
                    },
                    Width = 640,
                    Height = 480,
                    Encoding = "rgb8",
                    Data = GenerateSimulatedImageData()
                };

                // Publish image data
                Publish("/camera/image_raw", imageMessage);
                
                _frameCount++;
                
                if (_frameCount % 30 == 0) // Log every second
                {
                    this.LogDebug($"Published frame {_frameCount}");
                }
            }
            catch (Exception ex)
            {
                this.LogError($"Error capturing frame: {ex.Message}");
            }
        }

        private byte[] GenerateSimulatedImageData()
        {
            // Generate simulated RGB image data
            var width = 640;
            var height = 480;
            var data = new byte[width * height * 3]; // RGB format
            
            var random = new Random();
            random.NextBytes(data);
            
            return data;
        }
    }
}