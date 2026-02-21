using System;
using System.Collections.Generic;
using FishNet;
using UnityEngine;

namespace Game.RigidbodyInterpolation
{
    public static class InterpolationUtils
    {
        // Send interval derived from the network tick rate.
        public static float SendInterval => 1f / InstanceFinder.TimeManager.TickRate;
        public static int SendRate => InstanceFinder.TimeManager.TickRate;

        // Computes the actual buffer time multiplier based on how snapshots are arriving.
        // We planned to lag behind by exactly one send interval, but in practice the gap
        // may be larger or smaller due to jitter. This calculates the multiplier that
        // accounts for that delivery variance.
        public static float DynamicAdjustment(
            float sendInterval, // interval between snapshot sends
            float jitterStandardDeviation, // how much delivery interval deviates from planned send interval on average
            float dynamicAdjustmentTolerance) // how much deviation from the send interval we tolerate
        {
            float intervalWithJitter = sendInterval + jitterStandardDeviation;
            float multiples = intervalWithJitter / sendInterval;

            float safeZone = multiples + dynamicAdjustmentTolerance;
            return safeZone;
        }

        // The buffer automatically sorts snapshots by their send time (RemoteTime).
        // SortedList is like a dictionary sorted by key.
        // We insert a new snapshot keyed by its RemoteTime — if that key already exists, we just update the data.
        // If the buffer is at capacity, new snapshots are ignored (might be worth evicting oldest instead — needs testing).
        // Returns true if a new entry was added (sorted automatically on insert).
        // Returns false if the buffer is full, or if a snapshot with this RemoteTime already existed and was overwritten.
        public static bool InsertIfNotExists<T>(
            SortedList<float, T> buffer, // snapshot buffer
            int bufferLimit, // don't grow infinitely
            T snapshot) // the newly received snapshot
            where T : IInterpolateSnapshot
        {
            if (buffer.Count >= bufferLimit) return false;

            int before = buffer.Count;
            buffer[snapshot.RemoteTime] = snapshot; // overwrites if key exists
            return buffer.Count > before;
        }

        // Inserts a new snapshot into the buffer and adjusts the local timeline accordingly.
        public static void InsertAndAdjust<T>(
            SortedList<float, T> buffer, // snapshot buffer
            SnapshotInterpolationSettings interpolationSettings,
            T snapshot, // the newly received snapshot
            ref float localTimeline, // local interpolation time based on server time
            ref float localTimescale, // timeline multiplier to apply catchup / slowdown over time
            float bufferTime, // offset for buffering
            ref ExponentalMovingAverage driftEma, // for catchup / slowdown
            ref ExponentalMovingAverage deliveryTimeEma) // for dynamic buffer time adjustment
            where T : IInterpolateSnapshot
        {
            // If the buffer is empty, hard-set local time to lag behind by exactly the buffer time
            // to synchronize timelines on the very first snapshot.
            if (buffer.Count == 0)
                localTimeline = snapshot.RemoteTime - bufferTime;

            if (!InsertIfNotExists(buffer, interpolationSettings.BufferLimit, snapshot))
                return;

            if (buffer.Count >= 2)
            {
                // When we have multiple snapshots, compute the latest delivery interval
                // and feed it into the delivery time EMA.

                // The EMA (Exponential Moving Average) gives more weight to recent values,
                // smoothing out short-term fluctuations.

                // LocalTime is only used here — its sole purpose is to track how fast
                // snapshots arrive so buffer lag can be adjusted if they come faster or slower.

                float previousLocalTime = buffer.Values[buffer.Count - 2].LocalTime;
                float lastLocalTime = buffer.Values[buffer.Count - 1].LocalTime;
                float localDeliveryTime = lastLocalTime - previousLocalTime;

                deliveryTimeEma.Add(localDeliveryTime);
            }

            // Clamp local time relative to the latest RemoteTime so it doesn't
            // run too far ahead or fall too far behind the source.
            float latestRemoteTime = snapshot.RemoteTime;
            localTimeline = TimelineClamp(localTimeline, bufferTime, latestRemoteTime);

            // Compute how far local time lags behind remote time and feed into drift EMA.
            float timeDiff = latestRemoteTime - localTimeline;
            driftEma.Add(timeDiff);

            // Compute how far the actual lag deviates from the target buffer time.
            // The goal is to keep the lag equal to bufferTime.
            // Adjust the timescale to gradually correct the drift.
            float drift = driftEma.Value - bufferTime;
            localTimescale = Timescale(drift, interpolationSettings);
        }

