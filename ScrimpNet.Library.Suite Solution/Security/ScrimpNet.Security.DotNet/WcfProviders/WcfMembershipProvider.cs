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
using System.Web.Security;
using ScrimpNet.Security.Contracts;
using System.Collections.Specialized;
using ScrimpNet.ServiceModel;
using ScrimpNet.Diagnostics;

using ScrimpNet.ServiceModel.BaseContracts;
using ScrimpNet;
//using ScrimpNet.Security.Client.Library;
using System.Configuration;
using ScrimpNet.Configuration;

using System.ServiceModel;
using ScrimpNet.Web;


namespace ScrimpNet.Security.WcfProviders
{
  /// <summary>
  /// An implementation of ASP.Net membership provider connecting to a WCF host 
  /// </summary>
    public class WcfMembershipProvider : MembershipProvider, IDisposable, IMembershipProvider
    {
        Log _log = Log.NewLog(typeof(WcfMembershipProvider));

        private string _userSecurityToken;

        NameValueCollection _config;
        IWcfSecurityService _membershipService;
        
        public WcfMembershipProvider(string serviceUri)
        {
            Guard.NotNullOrEmpty(serviceUri);
            Uri uri;

            if (Uri.TryCreate(serviceUri, UriKind.RelativeOrAbsolute, out uri) == false)
            {
                ExceptionFactory.Throw<ArgumentException>("Unable to convert '{0}' to valid URI", serviceUri);
            }
            createService(uri.OriginalString);
        }

        private void createService(string serviceUri)
        {
            _membershipService = WcfClientFactory.Create<IWcfSecurityService>(serviceUri);
        }

        private  static MembershipSettingReply _settings = null;
        private  MembershipSettingReply settings
        {
            get
            {

                if (_settings != null) return _settings;

                using (_log.NewTrace())
                {
                    try
                    {
                        MembershipSettingRequest request = new MembershipSettingRequest();
                        request.ServiceSessionToken = WcfClientUtils.SessionToken;
                        _settings = _membershipService.SettingsRetrieve(request);
                        return _settings;
                    }
                    catch (Exception ex)
                    {
                        throw WcfUtils.Extract(ex);
                    }
                }
            }
        }

        public static string _authenticationKey = string.Empty;
        public static string _encryptionKey = string.Empty;

        public override void Initialize(string name, NameValueCollection config)
        {
            foreach (var key in config.AllKeys)
            {
                config[key] = ConfigManager.ResolveValueSetting(config[key]);
            }
            _config = config;            
            var svcUri = _config["serviceUri"];
            
            if (config.AllKeys.Contains("serviceUri") == false || string.IsNullOrEmpty(config["serviceUri"])==true)
            {
                ExceptionFactory.Throw<ConfigurationErrorsException>("Required attribute 'serviceUri' missing from provider configuration. Value may contain ScrimpNet configuration tokens: {app}, {env}, {user}, {machine}, {%app:<key>%}, and {%connection:<key>%}");
            }
            if (string.IsNullOrEmpty(_authenticationKey) == true && config.AllKeys.Contains("authenticationKey"))
            {
                _authenticationKey = config["authenticationKey"];
            }
            if (string.IsNullOrEmpty(_encryptionKey) == true && config.AllKeys.Contains("encryptionKey"))
            {
                _encryptionKey = config["encryptionKey"];
            }
            createService(svcUri);
            
            base.Initialize(name, config);
        }

