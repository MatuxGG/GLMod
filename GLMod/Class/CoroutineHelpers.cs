using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace GLMod.Class
{
    /// <summary>
    /// Bridges asynchronous <see cref="Task"/>-based work to Unity coroutines.
    ///
    /// The shared pattern is: schedule the async work on the thread pool, poll
    /// a volatile <c>done</c> flag from the main thread with <c>yield return null</c>,
    /// then invoke the appropriate callback once completed.
    /// </summary>
    public static class CoroutineHelpers
    {
        /// <summary>
        /// Runs <paramref name="asyncWork"/> on the thread pool and yields until it
        /// completes. Invokes <paramref name="onCompleted"/> with the result on success,
        /// or <paramref name="onError"/> with the thrown exception on failure.
        /// Callbacks run on the Unity main thread.
        /// </summary>
        public static IEnumerator RunAsync<T>(
            Func<Task<T>> asyncWork,
            Action<T> onCompleted = null,
            Action<Exception> onError = null)
        {
            T result = default;
            Exception error = null;
            bool done = false;

            _ = Task.Run(async () =>
            {
                try
                {
                    result = await asyncWork().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    Volatile.Write(ref done, true);
                }
            });

            while (!Volatile.Read(ref done))
                yield return null;

            if (error != null)
                onError?.Invoke(error);
            else
                onCompleted?.Invoke(result);
        }

        /// <summary>
        /// Runs <paramref name="asyncWork"/> on the thread pool and yields until it
        /// completes. Invokes <paramref name="onCompleted"/> on success or
        /// <paramref name="onError"/> with the thrown exception on failure.
        /// Callbacks run on the Unity main thread.
        /// </summary>
        public static IEnumerator RunAsync(
            Func<Task> asyncWork,
            Action onCompleted = null,
            Action<Exception> onError = null)
        {
            Exception error = null;
            bool done = false;

            _ = Task.Run(async () =>
            {
                try
                {
                    await asyncWork().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    Volatile.Write(ref done, true);
                }
            });

            while (!Volatile.Read(ref done))
                yield return null;

            if (error != null)
                onError?.Invoke(error);
            else
                onCompleted?.Invoke();
        }
    }
}
