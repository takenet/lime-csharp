using System.Threading.Tasks;

namespace Lime.Protocol.Util
{
    public static class TaskUtil
    {
        /// <summary>
        /// Get a completed task.
        /// </summary>
        public readonly static Task CompletedTask = Task.FromResult<object>(null);
    }
}