        public WcfMembershipProvider(string url, string token)
        {

            url = ConfigManager.ResolveValueSetting(url);
            _membershipService = WcfClientFactory.Create<IWcfSecurityService>(url);
        }
        public WcfMembershipProvider()
        {

        }
        public override string ApplicationName
        {
            get
            {
                return WcfClientUtils.ApplicationKey;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            using (_log.NewTrace())
            {
                WcfClientUtils.VerifyParameter("username", username);
                WcfClientUtils.VerifyParameter("oldPassword", oldPassword);
                WcfClientUtils.VerifyParameter("newPassword", newPassword);

                try
                {
                    PasswordChangeRequest request = new PasswordChangeRequest();
                    request.NewPassword = newPassword;
                    request.OldPassword = oldPassword;
                    request.UserName = username;
                    PasswordChangeReply response = _membershipService.CredentialChangePassword(request);
                    return (response.Status == ActionStatus.OK && response.ResultStatus == true);
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            WcfClientUtils.VerifyParameter("username", username);
            WcfClientUtils.VerifyParameter("password", password);
            WcfClientUtils.VerifyParameter("newPasswordQuestion", newPasswordQuestion);
            WcfClientUtils.VerifyParameter("newPasswordAnswer", newPasswordAnswer);

            using (_log.NewTrace())
            {
                try
                {
                    PasswordQAChangeRequest request = new PasswordQAChangeRequest();
                    request.NewAnswer = newPasswordAnswer;
                    request.NewQuestion = newPasswordQuestion;
                    request.Password = password;
                    request.UserName = username;
                    request.ServiceSessionToken = WcfClientUtils.SessionToken;
                    PasswordQAChangeReply response = _membershipService.CredentialChangePasswordQA(request);
                    return (response.Status == ActionStatus.OK && response.ResultStatus == true);
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            using (_log.NewTrace())
            {
                //WcfClientUtils.VerifyParameter("username", username);
                //WcfClientUtils.VerifyParameter("password", password);
                //WcfClientUtils.VerifyParameter("email", email);
                //WcfClientUtils.VerifyParameter("passwordQuestion", passwordQuestion);
                //WcfClientUtils.VerifyParameter("passwordAnswer", passwordAnswer);
                try
                {
                    IdentityCreateRequest request = new IdentityCreateRequest();
                    request.ServiceSessionToken = WcfClientUtils.SessionToken;
                    request.Answer = passwordAnswer;
                    request.User = new MembershipUser("scrimpNetMembershipProvider", username, providerUserKey, email, passwordQuestion, "", isApproved, false, DateTime.UtcNow, Utils.Date.SqlMinDate, Utils.Date.SqlMinDate, DateTime.UtcNow, Utils.Date.SqlMinDate);
                    request.Password = password;
                    IdentityCreateReply response = _membershipService.IdentityCreate(request);
                    status = response.CreateStatus;
                    return response.User;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            using (_log.NewTrace())
            {
                WcfClientUtils.VerifyParameter("username", username);
                try
                {
                    UserDeleteRequest request = new UserDeleteRequest();
                    request.DeletedRelatedData = deleteAllRelatedData;
                    request.UserName = username;
                    request.ServiceSessionToken = WcfClientUtils.SessionToken;
                    UserDeleteReply response = _membershipService.IdentityDelete(request);
                    return (response.ResultStatus == true && response.Status  == ActionStatus.OK);
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override bool EnablePasswordReset
        {
            get 
            {
                return settings.EnablePasswordReset;
            }
        }

        public override bool EnablePasswordRetrieval
        {
            get { return settings.EnablePasswordRetrieval; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="emailToMatch"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalRecords"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="emailToMatch"/> is null</exception>
        /// <exception cref="ArgumentException">Throw if <paramref name="pageIndex"/> or <paramref name="pageSize"/> is &lt; 0</exception>
        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            using (_log.NewTrace())
            {
                WcfClientUtils.VerifyParameter("emailToMatch", emailToMatch);

                try
                {
                    UsersFindByEMailRequest request = new UsersFindByEMailRequest();
                    request.EMailPattern = emailToMatch;
                    request.PageIndex = pageIndex;
                    request.PageSize = pageSize;
                    request.ServiceSessionToken = WcfClientUtils.SessionToken;
                    UsersFindByEMailReply response = _membershipService.FindUsersByEmail(request);
                    totalRecords = response.TotalRecords;
                    return response.UserList;
                }
                catch (FaultException<WcfFault> ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }          
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            using (_log.NewTrace())
            {
                WcfClientUtils.VerifyParameter("usernameToMatch", usernameToMatch);
                
                try
                {
                    UsersFindByNameRequest request = new UsersFindByNameRequest();
                    request.PageIndex = pageIndex;
                    request.PageSize = pageSize;
                    request.ServiceSessionToken = WcfClientUtils.SessionToken;
                    request.UserNamePatter = usernameToMatch;
                    UserFindByNameReply response = _membershipService.FindUsersByName(request);
                    totalRecords = response.TotalRecords;
                    return response.UserList;
                }
                catch (FaultException<WcfFault> fault)
                {
                    throw WcfUtils.Extract(fault);
                }
            }
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            using (_log.NewTrace())
            {
                try
                {
                    UsersGetAllRequest request = new UsersGetAllRequest();
                    request.ServiceSessionToken = WcfClientUtils.SessionToken;
                    request.PageIndex = pageIndex;
                    request.PageSize = pageSize;

                    UsersGetAllReply response = _membershipService.IdentityGetAll(request);
                    totalRecords = response.TotalRecords;
                    return response.UserList;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override int GetNumberOfUsersOnline()
        {
            using (_log.NewTrace())
            {
                try
                {
                    return _membershipService.GetNumberOfUsersOnline();
                }
                catch(Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override string GetPassword(string username, string answer)
        {
            using (_log.NewTrace())
            {
                WcfClientUtils.VerifyParameter("username", username);
                WcfClientUtils.VerifyParameter("answer", answer);
                try
                {
                    PasswordGetRequest request = new PasswordGetRequest();
                    request.ServiceSessionToken = WcfClientUtils.SessionToken;
                    request.UserName = username;
                    request.Answer = answer;
                    PasswordGetReply response = _membershipService.IdentityGetPassword(request);
                    return response.Password;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            using (_log.NewTrace())
            {
                //WcfClientUtils.VerifyParameter("username", username);  //provider should return null if username is null or empty
                try
                {
                    UserGetByUserNameRequest request = new UserGetByUserNameRequest();
                    request.ServiceSessionToken = WcfClientUtils.SessionToken;
                    request.UserName = username;
                    request.UserIsOnLine = userIsOnline;
                    var response = _membershipService.IdentityGetByName(request);
                    return response.User;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }

        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            using (_log.NewTrace())
            {
                UserGetByProviderKeyRequest request = new UserGetByProviderKeyRequest();
                try
                {
                    request.ServiceSessionToken = WcfClientUtils.SessionToken;
                    request.ProviderKey = new Guid(providerUserKey.ToString());
                    request.IsOnLine = userIsOnline;
                    var response = _membershipService.IdentityGetByKey(request);
                    return response.User;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override string GetUserNameByEmail(string email)
        {
            using (_log.NewTrace())
            {
               // WcfClientUtils.VerifyParameter("email", email);  // providers do not validate email
                try
                {
                    UsernameGetByEMailRequest request = new UsernameGetByEMailRequest();
                    request.EMailAddress = email;
                    request.ServiceSessionToken = WcfClientUtils.SessionToken;
                    var response = _membershipService.IdentityGetByEMail(request);
                    return response.Username;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { return settings.MaxInvalidPasswordAttempts; }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return settings.MinRequiredNonAlphanumericCharacters;  }
        }

        public override int MinRequiredPasswordLength
        {
            get { return settings.MinRequiredPasswordLength; }
        }

        public override int PasswordAttemptWindow
        {
            get { return settings.PasswordAttemptWindow; }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { return settings.PasswordFormat; }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { return settings.PasswordStrengthRegularExpression; }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { return settings.RequiresQuestionAndAnswer; }
        }

        public override bool RequiresUniqueEmail
        {
            get { return settings.RequiresUniqueEmail; }
        }

        public override string ResetPassword(string username, string answer)
        {
            using (_log.NewTrace())
            {
                //WcfClientUtils.VerifyParameter("username", username);
                //WcfClientUtils.VerifyParameter("answer", answer);
                try
                {
                    PasswordResetRequest request = new PasswordResetRequest();
                    request.PasswordAnswer = answer;
                    request.ServiceSessionToken = WcfClientUtils.SessionToken;
                    request.Username = username;
                    var response = _membershipService.CredentialReset(request);
                    return response.NewPassword;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override bool UnlockUser(string userName)
        {
            using (_log.NewTrace())
            {
               // WcfClientUtils.VerifyParameter("userName", userName);
                try
                {
                    var request = new UserUnlockRequest();
                    request.ServiceSessionToken = WcfClientUtils.SessionToken;
                    request.UserName = userName;
                    var response = _membershipService.CredentialUnlock(request);
                    return response.ResultStatus;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override void UpdateUser(MembershipUser user)
        {
            using (_log.NewTrace())
            {
                try
                {
                    var request = new UserUpdateRequest();
                    request.ServiceSessionToken = WcfClientUtils.SessionToken;
                    request.User = user;
                    var response = _membershipService.IdentityUpdate(request);
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public bool UserActivate(string username, string password, string activationKey)
        {
            using (_log.NewTrace())
            {
                WcfClientUtils.VerifyParameter("username",username);
                WcfClientUtils.VerifyParameter("password",password);
                WcfClientUtils.VerifyParameter("activationkey",activationKey);
                try
                {
                    CredentialActivateRequest request = new CredentialActivateRequest();
                    request.ServiceSessionToken = WcfClientUtils.SessionToken;
                    request.IdentityPassword = password;
                    request.IdentityName = username;
                    request.ActivationKey = activationKey;
                    return _membershipService.CredentialActivate(request).IsActivated;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }



        public override bool ValidateUser(string username, string password)
        {
            using (_log.NewTrace())
            {
               
                //WcfClientUtils.VerifyParameter("username",username);  //validate user should not throw an error,simply return false
                //WcfClientUtils.VerifyParameter("password", password);

                try
                {
                    UserVerifyRequest request = new UserVerifyRequest();
                    request.ServiceSessionToken = WcfClientUtils.SessionToken;;
                    request.ChallengePrompt = username;
                    request.ChallengeAnswer = password;

                    UserVerifyReply response = _membershipService.CredentialVerify(request);
                    if (response.Context.IsAuthenticated == true)
                    {
                        _userSecurityToken = response.Context.IdentityToken;                        
                        return true;
                    }

                    _log.Warning("User '{0}' is not validated with status '{1}'. {2}",
                        username, response.Status,response.Messages.ToString());
                    _userSecurityToken = null;
                    return false;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        /// <summary>
        /// Close and dispose WCF connection
        /// </summary>
        /// <param name="isDisposing">True if being explicitly called</param>
        private void dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                WcfClientFactory.CloseAndDispose((System.ServiceModel.IClientChannel)_membershipService);
            }
        }

        /// <summary>
        /// Release reference to service
        /// </summary>
        public void Dispose()
        {
            dispose(true);
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~WcfMembershipProvider()
        {
            dispose(false);
        }

  

    }

}
