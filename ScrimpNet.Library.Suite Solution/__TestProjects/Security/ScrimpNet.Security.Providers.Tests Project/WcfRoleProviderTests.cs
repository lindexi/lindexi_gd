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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Web.Security;
using System.Configuration;
using ScrimpNet.ServiceModel;

using ScrimpNet.Security.Providers.Tests_Project;
using System.Configuration.Provider;

namespace ScrimpNet.Security.AspNet.Providers.Tests_Project
{
    [TestClass]
    public class WcfRoleProviderTests
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }
        private static RoleProvider _provider;
        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void ClassInitialize(TestContext testContext)
        {
            _provider = TestUtils.ActiveRoleProvider;
            //TestUtils.CreateUserRecords(_provider);  //setup provider with known records of known quantity and quality
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestUtils.CreateUserRecords(TestUtils.ActiveMembershipProvider);
            TestUtils.CreateRoles(TestUtils.ActiveRoleProvider);
        }
        #endregion

        [TestMethod]
        public void MembershipRole_AddUsersToRolesTests()
        {
            string[] users = new string[] { TestUtils.Users[16].Username };
            string[] roles = new string[] { };
            //-------------------------------------------------------
            // empty role list
            //-------------------------------------------------------
            try
            {
                _provider.AddUsersToRoles(users, roles);
                Assert.Fail("Provider did not throw expected ArgumentException on empty roleName list");
            }
            catch (ArgumentException)
            {
               //ignore expected exception
            }

            //-------------------------------------------------------
            // empty user list
            //-------------------------------------------------------
            users = new string[] { };
            roles = new string[] { TestUtils.ProviderRoles[0] };
            try
            {
                _provider.AddUsersToRoles(users, roles);
                Assert.Fail("Provider did not throw expected ArgumentException on empty users list");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // null user list
            //-------------------------------------------------------
            users = null;
            roles = new string[] { TestUtils.ProviderRoles[0] };
            try
            {
                _provider.AddUsersToRoles(users, roles);
                Assert.Fail("Provider did not throw expected ArgumentException on null user list");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // null role list
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[14].Username };
            roles = null;
            try
            {
                _provider.AddUsersToRoles(users, roles);
                Assert.Fail("Provider did not throw expected ArgumentException on null role list");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // add existing user to exiting role
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[20].Username };
            roles = new string[] { TestUtils.ProviderRoles[10] };
            _provider.AddUsersToRoles(users, roles);

            //-------------------------------------------------------
            // add existing users to existing roles
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[24].Username, TestUtils.Users[26].Username };
            roles = new string[] { TestUtils.ProviderRoles[10], TestUtils.ProviderRoles[11] };
            _provider.AddUsersToRoles(users, roles);

            //-------------------------------------------------------
            // add existing users to existing roles except one role is null
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[20].Username, TestUtils.Users[22].Username };
            roles = new string[] { TestUtils.ProviderRoles[10], null };
            try
            {
                _provider.AddUsersToRoles(users, roles);
                Assert.Fail("Provider did not throw expected ArgumentNullException when one role is null in the list");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // add existing users to existing roles except one role is empty
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[20].Username, TestUtils.Users[22].Username };
            roles = new string[] { TestUtils.ProviderRoles[10], string.Empty };
            try
            {
                _provider.AddUsersToRoles(users, roles);
                Assert.Fail("Provider did not throw expected ArgumentException when one role is null in the list");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // add existing users to existing roles except one role is non-existant
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[20].Username, TestUtils.Users[22].Username };
            roles = new string[] { TestUtils.ProviderRoles[10], "TEST_" + Guid.NewGuid().ToString() };
            try
            {
                _provider.AddUsersToRoles(users, roles);
                Assert.Fail("Provider did not throw expected ProviderException when one role did not exist");
            }
            catch (ProviderException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // add existing users (except one user is null) to existing roles
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[20].Username, null };
            roles = new string[] { TestUtils.ProviderRoles[10], TestUtils.ProviderRoles[11] };
            try
            {
                _provider.AddUsersToRoles(users, roles);
                Assert.Fail("Provider did not throw expected ArgumentNull expception when one user is null");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // add existing users (except one user is empty) to existing roles
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[20].Username, string.Empty };
            roles = new string[] { TestUtils.ProviderRoles[10], TestUtils.ProviderRoles[11] };
            try
            {
                _provider.AddUsersToRoles(users, roles);
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }
            //-------------------------------------------------------
            // add existing users to existing roles except one user
            //  doesn't exist
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[20].Username, "TEST_" + Guid.NewGuid().ToString() };
            roles = new string[] { TestUtils.ProviderRoles[10], TestUtils.ProviderRoles[11] };
            try
            {
                _provider.AddUsersToRoles(users, roles);
            }
            catch (ProviderException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // add existing user to existing roles except user is already
            //  member of that role
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[30].Username, TestUtils.Users[31].Username };
            roles = new string[] { TestUtils.ProviderRoles[18], TestUtils.ProviderRoles[19] };
            try
            {
                _provider.AddUsersToRoles(users, roles);
                _provider.AddUsersToRoles(users, roles);

            }
            catch (ProviderException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // duplicate rolenames in list
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[30].Username, TestUtils.Users[31].Username };
            roles = new string[] { TestUtils.ProviderRoles[18], TestUtils.ProviderRoles[18] };
            try
            {
                _provider.AddUsersToRoles(users, roles);
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // duplicate users in list
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[30].Username, TestUtils.Users[30].Username };
            roles = new string[] { TestUtils.ProviderRoles[18], TestUtils.ProviderRoles[19] };
            try
            {
                _provider.AddUsersToRoles(users, roles);
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

        }

        [TestMethod]
        public void MembershipRole_CreateRoleTests()
        {
            string rolename;
            //-------------------------------------------------------
            //  valid name
            //-------------------------------------------------------
            rolename = "TEST_NEWROLE";
            _provider.CreateRole(rolename);

            //-------------------------------------------------------
            //  null name
            //-------------------------------------------------------
            rolename = null;
            try
            {
                _provider.CreateRole(rolename);
                Assert.Fail("Provider did not throw expected ArgumentNullException on null role name");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            //  empty name
            //-------------------------------------------------------
            rolename = string.Empty;
            try
            {
                _provider.CreateRole(rolename);
                Assert.Fail("Provider did not throw expected ArgumentException on empty role name");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            //  existing name - expect duplicate error
            //-------------------------------------------------------
            rolename = TestUtils.ProviderRoles[0];
            try
            {
                _provider.CreateRole(rolename);
            }
            catch (ProviderException)
            {
               //ignore expected exception
            }
 
        }

        [TestMethod]
        public void MembershipRole_DeleteRoleTests()
        {
            bool throwError = false;
            string rolename;
            //-------------------------------------------------------
            //  null name
            //-------------------------------------------------------
            rolename = null;
            try
            {
                _provider.DeleteRole(rolename, throwError);
                Assert.Fail("Provider did not throw expected ArgumentNullException on null rolename");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }
            //-------------------------------------------------------
            //  empty name
            //-------------------------------------------------------
            rolename = string.Empty;
            try
            {
                _provider.DeleteRole(rolename, throwError);
                Assert.Fail("Provider did not thorw expected ArgumentException on empty role name");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }
            //-------------------------------------------------------
            //  non existant name - asp doesn't thorw an error
            //    on non existant name.  It probably should but
            //    the argument can be made either way.
            //-------------------------------------------------------
            rolename = Guid.NewGuid().ToString();
            try
            {
                _provider.DeleteRole(rolename, throwError);
            }
            catch (Exception ex)
            {
                Assert.Fail("Provider threw unexpected exception '{0}' when none was expected", ex.Message);
            }

            //-------------------------------------------------------
            //  existing role with users, throw error = true
            //-------------------------------------------------------
            rolename = TestUtils.ProviderRoles[3];
            throwError = true;
            try
            {
                _provider.DeleteRole(rolename, throwError);
                Assert.Fail("Provider did not throw expected ProviderException when deleting populated role");
            }
            catch (ProviderException)
            {
                //ignore expected exception
            }
            //-------------------------------------------------------
            //  existing role without users, throw error = true
            //-------------------------------------------------------
            rolename = TestUtils.ProviderRoles[4];
            throwError = true;
            _provider.DeleteRole(rolename, throwError);

            //-------------------------------------------------------
            //  existing role with users, throw error = false
            //-------------------------------------------------------
            rolename = TestUtils.ProviderRoles[3];
            throwError = false;
            _provider.DeleteRole(rolename, throwError);
            Assert.IsFalse(_provider.RoleExists(rolename));

            //-------------------------------------------------------
            //  existing role without users, throw error = false
            //-------------------------------------------------------
            rolename = TestUtils.ProviderRoles[13];
            throwError = false;
            _provider.DeleteRole(rolename, throwError);
        }

        [TestMethod]
        public void MembershipRole_FindUsersInRoleTests()
        {
            string userMask;
            string roleName;
            string[] userNames;
            //-------------------------------------------------------
            // single user mask in existing role - success
            //-------------------------------------------------------
            userMask = TestUtils.Users[0].Username;
            roleName = TestUtils.ProviderRoles[0];
            userNames = _provider.FindUsersInRole(roleName, userMask);
            Assert.AreEqual<int>(1, userNames.Length);
            Assert.AreEqual<string>(userNames[0], userMask);

            //-------------------------------------------------------
            // ? wild card user mask in existing role - success
            //-------------------------------------------------------
            userMask = "TEST_USER:00?";
            roleName = TestUtils.ProviderRoles[0];
            userNames = _provider.FindUsersInRole(roleName, userMask);
            Assert.AreEqual<int>(3, userNames.Length);
            Assert.AreEqual<string>(userNames[0], TestUtils.Users[0].Username);
            Assert.AreEqual<string>(userNames[1], TestUtils.Users[2].Username);
            Assert.AreEqual<string>(userNames[2], TestUtils.Users[4].Username);

            //-------------------------------------------------------
            // * wild card user mask in existing role - success
            //-------------------------------------------------------
            userMask = "TEST_USER:*";
            roleName = TestUtils.ProviderRoles[0];
            userNames = _provider.FindUsersInRole(roleName, userMask);
            Assert.AreEqual<int>(3, userNames.Length);
            Assert.AreEqual<string>(userNames[0], TestUtils.Users[0].Username);
            Assert.AreEqual<string>(userNames[1], TestUtils.Users[2].Username);
            Assert.AreEqual<string>(userNames[2], TestUtils.Users[4].Username);

            //-------------------------------------------------------
            // empty user mask - error
            //-------------------------------------------------------
            userMask = string.Empty;
            roleName = TestUtils.ProviderRoles[0];
            try
            {
                userNames = _provider.FindUsersInRole(roleName, userMask);
                Assert.Fail("Provider did not throw expected ArgumentException");
            }
            catch(ArgumentException)
            {
                // ignore expected exception
            }

            //-------------------------------------------------------
            //  nonexistant user mask in existing role - success. no results
            //-------------------------------------------------------
            userMask = Guid.NewGuid().ToString();
            roleName = TestUtils.ProviderRoles[0];
            userNames = _provider.FindUsersInRole(roleName, userMask);
            Assert.IsNotNull(userNames);
            Assert.AreEqual<int>(0, userNames.Length);

            //-------------------------------------------------------
            //  null user mask - error
            //-------------------------------------------------------
            userMask = null;
            roleName = TestUtils.ProviderRoles[0];
            try
            {
                userNames = _provider.FindUsersInRole(roleName, userMask);
                Assert.Fail("Provider did not throw expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // ignore expected exception
            }

            //-------------------------------------------------------
            // valid user mask, empty rolename - error
            //-------------------------------------------------------
            userMask = TestUtils.Users[0].Username;
            roleName = string.Empty;
            try
            {
                userNames = _provider.FindUsersInRole(roleName, userMask);
                Assert.Fail("Provider did not throw expected ArgumentException");
            }
            catch (ArgumentException)
            {
                // ignore expected exception
            }

            //-------------------------------------------------------
            // valid user mask, null rolename - error
            //-------------------------------------------------------
            userMask = TestUtils.Users[0].Username;
            roleName = null;
            try
            {
                userNames = _provider.FindUsersInRole(roleName, userMask);
                Assert.Fail("Provider did not throw expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // ignore expected exception
            }

            //-------------------------------------------------------
            // valid user mask, nonexistant rolename - error
            //-------------------------------------------------------
            userMask = TestUtils.Users[0].Username;
            roleName = Guid.NewGuid().ToString();
            try
            {
                userNames = _provider.FindUsersInRole(roleName, userMask);
                Assert.Fail("Provider did not throw expected ProviderException");
            }
            catch (ProviderException)
            {
                //ignore expected exception
            }
          
        }

        [TestMethod]
        public void MembershipRole_GetAllRolesTests()
        {
            //-------------------------------------------------------
            // no parameters.  just execute
            //-------------------------------------------------------
            string[] roleNames;

            roleNames = _provider.GetAllRoles();
            Assert.IsNotNull(roleNames);
            Assert.AreEqual<int>(20, roleNames.Length);
        }

        [TestMethod]
        public void MembershipRole_RoleExistsTests()
        {
            bool result;
            string roleName;

            //-------------------------------------------------------
            // existing role - true
            //-------------------------------------------------------
            roleName = TestUtils.ProviderRoles[0];
            result = _provider.RoleExists(roleName);
            Assert.AreEqual(true, result);

            //-------------------------------------------------------
            // non existant role - false
            //-------------------------------------------------------
            roleName = Guid.NewGuid().ToString();
            result = _provider.RoleExists(roleName);
            Assert.AreEqual(false, result);

            //-------------------------------------------------------
            // null role name
            //-------------------------------------------------------
            roleName = null;
            try
            {
                result = _provider.RoleExists(roleName);
                Assert.Fail("Provider did not throw expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // empty role name
            //-------------------------------------------------------
            roleName = string.Empty;
            try
            {
                result = _provider.RoleExists(roleName);
                Assert.Fail("Provider did not throw expected ArgumentException");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }
        }

        [TestMethod]
        public void MembershipRole_GetRolesForUserTests()
        {
            string[] results;
            string userName;

            //-------------------------------------------------------
            // existing user with roles
            //-------------------------------------------------------
            userName = TestUtils.Users[6].Username;
            results = _provider.GetRolesForUser(userName);
            Assert.IsNotNull(results);
            Assert.AreEqual(3, results.Length);
            Assert.AreEqual<string>(TestUtils.ProviderRoles[1], results[0]);
            Assert.AreEqual<string>(TestUtils.ProviderRoles[2], results[1]);
            Assert.AreEqual<string>(TestUtils.ProviderRoles[3], results[2]);

            //-------------------------------------------------------
            // existing user without roles
            //-------------------------------------------------------
            userName = TestUtils.Users[12].Username;
            results = _provider.GetRolesForUser(userName);
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Length);

            //-------------------------------------------------------
            // null usernmae
            //-------------------------------------------------------
            userName = null;
            try
            {
                results = _provider.GetRolesForUser(userName);
                Assert.Fail("Provider did not throw expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
               //ignore expected exception
            }

            //-------------------------------------------------------
            // empty username
            //-------------------------------------------------------
            userName = string.Empty;
            try
            {
                results = _provider.GetRolesForUser(userName);
                Assert.Fail("Provider did not throw expected ArgumentException");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

        }

        [TestMethod]
        public void MembershipRole_GetUsersInRoleTests()
        {
            string roleName;
            string[] results;
            //-------------------------------------------------------
            // existing role no users
            //-------------------------------------------------------
            roleName = TestUtils.ProviderRoles[10];
            results = _provider.GetUsersInRole(roleName);
            Assert.IsNotNull(results);
            Assert.AreEqual<int>(0, results.Length);

            //-------------------------------------------------------
            // existing role with users
            //-------------------------------------------------------
            roleName = TestUtils.ProviderRoles[0];
            results = _provider.GetUsersInRole(roleName);
            Assert.IsNotNull(roleName);
            Assert.AreEqual<int>(3, results.Length);
            Assert.AreEqual<string>(TestUtils.Users[0].Username, results[0]);
            Assert.AreEqual<string>(TestUtils.Users[2].Username, results[1]);
            Assert.AreEqual<string>(TestUtils.Users[4].Username, results[2]);

            //-------------------------------------------------------
            // nonexisting role
            //-------------------------------------------------------
            roleName = Guid.NewGuid().ToString();
            try
            {
                results = _provider.GetUsersInRole(roleName);
                Assert.Fail("Provider did not throw expected ProviderException");
            }
            catch(ProviderException)
            {
                //ignore expected exception
            }
            //-------------------------------------------------------
            // null role
            //-------------------------------------------------------
            roleName = null;
            try
            {
                results = _provider.GetUsersInRole(roleName);
                Assert.Fail("Provider did not throw expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }
            //-------------------------------------------------------
            // empty role
            //-------------------------------------------------------
            roleName = string.Empty;
            try
            {
                results = _provider.GetUsersInRole(roleName);
                Assert.Fail("Provider did not throw expected ArgumentException");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }  
        }

        [TestMethod]
        public void MembershipRole_IsUserInRoleTests()
        {
            string userName;
            string roleName;
            bool result;

            //-------------------------------------------------------
            // existing user in existing role
            //-------------------------------------------------------
            userName = TestUtils.Users[0].Username;
            roleName = TestUtils.ProviderRoles[0];
            result = _provider.IsUserInRole(userName, roleName);
            Assert.AreEqual<bool>(true, result);

            //-------------------------------------------------------
            // existing user not in existing role
            //-------------------------------------------------------
            userName = TestUtils.Users[0].Username;
            roleName = TestUtils.ProviderRoles[10];
            result = _provider.IsUserInRole(userName, roleName);
            Assert.AreEqual<bool>(false, result);

            //-------------------------------------------------------
            // existing user, non existing role
            //-------------------------------------------------------
            userName = TestUtils.Users[0].Username;
            roleName = Guid.NewGuid().ToString();
            result = _provider.IsUserInRole(userName,roleName);
            Assert.IsFalse(result);
             
            //-------------------------------------------------------
            // non existing user, existing role
            //-------------------------------------------------------
            userName = Guid.NewGuid().ToString();
            roleName = TestUtils.ProviderRoles[0];
            result = _provider.IsUserInRole(userName, roleName);
            Assert.IsFalse(result);

            //-------------------------------------------------------
            // existing user, roleName contains comma
            //-------------------------------------------------------
            userName = Guid.NewGuid().ToString();
            roleName = TestUtils.ProviderRoles[0]+","+TestUtils.ProviderRoles[1];
            try
            {
                result = _provider.IsUserInRole(userName, roleName);
                Assert.Fail("Provider did not throw expected ArugumentException");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }
            
            //-------------------------------------------------------
            // null user, valid role
            //-------------------------------------------------------
            userName = null;
            roleName = TestUtils.ProviderRoles[0];
            try
            {
                result = _provider.IsUserInRole(userName, roleName);
                Assert.Fail("Provider did not throw expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }
            
            //-------------------------------------------------------
            // empty user, valid role
            //-------------------------------------------------------
            userName = string.Empty;
            roleName = TestUtils.ProviderRoles[0];
            try
            {
                result = _provider.IsUserInRole(userName, roleName);
                Assert.Fail("Provider did not throw expected ArgumentException");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // valid user, null role
            //-------------------------------------------------------
            userName = TestUtils.Users[0].Username;
            roleName = null;
            try
            {
                result = _provider.IsUserInRole(userName, roleName);
                Assert.Fail("Provider did not throw expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // valid user, empty role
            //-------------------------------------------------------
            userName = TestUtils.Users[0].Username;
            roleName = string.Empty;
            try
            {
                result = _provider.IsUserInRole(userName, roleName);
                Assert.Fail("Provider did not throw expected ArgumentException");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }
        }

        [TestMethod]
        public void MembershipRole_RemoveUsersFromRolesTests()
        {
            string[] users = new string[] { TestUtils.Users[16].Username };
            string[] roles = new string[] { };
            //-------------------------------------------------------
            // empty role list, error
            //-------------------------------------------------------
            try
            {
                _provider.RemoveUsersFromRoles(users, roles);
                Assert.Fail("Provider did not throw expected ArgumentException on empty roleName list");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // empty user list, error
            //-------------------------------------------------------
            users = new string[] { };
            roles = new string[] { TestUtils.ProviderRoles[0] };
            try
            {
                _provider.RemoveUsersFromRoles(users, roles);
                Assert.Fail("Provider did not throw expected ArgumentException on empty users list");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // null user list, error
            //-------------------------------------------------------
            users = null;
            roles = new string[] { TestUtils.ProviderRoles[0] };
            try
            {
                _provider.RemoveUsersFromRoles(users, roles);
                Assert.Fail("Provider did not throw expected ArgumentException on null user list");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // null role list, error
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[14].Username };
            roles = null;
            try
            {
                _provider.RemoveUsersFromRoles(users, roles);
                Assert.Fail("Provider did not throw expected ArgumentNullException on null role list");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // remove existing user from exiting role
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[8].Username };
            roles = new string[] { TestUtils.ProviderRoles[1] };
            _provider.RemoveUsersFromRoles(users, roles);

            //-------------------------------------------------------
            // remove several users from existing roles, some users
            //  not members of existing roles, error
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[4].Username, TestUtils.Users[6].Username };
            roles = new string[] { TestUtils.ProviderRoles[0], TestUtils.ProviderRoles[3] };
            try
            {
                _provider.RemoveUsersFromRoles(users, roles);
                Assert.Fail("Provider did not throw expected ProviderException");
            }
            catch (ProviderException)
            {
                //ignore expected order
            }

            //-------------------------------------------------------
            // remove existing users from existing roles except one role is null
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[6].Username, TestUtils.Users[10].Username };
            roles = new string[] { TestUtils.ProviderRoles[1], null };
            try
            {
                _provider.RemoveUsersFromRoles(users, roles);
                Assert.Fail("Provider did not throw expected ArgumentNullException when one role is null in the list");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // remove existing users from existing roles except one role is empty
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[20].Username, TestUtils.Users[22].Username };
            roles = new string[] { TestUtils.ProviderRoles[10], string.Empty };
            try
            {
                _provider.RemoveUsersFromRoles(users, roles);
                Assert.Fail("Provider did not throw expected ArgumentException when one role is null in the list");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // remove existing users from existing roles except one role is non-existant
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[20].Username, TestUtils.Users[22].Username };
            roles = new string[] { TestUtils.ProviderRoles[10], "TEST_" + Guid.NewGuid().ToString() };
            try
            {
                _provider.RemoveUsersFromRoles(users, roles);
                Assert.Fail("Provider did not throw expected ProviderException when one role did not exist");
            }
            catch (ProviderException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // remove existing users (except one user is null) from existing roles
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[20].Username, null };
            roles = new string[] { TestUtils.ProviderRoles[10], TestUtils.ProviderRoles[11] };
            try
            {
                _provider.RemoveUsersFromRoles(users, roles);
                Assert.Fail("Provider did not throw expected ArgumentNull expception when one user is null");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // remove existing users (except one user is empty) from existing roles
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[20].Username, string.Empty };
            roles = new string[] { TestUtils.ProviderRoles[10], TestUtils.ProviderRoles[11] };
            try
            {
                _provider.RemoveUsersFromRoles(users, roles);
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // remove existing users from existing roles except one user
            //  doesn't exist
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[20].Username, "TEST_" + Guid.NewGuid().ToString() };
            roles = new string[] { TestUtils.ProviderRoles[10], TestUtils.ProviderRoles[11] };
            try
            {
                _provider.RemoveUsersFromRoles(users, roles);
            }
            catch (ProviderException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // remove existing user from existing roles except one user is not member of that role
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[30].Username, TestUtils.Users[6].Username };
            roles = new string[] { TestUtils.ProviderRoles[6], TestUtils.ProviderRoles[10] };
            try
            {
                _provider.RemoveUsersFromRoles(users, roles);

            }
            catch (ProviderException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // duplicate rolenames in list
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[30].Username, TestUtils.Users[31].Username };
            roles = new string[] { TestUtils.ProviderRoles[18], TestUtils.ProviderRoles[18] };
            try
            {
                _provider.RemoveUsersFromRoles(users, roles);
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // duplicate users in list
            //-------------------------------------------------------
            users = new string[] { TestUtils.Users[30].Username, TestUtils.Users[30].Username };
            roles = new string[] { TestUtils.ProviderRoles[18], TestUtils.ProviderRoles[19] };
            try
            {
                _provider.RemoveUsersFromRoles(users, roles);
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }
 
        }
    }
}
