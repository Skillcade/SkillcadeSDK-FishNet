using System.Collections.Generic;
using FishNet.Object;
using FishNet.Utility.Template;
using SkillcadeSDK;
using SkillcadeSDK.FishNetAdapter.Players;
using UnityEngine;
using VContainer;

namespace Game.RigidbodyInterpolation
{
    /// <summary>
    /// Wraps Rigidbody2D and provides smooth visual interpolation for physics-driven objects.
    /// Uses timeline adjustment (from TestInterpolation) to handle FixedUpdate/Update rate differences.
    /// FixedUpdate "sends" snapshots, Update advances a local timeline and interpolates.
    /// </summary>
    [DisallowMultipleComponent]
    public class NetworkRigidbody2DInterpolator : TickNetworkBehaviour
    {
        private enum State
        {
            None,
            Replaying,
            Online,
        }
        
        /// <summary>
        /// Snapshot storing a rigidbody position at a specific point in time.
        /// Implements <see cref="IInterpolateSnapshot"/> so it can be used with <see cref="InterpolationUtils"/>.
        /// </summary>
        private struct PositionSnapshot : IInterpolateSnapshot
        {
            public float RemoteTime { get; set; }
            public float LocalTime { get; set; }
            public Vector2 Position;
        }

        public Rigidbody2D Rigidbody => _rigidbody;
        public bool Ignore { get; set; }

        [Header("References")]
        [SerializeField] private NetworkObject _networkObject;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private Transform _visualTransform;

        [Header("Config")]
        [SerializeField] private bool _workOnNotOwner;
        [SerializeField] private SnapshotInterpolationSettings _interpolationSettings;

        private float BufferTime => InterpolationUtils.SendInterval * _bufferTimeMultiplier;

        [Inject] private readonly IObjectResolver _objectResolver;

        [Header("Debug")]
        [SerializeField] private State _state;
        [SerializeField] private int _isActive;
        [SerializeField] private float _timelineOffset;

        // Buffers
        private SortedList<float, TimeSnapshot> _timeBuffer;
        private SortedList<float, PositionSnapshot> _positionBuffer;

        private uint _fixedUpdateTick;
        
        // Timeline state
        private float _localTimeline;
        private float _localTimescale = 1f;
        private float _bufferTimeMultiplier;
        private ExponentalMovingAverage _driftEma;
        private ExponentalMovingAverage _deliveryTimeEma;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            InitializeBuffers();
            ApplyVisualPosition(_rigidbody.position);
        }

        /// <summary>
        /// Applies a force to the underlying Rigidbody2D.
        /// </summary>
        public void AddForce(Vector2 force, ForceMode2D mode = ForceMode2D.Force)
        {
            _rigidbody.AddForce(force, mode);
        }

        /// <summary>
        /// Applies a force at a world position to the underlying Rigidbody2D.
        /// </summary>
        public void AddForceAtPosition(Vector2 force, Vector2 position, ForceMode2D mode = ForceMode2D.Force)
        {
            _rigidbody.AddForceAtPosition(force, position, mode);
        }

        /// <summary>
        /// Teleport to a position. Skips interpolation and snaps visual immediately.
        /// Use for respawns or other instantaneous moves.
        /// </summary>
        public void Teleport(Vector2 position, bool resetVelocity = false)
        {
            if (resetVelocity)
                _rigidbody.linearVelocity = Vector2.zero;

            _rigidbody.position = position;
            ResetBuffers();
            ApplyVisualPosition(position);
        }

        protected override void TimeManager_OnPostTick()
        {
            AddSnapshot();
        }

        private void FixedUpdate()
        {
            _fixedUpdateTick++;
            if (_state == State.Replaying)
                AddSnapshot();
        }

