using System.Threading.Tasks;

namespace Lime.Protocol.Util
{
    public static class TaskUtil
    {
        /// <summary>
        /// Gets a completed task.
        /// </summary>
        public static readonly Task CompletedTask = Task.FromResult<object>(null);

        /// <summary>
        /// Gets a completed task with true result.
        /// </summary>
        public static readonly Task<bool> TrueCompletedTask = Task.FromResult(true);

        /// <summary>
        /// Gets a completed task with true result.
        /// </summary>
        public static readonly Task<bool> FalseCompletedTask = Task.FromResult(false);
    }
}
