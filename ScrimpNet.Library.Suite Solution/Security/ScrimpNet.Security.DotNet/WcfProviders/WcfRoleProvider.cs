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
using ScrimpNet.ServiceModel;
using System.Collections.Specialized;
using ScrimpNet.Diagnostics;
using ScrimpNet.Security.Contracts;
using System.ServiceModel;
using System.Net;
using ScrimpNet.Configuration;
using System.Configuration;

namespace ScrimpNet.Security.WcfProviders
{
    /// <summary>
    /// An implementation of ASP.Net role provider connecting to a WCF host 
    /// </summary>
    public class WcfRoleProvider : RoleProvider, IRoleProvider
    {
        Log _log = Log.NewLog(typeof(WcfRoleProvider));

        NameValueCollection _config;
        IWcfSecurityService _membershipService;
        public WcfRoleProvider() { }
        private static string _authenticationKey;
        private static string _encryptionKey;
        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config)
        {
            foreach (var key in config.AllKeys) //Expand any embedded tokens in configuration string
            {
                config[key] = ConfigManager.ResolveValueSetting(config[key]);
            }
            _config = config;
            var serviceUri = _config["serviceUri"];
            if (config.AllKeys.Contains("serviceUri") == false || string.IsNullOrEmpty(config["serviceUri"]) == true)
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
            _membershipService = WcfClientFactory.Create<IWcfSecurityService>(_config["serviceUri"]);
            base.Initialize(name, config); //NOTE: RoleProvider empties this collection
        }


        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            using (_log.NewTrace())
            {
                WcfClientUtils.VerifyParameter("usernames", usernames);
                WcfClientUtils.VerifyParameter("roleNames", roleNames);
                try
                {
                    UsersAddToRolesRequest request = new UsersAddToRolesRequest();
                    request.ServiceSessionToken = _authenticationKey;
                    request.Rolenames = roleNames;
                    request.Usernames = usernames;
                    UsersAddToRolesReply response = _membershipService.RolesAddUsers(request);
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
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

        public override void CreateRole(string roleName)
        {
            WcfClientUtils.VerifyParameter("roleName", roleName);
            using (_log.NewTrace())
            {
                try
                {
                    RoleCreateRequest request = new RoleCreateRequest();
                    request.RoleName = roleName;
                    request.ServiceSessionToken = _authenticationKey;
                    var response = _membershipService.RoleCreate(request);
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            using (_log.NewTrace())
            {
                WcfClientUtils.VerifyParameter("roleName", roleName);
                try
                {
                    RoleDeleteRequest request = new RoleDeleteRequest();
                    request.RoleName = roleName;
                    request.ThrowOnPopulated = throwOnPopulatedRole;
                    request.ServiceSessionToken = _authenticationKey;
                    return _membershipService.RoleDelete(request).ResultStatus;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {

            using (_log.NewTrace())
            {
                WcfClientUtils.VerifyParameter("roleName", roleName);
                WcfClientUtils.VerifyParameter("usernameToMatch", usernameToMatch);
                try
                {
                    RoleFindUsersRequest request = new RoleFindUsersRequest();
                    request.ServiceSessionToken = _authenticationKey;
                    request.RoleName = roleName;
                    request.UsernamePattern = usernameToMatch;
                    return _membershipService.FindUsersInRole(request).Usernames;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override string[] GetAllRoles()
        {
            using (_log.NewTrace())
            {
                try
                {
                    RolesGetAllRequest request = new RolesGetAllRequest();
                    request.ServiceSessionToken = _authenticationKey;
                    return _membershipService.RolesGetAll(request).RoleNames;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override string[] GetRolesForUser(string username)
        {
            using (_log.NewTrace())
            {
                try
                {
                    WcfClientUtils.VerifyParameter("username", username);
                    RolesContainingUserRequest request = new RolesContainingUserRequest();
                    request.Username = username;
                    request.ServiceSessionToken = _authenticationKey;
                    RolesContainingUserReply response = _membershipService.RolesForUser(request);
                    return response.RoleNames;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);                                                                                   
                }
            }
        }

        public override string[] GetUsersInRole(string roleName)
        {
            using (_log.NewTrace())
            {
                try
                {
                    WcfClientUtils.VerifyParameter("roleName", roleName);
                    RoleGetUsersRequest request = new RoleGetUsersRequest();
                    request.ServiceSessionToken = _authenticationKey;
                    request.RoleName = roleName;
                    var response = _membershipService.RoleGetUsers(request);
                    return response.Usernames;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            using (_log.NewTrace())
            {
                WcfClientUtils.VerifyParameter("username", username);
                WcfClientUtils.VerifyParameter("roleName", roleName);
                try
                {
                    RoleContainsUserRequest request = new RoleContainsUserRequest();
                    request.ServiceSessionToken = _authenticationKey;
                    request.Username = username;
                    request.RoleName = roleName;
                    var response = _membershipService.RoleContainsUser(request);
                    return response.ResultStatus;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            using (_log.NewTrace())
            {
                WcfClientUtils.VerifyParameter("usernames", usernames);
                WcfClientUtils.VerifyParameter("roleNames", roleNames);

                try
                {
                    RolesRemoveUsersRequest request = new RolesRemoveUsersRequest();
                    request.ServiceSessionToken = _authenticationKey;
                    request.Usernames = usernames;
                    request.RoleNames = roleNames;
                    _membershipService.RolesRemoveUsers(request);
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }

        public override bool RoleExists(string roleName)
        {
            using (_log.NewTrace())
            {
                try
                {
                    WcfClientUtils.VerifyParameter("roleName", roleName);
                    RoleExistsRequest request = new RoleExistsRequest();
                    request.RoleName = roleName;
                    request.ServiceSessionToken = _authenticationKey;
                    return _membershipService.RoleExists(request).RoleExists;
                }
                catch (Exception ex)
                {
                    throw WcfUtils.Extract(ex);
                }
            }
        }
    }
}
