using System;
using System.Collections.Generic;

namespace OUT_ASHBOUND;

public sealed class OUT_EventBus
{
    private readonly Queue<OUT_Event> queue = new();

    public void Emit(OUT_Event evt)
    {
        queue.Enqueue(evt);
    }

    public void Flush(Action<OUT_Event> receiver)
    {
        int guard = 0;
        while (queue.Count > 0 && guard++ < 256)
        {
            receiver(queue.Dequeue());
        }
    }
}
