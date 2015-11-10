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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;

namespace ScrimpNet.Security.Providers.Tests_Project
{
    internal static class TestUtils
    {
        /// <summary>
        /// This class is used to represent a single user that is going to be tested
        /// </summary>
        public class UserCreateStub
        {
            public UserCreateStub(
                string userName,
                string email,
                string password,
                string question,
                string answer,
                bool isActive,
                Guid providerKey,
                string providerName
                )
            {
                Username = userName;
                EMail = email;
                Password = password;
                PasswordQuestion = question;
                PasswordAnswer = answer;
                IsActive = isActive;
                ProviderKey = providerKey;
                ProviderName = providerName;
            }

            //-------------------------------------------------------
            // header fields
            //-------------------------------------------------------
            public string Username { get; set; }
            public string EMail { get; set; }
            public string PasswordQuestion { get; set; }
            public string PasswordAnswer { get; set; }
            public string Password { get; set; }
            public bool IsActive { get; set; }
            public Guid ProviderKey { get; set; }
            public string ProviderName { get; set; }

            public void ValidateHeaderFields(MembershipUser user)
            {
                Assert.AreEqual(Username, user.UserName);
                Assert.AreEqual(EMail, user.Email);
                Assert.AreEqual(PasswordQuestion, user.PasswordQuestion);
                Assert.AreEqual(IsActive, user.IsApproved);
                Assert.AreEqual(ProviderKey, user.ProviderUserKey);
                //Assert.AreEqual(ProviderName, user.ProviderName);
            }
        }


        public static List<UserCreateStub> Users = new List<UserCreateStub>();

        internal static void CreateUserRecords(MembershipProvider provider)
        {
            Users = new List<UserCreateStub>();
            int totalRecords;
            MembershipUserCollection users = provider.GetAllUsers(0,int.MaxValue-1,out totalRecords);
            foreach (MembershipUser user in users)
            {
                if (
                    (user.UserName != null && user.UserName.ToLower().Contains("test_")) 
                 || (user.Email != null && user.Email.ToLower().Contains("test_")) 
                 || (user.PasswordQuestion != null && user.PasswordQuestion.ToLower().Contains("test_")))
                {                   
                    provider.DeleteUser(user.UserName, true);
                }
            }

            for (int x = 0; x < 50; x++)
            {
                string username = "TEST_USER:" + x.ToString("000");
                string email = "TEST_EMAIL_" + x.ToString("000")+"@test.com";
                string question = "TEST_QUESTION:" + x.ToString("000");
                string answer = "TEST_ANSWER:" + x.ToString("000");
                string password = "TEST_PASSWORD:" + x.ToString("000");
                bool isActive = (x % 2) == 0; //only even numbers are active
                Guid providerKey = Guid.NewGuid();
                MembershipCreateStatus status;
               
                var response = provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
                response.IsApproved = x!=19;
                response.Comment = "";               
                provider.UpdateUser(response);
                if (x != 19)
                {
                    provider.UnlockUser(username);  //record 19 is in 'Locked' status
                }
                Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.Success, status);
                Users.Add(new UserCreateStub(username, email, password, question, answer, response.IsApproved, providerKey, provider.Name));
            }
        }

