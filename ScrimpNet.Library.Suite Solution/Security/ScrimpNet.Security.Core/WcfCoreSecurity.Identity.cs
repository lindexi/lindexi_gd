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
using ScrimpNet;

using ScrimpNet.Diagnostics;

using ScrimpNet.ServiceModel.BaseContracts;
using asp = System.Web.Security;


using System.ServiceModel;
using ScrimpNet.ServiceModel;
using ScrimpNet.Web;
using ScrimpNet.Text;
using System.Data.SqlClient;
using System.Data;
using ScrimpNet.Security.Contracts;
using ScrimpNet.Security.SqlProviders;

namespace ScrimpNet.Security.Core
{
    /// <summary>
    /// Essentially expose functionality in ASP.Net membership and role provider
    /// </summary>
    public partial class WcfCoreSecurity : IWcfSecurityService
    {
        /// <summary>
        /// verify caller is allowed to execute request
        /// </summary>
        /// <typeparam name="I"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns>response.Status = Unknown if authentication and authorization SUCCEEDs otherwise one of the error codes</returns>
        private T verifyRequest<I, T>(I request, T response)
            where I : ServiceSessionRequestBase
            where T : ServiceSessionReplyBase
        {
            response.Status = ActionStatus.Unknown;
            return response;
            //EXTENSION do robust authentication and authorization checking here
        }


        public bool verifySessionToken(string sessionToken, SecureSessionReplyBase response)
        {
            //EXTENSION verfiy session token to make sure known caller is hitting service end-point.  Use
            //  when your service is publically exposed and does not have a secure transport layer
            return true;
        }



