using System;
using System.Threading;

namespace CCC.CAS.Workflow2Service.Services
{
    public static class WorkDemo
    {
        public static void WasteTime(
            int workItemId, WorkDemoActivityState workDemoActivityState)
        {
            if (workDemoActivityState == null) throw new ArgumentNullException(nameof(workDemoActivityState));
            if (workItemId == 0) throw new ArgumentNullException(nameof(workDemoActivityState));

            Thread.Sleep(1000);
        }
    }
}
