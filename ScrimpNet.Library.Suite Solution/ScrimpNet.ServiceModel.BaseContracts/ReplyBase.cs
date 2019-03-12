/**
/// ScrimpNet.Core Library
/// Copyright © 2005-2011
///
/// This module is Copyright © 2005-2011 Steve Powell
/// All rights reserved.
///
/// This library is free software; you can redistribute it and/or
/// modify it under the terms of the Microsoft Public License (Ms-PL)
/// 
/// This library is distributed in the hope that it will be
/// useful, but WITHOUT ANY WARRANTY; without even the implied
/// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
/// PURPOSE.  See theMicrosoft Public License (Ms-PL) License for more
/// details.
///
/// You should have received a copy of the Microsoft Public License (Ms-PL)
/// License along with this library; if not you may 
/// find it here: http://www.opensource.org/licenses/ms-pl.html
///
/// Steve Powell, spowell@scrimpnet.com
**/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel;
using ScrimpNet.Web;

namespace ScrimpNet.ServiceModel.BaseContracts
{
    /// <summary>
    /// Common fields returned from all calls to the service end points
    /// </summary>
    [DataContract(Namespace=BaseUtils.ContractVersion)]
    public class ReplyBase:ActionReply
    {
        /// <summary>
        /// Default contructor
        /// </summary>
        public ReplyBase():base()
        {

        }

        public ReplyBase(Exception ex)
        {

        }
        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="request">Hydrated Request that contains fields some of which will be copied into response</param>
        public ReplyBase(RequestBase request)
        {
            this.RequestorData = request.RequestorData;
        }


        private Guid _responseId = Guid.NewGuid();
        /// <summary>
        /// Unique identifier of this request/response pair.  Used
        /// for logging and trouble shooting.  Each Request is assigned
        /// an identifier which can be used by technical support to 
        /// trace historical behavior
        /// </summary>
        [DataMember]
        public Guid ResponseId
        {
            get
            {
                return _responseId;
            }

            set
            {
                _responseId = value;
            }
        }

        /// <summary>
        /// Data that was supplied by caller in request.  Might
        /// be used by caller for correlation and asynchrounous 
        /// calls
        /// </summary>
        [DataMember]
        public string RequestorData { get; set; }

    }
    public class ReplyBase<T> : ActionReply<T> where T : class
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ReplyBase()
            : base()
        {

        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="result"></param>
        public ReplyBase(ActionReply<T> result)
            : base(result)
        {

        }

        /// <summary>
        /// Create new reply with ActionStatus.InternalError
        /// </summary>
        /// <param name="ex"></param>
        public ReplyBase(Exception ex)
            : base(ex)
        {
        }
    }
}
