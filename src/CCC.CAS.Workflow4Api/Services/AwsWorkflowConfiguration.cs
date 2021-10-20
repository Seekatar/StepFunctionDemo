using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace CCC.CAS.API.Common.Storage
{
    public class AwsWorkflowConfiguration
    {
        public const string DefaultConfigName = "Workflow";

        public string ConfigName { get; set; } = DefaultConfigName;

        public static AwsWorkflowConfiguration Load(IConfiguration configuration, string configName = DefaultConfigName)
        {
            if (configuration == null) { throw new ArgumentNullException(nameof(configuration)); }

            var section = configuration.GetSection(configName);
            return section.Get<AwsWorkflowConfiguration>();
        }

        [MinLength(5)]
        public string AccessKey { get; set; } = "";
        [MinLength(5)]
        public string SecretKey { get; set; } = "";
        [MinLength(5)]
        public string Region { get; set; } = "";
        [MinLength(5)]
        public string Domain { get; set; } = "";
        [MinLength(5)]
        public string DefaultTaskList { get; set; } = "defaultTaskList";

        public bool IsEnabled => !string.IsNullOrEmpty(AccessKey) && !string.IsNullOrEmpty(SecretKey) && !string.IsNullOrEmpty(Region) && !string.IsNullOrEmpty(Domain);

        public override string ToString()
        {
            return $@"{nameof(AwsWorkflowConfiguration)}:
    {nameof(ConfigName)}:           '{ConfigName}'
    {nameof(IsEnabled)}:            '{IsEnabled}'
    {nameof(AccessKey)}:            '{AccessKey}'
    {nameof(SecretKey)}:            '{new string('*', SecretKey.Length)}'
    {nameof(Domain)}:               '{Domain}'
    {nameof(Region)}:               '{Region}'
    {nameof(DefaultTaskList)}:      '{DefaultTaskList}'
";
        }
    }
}