        // Given the buffer and current local time, finds the two surrounding snapshots
        // and computes the interpolation ratio. Removes all snapshots that are no longer needed.
        public static void StepInterpolation<T>(
            SortedList<float, T> buffer, // snapshot buffer
            float localTimeline, // local interpolation time based on server time
            out T fromSnapshot, // we interpolate 'from' this snapshot
            out T toSnapshot, // 'to' this snapshot
            out float t) // at ratio 't' [0,1]
            where T : IInterpolateSnapshot
        {
            // check this in caller:
            // nothing to do if there are no snapshots at all yet

            // Sample the buffer to find which two snapshots bracket the local time.
            Sample(buffer, localTimeline, out int from, out int to, out t);

            // save from/to
            fromSnapshot = buffer.Values[from];
            toSnapshot = buffer.Values[to];

            // remove older snapshots that we definitely don't need anymore.
            // after(!) using the indices.
            //
            // if we have 3 snapshots, and we are between 2nd and 3rd:
            //   from = 1, to = 2
            // then we need to remove the first one, which is exactly 'from'.
            // because 'from-1' = 0 would remove none.
            // remember that buffer is sorted from lowest to highest, so older snapshots are in front,
            // so when we have 3 snapshots, we interpolate between latest two, and their indices are 1 and 2,
            // so we need to remove first snapshots,
            // so count to remove snapshots is equal to index of 'from'
            buffer.RemoveRange(from);
        }

        // Finds which two snapshots in the buffer bracket the given local time
        // and computes the interpolation factor between them.
        private static void Sample<T>(
            SortedList<float, T> buffer, // snapshot buffer
            float localTimeline, // local interpolation time based on server time
            out int from, // the snapshot <= time
            out int to, // the snapshot >= time
            out float t) // interpolation factor
            where T : IInterpolateSnapshot
        {
            from = -1;
            to = -1;
            t = 0;

            // sample from [0,count-1] so we always have two at 'i' and 'i+1'.
            for (int i = 0; i < buffer.Count - 1; i++)
            {
                // is local time between these two?
                var first = buffer.Values[i];
                var second = buffer.Values[i + 1];
                if (localTimeline < first.RemoteTime || localTimeline > second.RemoteTime)
                    continue;

                // use these two snapshots
                from = i;
                to = i + 1;
                t = Mathf.InverseLerp(first.RemoteTime, second.RemoteTime, localTimeline);
                return;
            }

            // oldest snapshot ahead of local time?
            if (buffer.Values[0].RemoteTime > localTimeline)
            {
                from = to = 0;
                t = 0;
            }
            // otherwise initialize both to the last one
            else
            {
                from = to = buffer.Count - 1;
                t = 0;
            }
        }

        // Checks how far the local timeline drifts from the target (RemoteTime - bufferTime)
        // and returns a timescale multiplier.
        // If drift is within the acceptable thresholds, timescale stays at 1.0 (no correction).
        // If lagging too far behind, speed up slightly. If running too far ahead, slow down.
        private static float Timescale(float drift, SnapshotInterpolationSettings interpolationSettings)
        {
            if (drift > SendInterval * interpolationSettings.CatchupPositiveThreshold)
                return 1 + interpolationSettings.CatchupSpeed;

            if (drift < SendInterval * interpolationSettings.CatchupNegativeThreshold)
                return 1 - interpolationSettings.SlowDownSpeed;

            return 1;
        }

        // Clamps the local timeline so it can't run too far ahead or fall too far behind the target.
        // Target = latestRemoteTime - bufferTime.
        // If snapshots stop arriving, local time won't overshoot.
        // If snapshots resume with a big RemoteTime jump after a pause, local time snaps forward to catch up.
        private static float TimelineClamp(
            float localTimeline, // local interpolation time
            float bufferTime, // artificial lag for interpolation headroom
            float latestRemoteTime) // most recent RemoteTime from a snapshot
        {
            float targetTime = latestRemoteTime - bufferTime;
            float lowerBound = targetTime - bufferTime; // how far behind we can get
            float upperBound = targetTime + bufferTime; // how far ahead we can get

            return Math.Clamp(localTimeline, lowerBound, upperBound);
        }

        // Removes the first 'amount' entries from a sorted list.
        // Used to evict old snapshots that are no longer needed for interpolation.
        private static void RemoveRange<T, U>(this SortedList<T, U> list, int amount)
        {
            for (int i = 0; i < amount && i < list.Count; ++i)
                list.RemoveAt(0);
        }
    }
}