        internal static List<string> ProviderRoles = new List<string>();
        internal static void CreateRoles(RoleProvider provider)
        {
            ProviderRoles = provider.GetAllRoles().ToList<string>().FindAll(roleName => roleName.ToLower().Contains("test_"));
            foreach (string role in ProviderRoles)
            {
                if (provider.RoleExists(role))
                {
                    string[] usersInRoles = provider.GetUsersInRole(role);
                    if (usersInRoles.Length != 0)
                    {
                        provider.RemoveUsersFromRoles(usersInRoles, new string[] { role }); //remove users from role
                    }
                    provider.DeleteRole(role, false); //remove role
                }
            }

            ProviderRoles.Clear();
            for (int x = 0; x < 20; x++)
            {
                string roleName = string.Format("TEST_ROLE:{0:000}",x);
                ProviderRoles.Add(roleName);
                provider.CreateRole(roleName);
            }

            //NOTE:  roles 0-9 are reserved for query type operations and should be considered read only
            //       roles 10-19 may be used for C-U-D operations
            addUserToRole(Users[0], ProviderRoles[0], provider);  // several users in one role
            addUserToRole(Users[2], ProviderRoles[0], provider);
            addUserToRole(Users[4], ProviderRoles[0], provider);

            addUserToRole(Users[6], ProviderRoles[1], provider); // same user in multiple roles
            addUserToRole(Users[6], ProviderRoles[2], provider);
            addUserToRole(Users[6], ProviderRoles[3], provider);

            addUserToRole(Users[10], ProviderRoles[1], provider); // same user in multiple roles
            addUserToRole(Users[10], ProviderRoles[2], provider);
            addUserToRole(Users[10], ProviderRoles[3], provider);

            addUserToRole(Users[8], ProviderRoles[1], provider);
            addUserToRole(Users[8], ProviderRoles[2], provider);

/*
TEST_ROLE:000
    Users[0]
    Users[2]
    Users[4]
TEST_ROLE:001
    Users[6]
    Users[8]
TEST_ROLE:002
    Users[6]
    Users[8]
TEST_ROLE:003
    Users[6]
TEST_ROLE:004
    -- no users
 
            */
        }

        private static void addUserToRole(UserCreateStub user, string roleName, RoleProvider provider)
        {
            provider.AddUsersToRoles(new string[] { user.Username }, new string[]{roleName});
        }
        internal static void VerifyUserHeaderFields(MembershipUser target)
        {
            if (target.UserName.StartsWith("TEST") == true)
            {
                UserCreateStub stub = Users.Find(master => string.Compare(master.Username, target.UserName) == 0);
                if (stub == null)
                {
                    Assert.Fail("Unable to find user '{0}' in list of pre configured test users", target.UserName);
                }
                stub.ValidateHeaderFields(target); //perform assertions on 'header' properties
            }
        }

        internal static void VerifyUserHeaderFields(MembershipUserCollection targetList)
        {
            foreach (MembershipUser target in targetList)
            {
                VerifyUserHeaderFields(target);                
            }
        }

        private static MembershipProvider _provider;
        internal static MembershipProvider ActiveMembershipProvider
        {
            get
            {
                if (_provider != null) return _provider;
                _provider = Membership.Provider;

                string providerName = ConfigurationManager.AppSettings["Testing.ActiveMembershipProviderName"];
                if (string.IsNullOrEmpty(providerName))
                {
                    Console.WriteLine("An explicit Membership Provider is not specified on .config key: Testing.ActiveMembershipProviderName. Using default provider: {0}",
                        _provider.Name);
                }
                else //provider specified
                {
                    _provider = Membership.Providers[providerName];
                    if (_provider == null)
                    {
                        throw new ConfigurationErrorsException(string.Format("Unable to find provider {0} specified in key: Testing.ActiveMembershipProviderName", providerName));
                    }
                }
                return _provider;
            }
        }

        private static RoleProvider _roleProvider;
        internal static RoleProvider ActiveRoleProvider
        {
            get
            {
                if (_roleProvider != null) return _roleProvider;
                _roleProvider = Roles.Provider;

                string providerName = ConfigurationManager.AppSettings["ActiveRoleProviderName"];
                if (string.IsNullOrEmpty(providerName))
                {
                    Console.WriteLine("An explicit Role Provider is not specified on .config key: ActiveRoleProviderName. Using default provider: {0}",
                        _roleProvider.Name);
                }
                else //provider specified
                {
                    _roleProvider = Roles.Providers[providerName];
                    if (_roleProvider == null)
                    {
                        throw new ConfigurationErrorsException(string.Format("Unable to find role provider {0} specified in key: ActiveRoleProviderName", providerName));
                    }
                }
                return _roleProvider;
            }
        }

        internal static MembershipUser CreateUser(UserCreateStub userCreateStub)
        {
            return new MembershipUser("scrimpNetMembershipProvider", "username", Guid.NewGuid().ToString(), "email", "question", "comment", true, false, DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now);
        }
    }
}
