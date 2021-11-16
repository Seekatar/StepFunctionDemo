using System;
using System.Collections.Generic;

namespace CCC.CAS.Workflow2Service.Services
{
    public class WorkDemoActivityState
    {
        /// <example>f8659a63-6850-4d06-9bba-576c7084936a</example>
        public string CorrelationId { get; set; } = "";

        /// <example>f8659a63-6850-4d06-9bba-576c7084936a</example>
        public string RequestId { get; set; } = "";

        /// <example>USAA</example>
        public string ClientCode { get; set; } = "";

        /// <example>1</example>
        public int ProfileId { get; set; }

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