        public MembershipSettingReply SettingsRetrieve(MembershipSettingRequest request)
        {
            var response = new MembershipSettingReply();
            try
            {
                response = verifyRequest<MembershipSettingRequest, MembershipSettingReply>(request, new MembershipSettingReply());

                response.ApplicationName = _membership.ApplicationName;
                response.EnablePasswordReset = _membership.EnablePasswordReset;
                response.EnablePasswordRetrieval = _membership.EnablePasswordRetrieval;
                response.MaxInvalidPasswordAttempts = _membership.MaxInvalidPasswordAttempts;
                response.MinRequiredNonAlphanumericCharacters = _membership.MinRequiredNonAlphanumericCharacters;
                response.MinRequiredPasswordLength = _membership.MinRequiredPasswordLength;
                response.PasswordAttemptWindow = _membership.PasswordAttemptWindow;
                response.PasswordFormat = _membership.PasswordFormat;
                response.PasswordStrengthRegularExpression = _membership.PasswordStrengthRegularExpression;
                response.RequiresQuestionAndAnswer = _membership.RequiresQuestionAndAnswer;
                response.RequiresUniqueEmail = _membership.RequiresUniqueEmail;
                response.Status = ActionStatus.OK;

                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public IdentityExistsReply IdentityExists(IdentityExistsRequest request)
        {
            IdentityExistsReply response = null;
            try
            {
                response = verifyRequest<IdentityExistsRequest, IdentityExistsReply>(request, new IdentityExistsReply(request));
                if (response.Status != ActionStatus.Unknown) return response;

                if (string.IsNullOrEmpty(request.IdentityName) == true)
                {
                    response.Status = ActionStatus.Error;
                    response.Messages.Add(ActionStatus.Error, "IdentityName must not be null or empty");
                    return response;
                }
                try
                {
                    asp.MembershipUser user = _membership.GetUser(request.IdentityName, false);
                    response.Status = ActionStatus.OK;
                    response.IdentityExists = user != null;
                    return response;
                }
                catch (Exception ex)
                {
                    response.Status = ActionStatus.Error;
                    response.IdentityExists = false;
                    response.Messages.Add(ex);
                    return response;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                response.Status = ActionStatus.InternalError;
                response.Messages.Add(ex);
                return response;
            }
        }


        private ScrimpNetSqlMembershipProvider _membership = asp.Membership.Provider as ScrimpNetSqlMembershipProvider;



        public PasswordChangeReply CredentialChangePassword(PasswordChangeRequest request)
        {
            try
            {
                PasswordChangeReply response = new PasswordChangeReply();
                response.ResultStatus = _membership.ChangePassword(request.UserName, request.OldPassword, request.NewPassword);
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public PasswordQAChangeReply CredentialChangePasswordQA(PasswordQAChangeRequest request)
        {
            try
            {
                PasswordQAChangeReply response = new PasswordQAChangeReply();
                response.ResultStatus = _membership.ChangePasswordQuestionAndAnswer(request.UserName, request.Password, request.NewQuestion, request.NewAnswer);
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public IdentityCreateReply IdentityCreate(IdentityCreateRequest request)
        {
            try
            {
                IdentityCreateReply response = new IdentityCreateReply();
                MembershipCreateStatus status = MembershipCreateStatus.ProviderError;
                MembershipUser user = request.User;
                user = _membership.CreateUser(user.UserName, request.Password, user.Email, user.PasswordQuestion, request.Answer, user.IsApproved, user.ProviderUserKey, out status);
                setStatus(response, status);
                response.User = user;
                response.CreateStatus = status;

                if (status == MembershipCreateStatus.Success && user != null)
                {
                    user.IsApproved = false;
                    user.Comment = Guid.NewGuid().ToString();
                    _membership.UpdateUser(user);
                    response.ActivationKey = user.Comment;
                }
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        private void setStatus(IdentityCreateReply response, MembershipCreateStatus status)
        {
            switch (status)
            {
                case MembershipCreateStatus.DuplicateEmail:
                    response.Messages.Add(ActionStatus.Conflict, "EMail address already exists");
                    break;
                case MembershipCreateStatus.DuplicateProviderUserKey:
                    response.Messages.Add(ActionStatus.Conflict, "Provider User Key already exists");
                    break;
                case MembershipCreateStatus.DuplicateUserName:
                    response.Messages.Add(ActionStatus.Conflict, "Username already exists");
                    break;
                case MembershipCreateStatus.InvalidAnswer:
                    response.Messages.Add(ActionStatus.Error, "Answer is missing or invalid");
                    break;
                case MembershipCreateStatus.InvalidEmail:
                    response.Messages.Add(ActionStatus.Error, "EMail is missing or invalid");
                    break;
                case MembershipCreateStatus.InvalidPassword:
                    response.Messages.Add(ActionStatus.Error, "Password is invalid. Password " + expandPasswordRules());
                    break;
                case MembershipCreateStatus.InvalidProviderUserKey:
                    response.Messages.Add(ActionStatus.Error, "Invalid provider user key");
                    break;
                case MembershipCreateStatus.InvalidQuestion:
                    response.Messages.Add(ActionStatus.Error, "Question is missing or invalid");
                    break;
                case MembershipCreateStatus.InvalidUserName:
                    response.Messages.Add(ActionStatus.Error, "Username is missing or invalid");
                    break;
                case MembershipCreateStatus.ProviderError:
                    response.Messages.Add(ActionStatus.InternalError, "Membership provider discovered some kind of error");
                    break;
                case MembershipCreateStatus.UserRejected:
                    response.Messages.Add(ActionStatus.Error, "User not created");
                    break;
            }
            response.CreateStatus = status;
            if (response.IsValid == true)
            {
                response.Status = ActionStatus.Created;
            }
            else
            {
                response.Status = response.Messages[0].Severity;
            }
        }

        private string expandPasswordRules()
        {
            string response = "";
            if (_membership.MinRequiredNonAlphanumericCharacters > 0)
            {
                response += TextUtils.StringFormat("must have {0} special symbols (e.g. '!', '#' etc.)", _membership.MinRequiredNonAlphanumericCharacters);
            }
            if (response.Length > 0)
            {
                response += TextUtils.StringFormat(" and be at least {0} characters long", _membership.MinRequiredPasswordLength);
            }
            else
            {
                response += TextUtils.StringFormat(" must be at least {0} characters long", _membership.MinRequiredPasswordLength);
            }
            return response;
        }

        public CredentialActivateReply CredentialActivate(CredentialActivateRequest request)
        {
            CredentialActivateReply response = new CredentialActivateReply();
            try
            {
                var member = _membership.GetUser(request.IdentityName, true);
                if (member == null)
                {
                    response.Status = ActionStatus.NotFound;
                    response.Messages.Add("Unable to verify user: {0}", request.IdentityName);
                    return response;
                }

                if (string.Compare(member.Comment, request.ActivationKey, false) != 0)
                {
                    response.Status = ActionStatus.Error;
                    response.Messages.Add(ActionStatus.Error, "Unable to validate authentication key.  Possibly key is incorrect or obsolete");
                    return response;
                }

                member.Comment = "";
                member.IsApproved = true;
                _membership.UpdateUser(member);

                var verifyResult = _membership.ValidateUser(request.IdentityName, request.IdentityPassword);
                if (verifyResult == false)
                {
                    member.Comment = request.ActivationKey;
                    member.IsApproved = false;
                    _membership.UpdateUser(member);
                    response.Status = ActionStatus.AuthenticationRequired;
                    response.Messages.Add(ActionStatus.AuthenticationRequired, "Username '{0}' or password not verfied", request.IdentityName);
                    return response;
                }
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                response.Messages.Add(ex, "Activation Error on user: {0} key: {1}", request.IdentityName, request.ActivationKey);
                response.Status = ActionStatus.Error;
                throw ex.NewFault();
            }
        }

        public UserDeleteReply IdentityDelete(UserDeleteRequest request)
        {
            try
            {
                UserDeleteReply response = new UserDeleteReply();
                response.ResultStatus = _membership.DeleteUser(request.UserName, request.DeletedRelatedData);
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public UsersFindByEMailReply FindUsersByEmail(UsersFindByEMailRequest request)
        {
            try
            {
                UsersFindByEMailReply response = new UsersFindByEMailReply();
                int totalRecords=0;
                response.UserList = _membership.FindUsersByEmail(request.EMailPattern, request.PageIndex, request.PageSize, out totalRecords);
                response.TotalRecords = totalRecords;
                return response;
            }
            catch (ArgumentException ex)
            {
                throw new FaultException<WcfFault>(new WcfFault(ex));
            }
        }

        public UserFindByNameReply FindUsersByName(UsersFindByNameRequest request)
        {
            try
            {
                UserFindByNameReply response = new UserFindByNameReply();
                int totalRecords=0;
                response.UserList = _membership.FindUsersByName(request.UserNamePatter, request.PageIndex, request.PageSize, out totalRecords);
                response.TotalRecords = totalRecords;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public UsersGetAllReply IdentityGetAll(UsersGetAllRequest request)
        {
            UsersGetAllReply response = new UsersGetAllReply();
            try
            {

                int totalRecords=0;
                response.UserList = _membership.GetAllUsers(request.PageIndex, request.PageSize, out totalRecords);
                response.TotalRecords = totalRecords;
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
            //catch (Exception ex)
            //{
            //    response.Status = ActionStatus.Error;
            //    response.Messages.Add(MessageSeverity.Error, 0, Utils.Expand(ex));
            //    return response;
            //}

        }

        public int GetNumberOfUsersOnline()
        {
            try
            {
                return _membership.GetNumberOfUsersOnline();
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public PasswordGetReply IdentityGetPassword(PasswordGetRequest request)
        {
            try
            {
                PasswordGetReply response = new PasswordGetReply();
                response.Password = _membership.GetPassword(request.UserName, request.Answer);
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }

        }

        public UserGetByUserNameReply IdentityGetByName(UserGetByUserNameRequest request)
        {
            try
            {
                var response = verifyRequest<UserGetByUserNameRequest, UserGetByUserNameReply>(request, new UserGetByUserNameReply(request));
                response.User = _membership.GetUser(request.UserName, request.UserIsOnLine);
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public UserGetByProviderKeyReply IdentityGetByKey(UserGetByProviderKeyRequest request)
        {
            try
            {
                UserGetByProviderKeyReply response = new UserGetByProviderKeyReply();
                response.User = _membership.GetUser(request.ProviderKey, request.IsOnLine);
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public UsernameGetByEMailReply IdentityGetByEMail(UsernameGetByEMailRequest request)
        {
            try
            {
                UsernameGetByEMailReply response = new UsernameGetByEMailReply();
                response.Username = _membership.GetUserNameByEmail(request.EMailAddress);
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public PasswordResetReply CredentialReset(PasswordResetRequest request)
        {
            try
            {
                PasswordResetReply response = new PasswordResetReply();
                response.NewPassword = _membership.ResetPassword(request.Username, request.PasswordAnswer);
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public UserUnlockReply CredentialUnlock(UserUnlockRequest request)
        {
            try
            {
                UserUnlockReply response = new UserUnlockReply();
                response.ResultStatus = _membership.UnlockUser(request.UserName);
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }


        public UserUpdateReply IdentityUpdate(UserUpdateRequest request)
        {
            try
            {
                UserUpdateReply response = new UserUpdateReply();
                _membership.UpdateUser(request.User);
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }

        }

        public UserVerifyReply CredentialVerify(UserVerifyRequest request)
        {

            UserVerifyReply response = new UserVerifyReply(request);
            try
            {
                // if (verifySessionToken(request.ServiceSessionToken, response) == false) return response;


                response.IsAuthenticated = _membership.ValidateUser(request.ChallengePrompt, request.ChallengeAnswer);
                if (response.IsAuthenticated == false)
                {
                    response.Status = ActionStatus.Error;
                    response.Messages.Add(ActionStatus.Forbidden, string.Format("Unable to validate credentials for '{0}'", request.ChallengePrompt));
                    response.Context.IdentityToken = null;
                    response.RequestorData = request.RequestorData;
                    response.ServiceSessionToken = request.ServiceSessionToken;
                    response.Context.Name = null;
                    return response;
                }
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
            //catch (Exception ex)
            //{
            //    response.Status = ActionStatus.Error;
            //    response.Messages.Add(MessageSeverity.Error, 0, Utils.Expand(ex));
            //    return response;
            //}

            //response.Context.Roles.AddRange(asp.Roles.GetRolesForUser(request.ChallengePrompt));
            response.ServiceSessionToken = request.ServiceSessionToken; //register new session
            response.Status = ActionStatus.OK;
            response.RequestorData = request.RequestorData;

            return response;
        }

        /// <summary>
        /// Change user's credentials.  Use non-null/non-empty value to indicate credential property to change
        /// </summary>
        /// <param name="request">New credentials to update</param>
        /// <returns>ActionStatus.OK and re/activation key</returns>
        public UserCredentialSaveReply UserCredentialSave(UserCredentialSaveRequest request)
        {
            UserCredentialSaveReply response = new UserCredentialSaveReply();

            try
            {
                //-------------------------------------------------------
                // verify original username and password exists
                //-------------------------------------------------------
                bool verifyUserResult = _membership.ValidateUser(request.OriginalIdentityName, request.OriginalPassword);
                if (verifyUserResult == false)
                {
                    response.Status = ActionStatus.Forbidden;
                    response.Messages.Add(ActionStatus.Forbidden, "Could not verify credentials for: '{0}'.  Account must be active and in good standing to change it's credentials", request.OriginalIdentityName);
                    return response;
                }
                response.Messages.Add("User '{0}' validated", request.OriginalIdentityName);

                //-------------------------------------------------------
                // change password
                //-------------------------------------------------------
                if (string.IsNullOrEmpty(request.NewPassword) == false)
                {
                    bool changePasswordResult = _membership.ChangePassword(request.OriginalIdentityName, request.OriginalPassword, request.NewPassword);
                    if (changePasswordResult == false)
                    {
                        response.Status = ActionStatus.Forbidden;
                        response.Messages.Add(ActionStatus.Forbidden, "Could not change password for: '{0}'.  Account must be active and in good standing to change it's credentials", request.OriginalIdentityName);
                        return response;
                    }
                    response.Messages.Add("Password updated");
                }

                //-------------------------------------------------------
                //  change password question & answer
                //-------------------------------------------------------
                bool changeQAResult = _membership.ChangePasswordQuestionAndAnswer(request.OriginalIdentityName, request.OriginalPassword, request.ChallengeQuestion, request.ChallengeAnswer);
                if (string.IsNullOrEmpty(request.ChallengeQuestion) == false && string.IsNullOrEmpty(request.ChallengeAnswer) == false)
                {
                    if (changeQAResult == false)
                    {
                        response.Status = ActionStatus.Forbidden;
                        response.Messages.Add(ActionStatus.Forbidden, "Could not change challenge question/answer for: '{0}'.  Account must be active and in good standing to change it's credentials", request.OriginalIdentityName);
                        return response;
                    }
                    response.Messages.Add("Challenge question updated");
                }
                //-------------------------------------------------------
                // verify username doesn't already exist
                //-------------------------------------------------------
                string activationKey = Guid.NewGuid().ToString();
                if (string.IsNullOrEmpty(request.NewIdentityName) == false)
                {
                    var tmpUser = _membership.GetUser(request.NewIdentityName, false);
                    if (tmpUser != null)
                    {
                        response.Status = ActionStatus.Conflict;
                        response.Messages.Add(ActionStatus.Conflict, "Requested identity '{0}' already exists.", request.OriginalIdentityName);
                        return response;
                    }

                    //-------------------------------------------------------
                    //  now update username.  NOTE: make ADO calls since
                    //  SQL membership provider does not have capability to 
                    //  change username
                    //-------------------------------------------------------                    
                    string sql = "UPDATE aspnet_Users SET username = @user1, loweredUserName = @user2 WHERE userId = @userId;UPDATE aspnet_Membership SET email=@user1, loweredEMail=@user2,IsApproved=0,Comment=@comment where USERID = @userId";
                    using (SqlConnection cn = new SqlConnection(_membership.dbConnectionString))
                    {
                        using (SqlCommand cmd = cn.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            cmd.CommandType = CommandType.Text;
                            cmd.Parameters.AddWithValue("user1", request.NewIdentityName);
                            cmd.Parameters.AddWithValue("user2", request.NewIdentityName.ToLower());
                            cmd.Parameters.AddWithValue("comment", activationKey);
                            throw new NotImplementedException("Need to set @userid");
                            cn.Open();
                            cmd.ExecuteNonQuery();
                            cn.Close();
                        }
                    }
                    response.Messages.Add("LoginName updated");
                }

                //-------------------------------------------------------
                //  all changes where successful so "deactivate" account so
                //  it can be "reactivated".  Credential changes always 
                //  force a "reactivation".
                //-------------------------------------------------------
                var user = _membership.GetUser(request.OriginalIdentityName, true);
                user.Comment = Guid.NewGuid().ToString();
                user.IsApproved = false;
                _membership.UpdateUser(user);

                response.Messages.Add("Account has been de-verified due to succssful credential change and must be reverified");
                response.ActivationKey = activationKey;
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                response.Status = ActionStatus.InternalError;
                response.Messages.Add(ex);
                return response;
            }
        }


        public CredentialDefaultGetReply CredentialDefaultsGet(CredentialDefaultGetRequest request)
        {
            CredentialDefaultGetReply response = new CredentialDefaultGetReply(request);
            var user = _membership.GetUser(request.IdentityName, false);
            response.ChallengeQuestion = user.PasswordQuestion;
            
            return response;
        }

        public UserCredentialSaveReply CredentialUpdate(UserCredentialSaveRequest request)
        {
            throw new NotImplementedException("This method is intended to specific VAR/OEM applications and is not currently implemented for the open source version");
        }
    }
}
