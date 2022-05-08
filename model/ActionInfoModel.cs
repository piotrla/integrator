using System;
using System.Collections.Generic;

namespace kanc_integrator
{
    public class ActionInfoModel<T>
    {
        public List<T> ListOfElements { get; set; }
        public ReturnMessage ReturnMessage { get; set; }
    }
}