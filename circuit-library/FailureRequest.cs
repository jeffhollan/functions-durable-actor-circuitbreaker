using System;

namespace Hollan.Function.CircuitLibrary   
{
    public class FailureRequest 
    {
        public string RequestId { get; set; }
        public DateTime FailureTime { get; set; }
        public string InstanceId { get; set; }
        public string ResourceId { get; set; }
    }
}