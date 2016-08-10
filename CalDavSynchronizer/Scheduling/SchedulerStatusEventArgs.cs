using System;

namespace CalDavSynchronizer.Scheduling
{
  public class SchedulerStatusEventArgs : EventArgs
  {
    public SchedulerStatusEventArgs (bool isRunning)
    {
      IsRunning = isRunning;
    }

    public bool IsRunning { get; }
  }
}
