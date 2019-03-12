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
using System.ServiceModel;
using ScrimpNet.ServiceModel.BaseContracts;
using ScrimpNet.ServiceModel;
namespace ScrimpNet.Security.Contracts
{
    [ServiceContract(Namespace = ContractConfig.ContractVersion)]
    public interface IWcfSecurityService
    {
        [OperationContract, FaultContract(typeof(WcfFault))]
        UsersAddToRolesReply RolesAddUsers(UsersAddToRolesRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        RoleCreateReply RoleCreate(RoleCreateRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        RoleDeleteReply RoleDelete(RoleDeleteRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        RoleFindUsersReply FindUsersInRole(RoleFindUsersRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        RolesGetAllReply RolesGetAll(RolesGetAllRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        RoleExistsReply RoleExists(RoleExistsRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        RolesContainingUserReply RolesForUser(RolesContainingUserRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        RoleGetUsersReply RoleGetUsers(RoleGetUsersRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        RoleContainsUserReply RoleContainsUser(RoleContainsUserRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        RolesRemoveUsersReply RolesRemoveUsers(RolesRemoveUsersRequest request);
        //=========================

        [OperationContract, FaultContract(typeof(WcfFault))]
        PasswordChangeReply CredentialChangePassword(PasswordChangeRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        PasswordQAChangeReply CredentialChangePasswordQA(PasswordQAChangeRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        UserVerifyReply CredentialVerify(UserVerifyRequest request);

        [OperationContract,FaultContract(typeof(WcfFault))]
        CredentialDefaultGetReply CredentialDefaultsGet(CredentialDefaultGetRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        UserCredentialSaveReply CredentialUpdate(UserCredentialSaveRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        UserDeleteReply IdentityDelete(UserDeleteRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        UsersFindByEMailReply FindUsersByEmail(UsersFindByEMailRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        UserFindByNameReply FindUsersByName(UsersFindByNameRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        UsersGetAllReply IdentityGetAll(UsersGetAllRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        int GetNumberOfUsersOnline();

        [OperationContract, FaultContract(typeof(WcfFault))]
        PasswordGetReply IdentityGetPassword(PasswordGetRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        UserGetByProviderKeyReply IdentityGetByKey(UserGetByProviderKeyRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        UserGetByUserNameReply IdentityGetByName(UserGetByUserNameRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        UsernameGetByEMailReply IdentityGetByEMail(UsernameGetByEMailRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        PasswordResetReply CredentialReset(PasswordResetRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        MembershipSettingReply SettingsRetrieve(MembershipSettingRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        UserUnlockReply CredentialUnlock(UserUnlockRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        UserUpdateReply IdentityUpdate(UserUpdateRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        CredentialActivateReply CredentialActivate(CredentialActivateRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        IdentityCreateReply IdentityCreate(IdentityCreateRequest request);

        [OperationContract, FaultContract(typeof(WcfFault))]
        IdentityExistsReply IdentityExists(IdentityExistsRequest request);
    }
}
