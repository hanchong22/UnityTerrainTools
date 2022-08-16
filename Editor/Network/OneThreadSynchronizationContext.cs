using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

public class OneThreadSynchronizationContext : SynchronizationContext
{
    public static OneThreadSynchronizationContext Instance { get; } = new OneThreadSynchronizationContext();
    // post all async actions to this, and exec its in main threading
    private readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

    private Action a;

    public void Update()
    {
        // exec the actions in main threading
        while (true)
        {
            if (!this.queue.TryDequeue(out a))
            {
                return;
            }
            a();
        }
    }

    /// <summary>
    /// recv actions, it is may call by other threadings
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="state"></param>
    public override void Post(SendOrPostCallback callback, object state)
    {
        this.queue.Enqueue(() => { callback(state); });
    }
}
