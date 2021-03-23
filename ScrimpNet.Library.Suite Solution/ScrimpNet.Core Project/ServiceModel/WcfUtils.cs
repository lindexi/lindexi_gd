using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace ScrimpNet.ServiceModel
{
    public static class WcfUtils
    {
        public static FaultException<WcfFault> NewFault(this Exception ex)
        {
            return new FaultException<WcfFault>(new WcfFault(ex), new FaultReason(new FaultReasonText(ex.Message)));
        }

        /// <summary>
        /// Extract internal exception, if any from FaultException.
        /// </summary>
        /// <param name="ex">Hydradted FaultException&lt;&gt;</param>
        /// <returns>Internal exception, if found, or parameter <paramref name="ex"/> if not</returns>
        public static Exception Extract(Exception ex)
        {
            if (ex is FaultException<WcfFault>)
            {
                return (ex as FaultException<WcfFault>).Detail.NativeException;
            }
            if (ex is FaultException<Exception>)
            {
                return ((ex as FaultException<Exception>).Detail);
            }
            return ex;
        }
    }
}
