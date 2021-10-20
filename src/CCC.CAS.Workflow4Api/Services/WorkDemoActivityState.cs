using System;
using System.Collections.Generic;

namespace CCC.CAS.Workflow2Service.Services
{
    public class WorkDemoActivityState
    {
        public DateTime EventTimestamp { get; set; }
        public string ClientCode { get; set; } = "";

        public string? TaskToken { get; set; }

        public int ScenarioNumber { get; set; }

        public override string ToString()
        {
            return $"ScenarioNumber: {ScenarioNumber}: ClientCode: {ClientCode} TaskToken: {TaskToken ?? "<null>"} ";
        }
    }
}
