using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Data.SqlClient;

namespace ScrimpNet.ServiceModel
{

    [DataContract]
    public class WcfFault
    {
        /// <summary>
        /// Create a native .Net exception wrapped in a WcfFault
        /// </summary>
        /// <param name="ex">Serializable hydrated .Net exception</param>
        /// <returns>Hydrated exception</returns>
        public static FaultException<WcfFault> Create(Exception ex)
        {
            return new FaultException<WcfFault>(new WcfFault(ex), new FaultReason(new FaultReasonText(ex.Message)));
        }

        /// <summary>
        /// Throws a WcfFault that has been wrapped around a native .Net exception
        /// </summary>
        /// <param name="ex">Serializable hydrated .Net exception</param>
        public static void Throw(Exception ex)
        {
            var wcfEx = Create(ex);
            throw wcfEx;
        }

        [DataMember]
        public WcfFault InnerFault { get; set; }

        [DataMember]
        public string ExceptionTypeName { get; set; }

        [DataMember]
        public string Reason { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string Code { get; set; }

        /// <summary>
        /// Throw an exception if there is a .Net exception embedded
        /// </summary>
        public void Throw()
        {
            if (NativeException != null)
            {
                throw NativeException;
            }

        }

        public WcfFault(Exception ex, string message, params object[] args)
            : this(ex)
        {
            Message = string.Format(message, args);
        }

        public WcfFault(Exception ex)
        {
            Code = ex.GetType().FullName;
            Reason = ex.ToString();
            Message = ex.Message;
            ExceptionTypeName = ex.GetType().AssemblyQualifiedName;
            if (ex.InnerException != null)
            {
                InnerFault = new WcfFault(ex.InnerException, ex.Message);
            }
        }

        private Exception _detail = null;
        /// <summary>
        /// Rehydrate .Net exception(s) from meta data bound in this class
        /// </summary>
        public Exception NativeException
        {
            get
            {
                if (_detail != null) return _detail;
                string[] args = new string[] { Message };
                if (ExceptionTypeName != null)
                {
                    try
                    {
                        _detail = (Exception)Activator.CreateInstance(Type.GetType(ExceptionTypeName), args);
                    }
                    catch (MissingMethodException) //type doesn't have default constructor (e.g. SqlException)
                    {
                        _detail = (Exception)Activator.CreateInstance(typeof(ApplicationException), args);
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        string s = ex.Message;
#endif //DEBUG
                        throw;
                    }
                }
                return _detail;
            }
            set
            {
                _detail = null;
            }
        }

        public override string ToString()
        {
            string retVal = "";
            retVal = Message;
            return retVal;
        }

    }
}