        /// <summary>
        /// Records the current rigidbody position as a new snapshot.
        /// Inserts into both the time buffer (for timeline/timescale adjustment)
        /// and the position buffer (for visual interpolation).
        /// Should be called once per network tick from the owning script.
        /// </summary>
        public void AddSnapshot()
        {
            if (_interpolationSettings.DynamicAdjustment)
            {
                _bufferTimeMultiplier = InterpolationUtils.DynamicAdjustment(
                    InterpolationUtils.SendInterval,
                    _deliveryTimeEma.StandardDeviation,
                    _interpolationSettings.DynamicAdjustmentTolerance);
            }

            var tick = _state == State.Online ? _networkObject.TimeManager.LocalTick : _fixedUpdateTick;
            var remoteTime = _state == State.Online ? (float)_networkObject.TimeManager.TicksToTime(tick) : Time.fixedTime;
            var localTime = _state == State.Online ? _networkObject.TimeManager.ClientUptime : Time.time;

            var timeSnapshot = new TimeSnapshot(remoteTime, localTime);
            InterpolationUtils.InsertAndAdjust(
                _timeBuffer, _interpolationSettings, timeSnapshot,
                ref _localTimeline, ref _localTimescale, BufferTime,
                ref _driftEma, ref _deliveryTimeEma);

            var posSnapshot = new PositionSnapshot
            {
                RemoteTime = remoteTime,
                LocalTime = localTime,
                Position = _rigidbody.position
            };
            InterpolationUtils.InsertIfNotExists(_positionBuffer, _interpolationSettings.BufferLimit, posSnapshot);
        }

        /// <summary>
        /// Advances the local timeline by delta time scaled by the current timescale,
        /// then interpolates the visual transform between the two surrounding snapshots.
        /// </summary>
        private void Update()
        {
            _isActive = 0;
            if (_objectResolver == null)
                this.InjectToMe();

            if (_state == State.None && _objectResolver.TryResolve(out FishNetPlayersController _))
                _state = State.Online;
            
            if (_state != State.Online || (!_workOnNotOwner && !IsOwner))
            {
                _isActive = 1;
                return;
            }
            
            if (_timeBuffer.Count > 0)
            {
                _localTimeline += Time.unscaledDeltaTime * _localTimescale;
                InterpolationUtils.StepInterpolation(_timeBuffer, _localTimeline, out _, out _, out _);
            }

            if (_positionBuffer.Count == 0)
            {
                _isActive = 2;
                ApplyVisualPosition(_rigidbody.position);
                return;
            }

            if (_interpolationSettings.TeleportBufferThreshold > 0 && _positionBuffer.Count >= _interpolationSettings.TeleportBufferThreshold)
            {
                _isActive = 3;
                Teleport(_rigidbody.position);
                return;
            }

            _isActive = 4;
            InterpolationUtils.StepInterpolation(
                _positionBuffer, _localTimeline,
                out var from, out var to, out float t);

            _timelineOffset = t;
            var interpolated = Vector2.Lerp(from.Position, to.Position, t);
            ApplyVisualPosition(interpolated);
        }

        /// <summary>
        /// Allocates fresh buffers and resets all timeline state to defaults.
        /// Called on Awake and OnEnable to ensure a clean starting state.
        /// </summary>
        private void InitializeBuffers()
        {
            _timeBuffer = new SortedList<float, TimeSnapshot>();
            _positionBuffer = new SortedList<float, PositionSnapshot>();
            _bufferTimeMultiplier = _interpolationSettings.BufferTimeMultiplier;
            _localTimeline = 0;
            _localTimescale = 1f;
            _driftEma = new ExponentalMovingAverage(InterpolationUtils.SendRate * _interpolationSettings.DriftEmaDuration);
            _deliveryTimeEma = new ExponentalMovingAverage(InterpolationUtils.SendRate * _interpolationSettings.DeliveryTimeEmaDuration);
        }

        /// <summary>
        /// Clears existing buffers and resets timeline state without reallocating.
        /// Used after teleports to prevent stale snapshots from causing interpolation artifacts.
        /// </summary>
        private void ResetBuffers()
        {
            Ignore = false;
            _timeBuffer.Clear();
            _positionBuffer.Clear();
            _localTimeline = 0;
            _localTimescale = 1f;
            _bufferTimeMultiplier = _interpolationSettings.BufferTimeMultiplier;
            _driftEma = new ExponentalMovingAverage(InterpolationUtils.SendRate * _interpolationSettings.DriftEmaDuration);
            _deliveryTimeEma = new ExponentalMovingAverage(InterpolationUtils.SendRate * _interpolationSettings.DeliveryTimeEmaDuration);
        }

        /// <summary>
        /// Moves the visual transform to the given world position,
        /// preserving the cached offset and Z depth.
        /// </summary>
        private void ApplyVisualPosition(Vector2 worldPosition)
        {
            if (Ignore)
            {
                _visualTransform.localPosition = Vector3.zero;
                return;
            }
            
            _visualTransform.position = new Vector3(
                worldPosition.x,
                worldPosition.y,
                _rigidbody.transform.position.z);
        }
    }
}
