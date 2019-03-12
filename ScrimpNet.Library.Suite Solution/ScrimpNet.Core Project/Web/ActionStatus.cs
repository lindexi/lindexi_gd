using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace ScrimpNet.Web
{
    /// <summary>
    /// Determines the condition of the matching request.  Check the response's
    /// ActionState for more information.  
    /// </summary>
    /// <remarks>
    /// Patterned after HTTP Response Codes: http://www.w3.org/Protocols/rfc2616/rfc2616-sec10.html
    /// </remarks>
    [DataContract,Serializable]
    public enum ActionStatus
    {
        /// <summary>
        /// Required for serialization (Value:0)
        /// </summary>
        [EnumMember]
        Unknown = 0,

        /// <summary>
        /// System generated messages that are returned, often due to outage announcements etc.
        /// Analogous to HTTP.100
        /// </summary>
        [EnumMember]
        Information = 100,

        /// <summary>
        /// Requested operation completed with no errors
        ///  this does not mean the application did not
        ///  fault due to a business rule.  Thus it is likely
        ///  you'll have a MessageStatus of OK and differing
        ///  application status.  Analogous to HTTP.200
        /// </summary>
        [EnumMember]
        OK = 200,

        /// <summary>
        /// New object was created.
        /// Analogous to HTTP.201
        /// </summary>
        [EnumMember]
        Created = 201,

        /// <summary>
        /// Asynchronous Request received and processing started.
        /// Analogous to HTTP.202
        /// </summary>
        [EnumMember]
        Accepted = 202,

        /// <summary>
        /// Object of Request is empty.  Not an error.
        /// Analogous to HTTP.204
        /// </summary>
        [EnumMember]
        NoContent = 204,

        /// <summary>
        /// The Request requires missing authenticated user some other non-specific security error occured.
        /// Analogous to HTTP.401 
        /// </summary>
        [EnumMember]
        AuthenticationRequired = 401,

        /// <summary>
        /// The request(s) identity is not allowed to access service method or data.
        /// Analogous to HTTP.403
        /// </summary>
        [EnumMember]
        Forbidden = 403,

        /// <summary>
        /// The Request did not find any data to return or update in the Request (e.g. GetById with an id that doesn't exist).  Similar to NoContent but is considered an error condition
        /// Analogous to HTTP.404
        /// </summary>
        [EnumMember]
        NotFound = 404,

        /// <summary>
        /// Application logic error (e.g. Bank Account Below Minimum, Cannot Add Duplicate User)
        /// Analogous to HTTP.406
        /// </summary>
        [EnumMember]
        Error = 406,

        /// <summary>
        /// Something in the application stack did not response as expected
        /// Analogous to HTTP.408
        /// </summary>
        [EnumMember]
        RequestTimeOut = 408,

        /// <summary>
        /// This operation would result in an unacceptable conflict (e.g. duplicate key)
        /// Analogous to HTTP.409
        /// </summary>
        [EnumMember]
        Conflict = 409,

        /// <summary>
        /// An unexpected system error occurred. Analogous to HTTP.500
        /// </summary>
        [EnumMember]
        InternalError = 500,

    }
}
