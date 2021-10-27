using System;
using System.Collections.Generic;

namespace CCC.CAS.Workflow2Service.Services
{
    public class WorkDemoActivityState
    {
        /// <example>2021-01-02T12:00</example>
        public DateTime EventTimestamp { get; set; } = DateTime.Now;

        /// <example>USAA</example>
        public string ClientCode { get; set; } = "";

        /// <example>AAAAK...iNi2</example>
        public string? TaskToken { get; set; }

        /// <example>1</example>
        public int ScenarioNumber { get; set; }

        public bool Ppo1{ get; set; }
        public bool Ppo2{ get; set; }
        public bool Ppon{ get; set; }

        public override string ToString()
        {
            return $"ScenarioNumber: {ScenarioNumber}: ClientCode: {ClientCode} TaskToken: {TaskToken ?? "<null>"} ";
        }
    }
}
