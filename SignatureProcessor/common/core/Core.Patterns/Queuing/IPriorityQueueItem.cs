using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Patterns.Queuing
{
    public interface IPriorityQueueItem
    {
        int Priority { get; set; }
    }
}
