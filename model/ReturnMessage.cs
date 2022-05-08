using System;

namespace kanc_integrator
{
    public class ReturnMessage
    {
        public Guid Id { get; set; }
        public DateTime OperationDate { get; set; }
        public string OperationMessage { get; set; }
    }
}