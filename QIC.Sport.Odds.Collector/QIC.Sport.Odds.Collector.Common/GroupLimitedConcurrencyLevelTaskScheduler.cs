using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QIC.Sport.Odds.Collector.Common
{

    public interface IGroupFlag
    {
        string GroupID { get; set; }
    }

    public class DefaultGroupFlag : IGroupFlag
    {
        public string GroupID { get; set; }
        
    }

    /// <summary>
    /// Provides a task scheduler that ensures a maximum concurrency level while
    /// running on top of the ThreadPool.
    /// </summary>
    public class GroupLimitedConcurrencyLevelTaskScheduler : TaskScheduler
    {
        /// <summary>Whether the current thread is processing work items.</summary>
        [ThreadStatic]
        private static bool _currentThreadIsProcessingItems;
        /// <summary>The maximum concurrency level allowed by this scheduler.</summary>
        private readonly int _maxDegreeOfParallelism;
        /// <summary>Whether the scheduler is currently processing work items.</summary>
        private int _delegatesQueuedOrRunning = 0; // protected by lock(_tasks)
        /// <summary>The list of tasks to be executed.</summary>
        private Dictionary<string, List<Task>> _tasksGroup = new Dictionary<string, List<Task>>();

        /// <summary>
        /// Initializes an instance of the LimitedConcurrencyLevelTaskScheduler class with the
        /// specified degree of parallelism.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism provided by this scheduler.</param>
        public GroupLimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
        {
            if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        /// <summary>Queues a task to the scheduler.</summary>
        /// <param name="task">The task to be queued.</param>
        protected sealed override void QueueTask(Task task)
        {
            // Add the task to the list of tasks to be processed.  If there aren't enough
            // delegates currently queued or running to process tasks, schedule another.
            lock (_tasksGroup)
            {
                TasksAddOrUpdate(task);
                //_tasks.AddLast(task);
                if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
                {
                    ++_delegatesQueuedOrRunning;
                    NotifyThreadPoolOfPendingWork();
                }
            }
        }

        private void TasksAddOrUpdate(Task task)
        {
            var paramObject = task.AsyncState as IGroupFlag;
            if (paramObject == null) throw new ArgumentOutOfRangeException("taskAsyncState");
            var k = paramObject.GroupID;
            if (_tasksGroup.ContainsKey(k))
            {
                _tasksGroup[k].Add(task);
            }
            else
            {
                _tasksGroup.Add(k, new List<Task>(new Task[] { task }));
            }
        }

        private bool TasksRemove(Task task)
        {
            var paramObject = task.AsyncState as IGroupFlag;
            if (paramObject == null) throw new ArgumentOutOfRangeException("taskAsyncState");
            var k = paramObject.GroupID;
            if (_tasksGroup.ContainsKey(k))
            {
                _tasksGroup[k].Remove(task);
                if (_tasksGroup[k].Count == 0) _tasksGroup.Remove(k);
            }
            return true;
        }

        private IEnumerable<Task> TasksToArray()
        {
            List<Task> ret = new List<Task>();
            foreach (var item in _tasksGroup)
            {
                ret.AddRange(item.Value.ToArray());
            }
            return ret;
        }

        /// <summary>
        /// Informs the ThreadPool that there's work to be executed for this scheduler.
        /// </summary>
        private void NotifyThreadPoolOfPendingWork()
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                // Note that the current thread is now processing work items.
                // This is necessary to enable inlining of tasks into this thread.
                _currentThreadIsProcessingItems = true;
                try
                {
                    // Process all available items in the queue.
                    while (true)
                    {
                        List<Task> items;
                        lock (_tasksGroup)
                        {
                            // When there are no more items to be processed,
                            // note that we're done processing, and get out.
                            if (_tasksGroup.Count == 0)
                            {
                                --_delegatesQueuedOrRunning;
                                break;
                            }

                            // Get the next item from the queue
                            var kv = _tasksGroup.FirstOrDefault();
                            items = kv.Value;
                            _tasksGroup.Remove(kv.Key);
                        }

                        // Execute the task we pulled out of the queue
                        foreach (var item in items) base.TryExecuteTask(item);
                    }
                }
                // We're done processing items on the current thread
                finally { _currentThreadIsProcessingItems = false; }
            }, null);
        }

        /// <summary>Attempts to execute the specified task on the current thread.</summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="taskWasPreviouslyQueued"></param>
        /// <returns>Whether the task could be executed on the current thread.</returns>
        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // If this thread isn't already processing a task, we don't support inlining
            if (!_currentThreadIsProcessingItems) return false;

            // If the task was previously queued, remove it from the queue
            if (taskWasPreviouslyQueued) TryDequeue(task);

            // Try to run the task.
            return base.TryExecuteTask(task);
        }

        /// <summary>Attempts to remove a previously scheduled task from the scheduler.</summary>
        /// <param name="task">The task to be removed.</param>
        /// <returns>Whether the task could be found and removed.</returns>
        protected sealed override bool TryDequeue(Task task)
        {
            lock (_tasksGroup)
            {
                return TasksRemove(task);
            }
        }

        /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
        public sealed override int MaximumConcurrencyLevel { get { return _maxDegreeOfParallelism; } }

        /// <summary>Gets an enumerable of the tasks currently scheduled on this scheduler.</summary>
        /// <returns>An enumerable of the tasks currently scheduled.</returns>
        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(_tasksGroup, ref lockTaken);
                if (lockTaken) return TasksToArray();
                else throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_tasksGroup);
            }
        }

        public int TaskCount()
        {
            return GetScheduledTasks().Count();
        }
    }
}
