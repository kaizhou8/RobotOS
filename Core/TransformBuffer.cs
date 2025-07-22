using System;
using System.Collections.Concurrent;
using System.Numerics;

namespace RobotFramework.Core
{
    /// <summary>
    /// 3D Transform class - similar to ROS tf2 Transform
    /// Represents transformation between coordinate frames
    /// </summary>
    public class Transform
    {
        public Vector3 Translation { get; set; }
        public Quaternion Rotation { get; set; }
        public DateTime Timestamp { get; set; }
        public string ParentFrame { get; set; }
        public string ChildFrame { get; set; }

        public Transform()
        {
            Translation = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Timestamp = DateTime.Now;
        }

        public Transform(Vector3 translation, Quaternion rotation, string parentFrame, string childFrame)
        {
            Translation = translation;
            Rotation = rotation;
            ParentFrame = parentFrame;
            ChildFrame = childFrame;
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Convert to 4x4 transformation matrix
        /// </summary>
        public Matrix4x4 ToMatrix()
        {
            return Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Translation);
        }

        /// <summary>
        /// Create transform from 4x4 matrix
        /// </summary>
        public static Transform FromMatrix(Matrix4x4 matrix, string parentFrame, string childFrame)
        {
            Matrix4x4.Decompose(matrix, out _, out var rotation, out var translation);
            return new Transform(translation, rotation, parentFrame, childFrame);
        }
    }

    /// <summary>
    /// Transform buffer - similar to ROS tf2 TransformBuffer
    /// Manages coordinate transformations between different frames
    /// </summary>
    public class TransformBuffer
    {
        private static readonly Lazy<TransformBuffer> _instance = new(() => new TransformBuffer());
        public static TransformBuffer Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Transform>> _transforms;
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(10);

        private TransformBuffer()
        {
            _transforms = new ConcurrentDictionary<string, ConcurrentDictionary<string, Transform>>();
            InitializeDefaultFrames();
        }

        /// <summary>
        /// Set transform between frames
        /// </summary>
        public void SetTransform(Transform transform)
        {
            var parentFrames = _transforms.GetOrAdd(transform.ParentFrame, _ => new ConcurrentDictionary<string, Transform>());
            parentFrames.AddOrUpdate(transform.ChildFrame, transform, (key, oldValue) => transform);
            
            Console.WriteLine($"[TransformBuffer] Transform set: {transform.ParentFrame} -> {transform.ChildFrame}");
        }

        /// <summary>
        /// Lookup transform between frames
        /// </summary>
        public Transform LookupTransform(string targetFrame, string sourceFrame, DateTime time = default)
        {
            if (time == default)
                time = DateTime.Now;

            // Direct lookup
            if (_transforms.TryGetValue(targetFrame, out var childFrames) &&
                childFrames.TryGetValue(sourceFrame, out var transform))
            {
                if (IsTransformValid(transform, time))
                    return transform;
            }

            // Reverse lookup
            if (_transforms.TryGetValue(sourceFrame, out var reverseChildFrames) &&
                reverseChildFrames.TryGetValue(targetFrame, out var reverseTransform))
            {
                if (IsTransformValid(reverseTransform, time))
                    return GetInverseTransform(reverseTransform);
            }

            // TODO: Implement chain transformation lookup
            throw new InvalidOperationException($"Cannot find transform from {sourceFrame} to {targetFrame}");
        }

        /// <summary>
        /// Transform point from source frame to target frame
        /// </summary>
        public Vector3 TransformPoint(Vector3 point, string targetFrame, string sourceFrame)
        {
            var transform = LookupTransform(targetFrame, sourceFrame);
            var matrix = transform.ToMatrix();
            return Vector3.Transform(point, matrix);
        }

        /// <summary>
        /// Transform vector from source frame to target frame
        /// </summary>
        public Vector3 TransformVector(Vector3 vector, string targetFrame, string sourceFrame)
        {
            var transform = LookupTransform(targetFrame, sourceFrame);
            return Vector3.Transform(vector, transform.Rotation);
        }

        /// <summary>
        /// Check if transform is still valid
        /// </summary>
        private bool IsTransformValid(Transform transform, DateTime time)
        {
            return Math.Abs((time - transform.Timestamp).TotalSeconds) <= _cacheTimeout.TotalSeconds;
        }

        /// <summary>
        /// Get inverse transform
        /// </summary>
        private Transform GetInverseTransform(Transform transform)
        {
            var inverseRotation = Quaternion.Conjugate(transform.Rotation);
            var inverseTranslation = Vector3.Transform(-transform.Translation, inverseRotation);
            
            return new Transform(inverseTranslation, inverseRotation, transform.ChildFrame, transform.ParentFrame)
            {
                Timestamp = transform.Timestamp
            };
        }

        /// <summary>
        /// Initialize default coordinate frames
        /// </summary>
        private void InitializeDefaultFrames()
        {
            // World to robot base transform
            SetTransform(new Transform(Vector3.Zero, Quaternion.Identity, "world", "base_link"));
            
            // Robot base to camera transform
            SetTransform(new Transform(new Vector3(0, 0, 1.5f), Quaternion.Identity, "base_link", "camera_link"));
            
            // Robot base to lidar transform
            SetTransform(new Transform(new Vector3(0, 0, 1.0f), Quaternion.Identity, "base_link", "laser_link"));
        }

        /// <summary>
        /// Get all frame names
        /// </summary>
        public string[] GetFrameNames()
        {
            var frames = new HashSet<string>();
            foreach (var parentFrame in _transforms.Keys)
            {
                frames.Add(parentFrame);
                foreach (var childFrame in _transforms[parentFrame].Keys)
                {
                    frames.Add(childFrame);
                }
            }
            return frames.ToArray();
        }
    }
}