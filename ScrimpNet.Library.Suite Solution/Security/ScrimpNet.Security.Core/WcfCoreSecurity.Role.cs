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
using ScrimpNet.Security.Contracts;
using asp = System.Web.Security;
using System.ServiceModel;
using ScrimpNet.Web;
using ScrimpNet.ServiceModel;

namespace ScrimpNet.Security.Core
{
    /// <summary>
    /// Essentially expose ASP.Net role provider as a WCF service
    /// </summary>
    public partial class WcfCoreSecurity 
    {
        private asp.RoleProvider _roles = asp.Roles.Provider;

        /// <summary>
        /// Adds the specified user names to the specified roles for the configured applicationName.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public UsersAddToRolesReply RolesAddUsers(UsersAddToRolesRequest request)
        {
            try
            {
                UsersAddToRolesReply response = new UsersAddToRolesReply();
                _roles.AddUsersToRoles(request.Usernames, request.Rolenames);
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public RoleCreateReply RoleCreate(RoleCreateRequest request)
        {
            try
            {
                RoleCreateReply response = new RoleCreateReply();
                _roles.CreateRole(request.RoleName);
                response.Status  = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public RoleDeleteReply RoleDelete(RoleDeleteRequest request)
        {
            try
            {
                RoleDeleteReply response = new RoleDeleteReply();
                response.ResultStatus = _roles.DeleteRole(request.RoleName, request.ThrowOnPopulated);
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public RoleFindUsersReply FindUsersInRole(RoleFindUsersRequest request)
        {
            try
            {
                RoleFindUsersReply response = new RoleFindUsersReply();
                if (string.IsNullOrEmpty(request.UsernamePattern) == false)
                {
                    request.UsernamePattern = request.UsernamePattern.Replace("?", "_").Replace("*", "%");
                }
                response.Usernames = _roles.FindUsersInRole(request.RoleName, request.UsernamePattern);
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public RolesGetAllReply RolesGetAll(RolesGetAllRequest request)
        {
            try
            {
                RolesGetAllReply response = new RolesGetAllReply();
                response.RoleNames = _roles.GetAllRoles();
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public RolesContainingUserReply RolesForUser(RolesContainingUserRequest request)
        {
            try
            {
                RolesContainingUserReply response = new RolesContainingUserReply();
                response.RoleNames = _roles.GetRolesForUser(request.Username);
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public RoleGetUsersReply RoleGetUsers(RoleGetUsersRequest request)
        {
            try
            {
                RoleGetUsersReply response = new RoleGetUsersReply();
                response.Usernames = _roles.GetUsersInRole(request.RoleName);
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public RoleContainsUserReply RoleContainsUser(RoleContainsUserRequest request)
        {
            try
            {
                RoleContainsUserReply response = new RoleContainsUserReply();
                response.ResultStatus = _roles.IsUserInRole(request.Username, request.RoleName);
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public RolesRemoveUsersReply RolesRemoveUsers(RolesRemoveUsersRequest request)
        {
            try
            {
                RolesRemoveUsersReply response = new RolesRemoveUsersReply();
                _roles.RemoveUsersFromRoles(request.Usernames, request.RoleNames);
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }

        public RoleExistsReply RoleExists(RoleExistsRequest request)
        {
            try
            {
                RoleExistsReply response = new RoleExistsReply();
                response.RoleExists = _roles.RoleExists(request.RoleName);
                response.Status = ActionStatus.OK;
                return response;
            }
            catch (Exception ex)
            {
                throw ex.NewFault();
            }
        }
    }
}
