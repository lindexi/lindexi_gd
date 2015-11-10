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
using System.Configuration.Provider;
using System.Reflection;
using System.Threading;
using System.Web.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ScrimpNet.Security.Providers.Tests_Project
{
    /// <summary>
    /// Exercise an ASP.Net membership provider.  Approximately 150 test cases
    /// </summary>
    [TestClass]
    public class WcfMembershipProviderTests
    {
        public WcfMembershipProviderTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

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
        private static MembershipProvider _provider;
        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void ClassInitialize(TestContext testContext)
        {
            _provider = TestUtils.ActiveMembershipProvider;
        }

        [TestInitialize]
        public  void TestInitialize()
        {
            TestUtils.CreateUserRecords(_provider);  //setup provider with known records.  This might be better in ClassInitialize but haven't proven tests will pass
        }
        #endregion
        [TestMethod]
        public void MembershipUser_FindByEMailTests()
        {
            Console.WriteLine("Running '{0}' using provider '{1}'",
                MethodInfo.GetCurrentMethod().Name, _provider.Name);

            string emailToMatch;
            int pageIndex;
            int pageSize;
            int totalRecords;
            MembershipUserCollection retList;

            //-------------------------------------------------------
            //  null emailToMatch string (0 item list)
            //-------------------------------------------------------
            emailToMatch = null;
            pageIndex = 0;
            pageSize = int.MaxValue - 1;
            try
            {
                retList = _provider.FindUsersByEmail(emailToMatch, pageIndex, pageSize, out totalRecords);
                Assert.Fail("Null email did not throw expected ArgumentNullException");
            }
            catch(ArgumentNullException)
            {
                //expected exception
            }

            //-------------------------------------------------------
            //  empty emailToMatch string (0 item list)
            //-------------------------------------------------------
            emailToMatch = "";
            pageIndex = 0;
            pageSize = int.MaxValue - 1;
            try
            {
                retList = _provider.FindUsersByEmail(emailToMatch, pageIndex, pageSize, out totalRecords);
                Assert.Fail("Empty email did not throw expected ArgumentException");
            }
            catch (ArgumentException)
            {
                //expected exception
            }
            
            //-------------------------------------------------------
            //  non existant emailToMatch string (0 item list)
            //-------------------------------------------------------
            emailToMatch = Guid.NewGuid().ToString();
            pageIndex = 0;
            pageSize = int.MaxValue - 1;
            retList = _provider.FindUsersByEmail(emailToMatch, pageIndex, pageSize, out totalRecords);
            Assert.IsNotNull(retList);
            Assert.AreEqual<int>(0, retList.Count);

            //-------------------------------------------------------
            //  expected match emailToMatch string (1 item list)
            //-------------------------------------------------------
            emailToMatch = TestUtils.Users[0].EMail;
            pageIndex = 0;
            pageSize = int.MaxValue - 1;
            retList = _provider.FindUsersByEmail(emailToMatch, pageIndex, pageSize, out totalRecords);
            Assert.IsNotNull(retList);
            Assert.AreEqual<int>(1, retList.Count);
            TestUtils.VerifyUserHeaderFields(retList);

            //-------------------------------------------------------
            // the following tests use wild cards to return a list
            //  of users.  According to MSDN, the wild cards are
            //  provider specific.  These tests are written
            //  for SqlMembershipProvider.  Other providers may
            //  use different wild cards or no wild cards
            //
            //  Format for email addresses are:
            //      TEST_EMAIL_XXX@test.com where XXX is a sequence
            //      number.  Generally 000-049 (see TestUtils.CreateUserRecords)
            //-------------------------------------------------------

            //-------------------------------------------------------
            // paging - full page
            //-------------------------------------------------------
            emailToMatch = "TEST_EMAIL_00%@test.com";
            pageIndex = 0;
            pageSize = 10;
            retList = _provider.FindUsersByEmail(emailToMatch, pageIndex, pageSize, out totalRecords);
            Assert.IsNotNull(retList);
            Assert.AreEqual<int>(10, retList.Count);
            Assert.AreEqual<int>(10, totalRecords);
            TestUtils.VerifyUserHeaderFields(retList);

            //-------------------------------------------------------
            // page index > total possible pages
            //-------------------------------------------------------
            emailToMatch = "TEST_EMAIL_00%@test.com";
            pageIndex = 1000;
            pageSize = 10;
            retList = _provider.FindUsersByEmail(emailToMatch, pageIndex, pageSize, out totalRecords);
            Assert.IsNotNull(retList);
            Assert.AreEqual<int>(0, retList.Count);
            Assert.AreEqual<int>(10, totalRecords);
            TestUtils.VerifyUserHeaderFields(retList);

            //-------------------------------------------------------
            // get second page of results
            //-------------------------------------------------------
            emailToMatch = "TEST_EMAIL_00%@test.com";
            pageIndex = 1; //0 based index
            pageSize = 3;
            retList = _provider.FindUsersByEmail(emailToMatch, pageIndex, pageSize, out totalRecords);
            Assert.IsNotNull(retList);
            Assert.AreEqual<int>(3, retList.Count);
            Assert.AreEqual<int>(10, totalRecords);
            TestUtils.VerifyUserHeaderFields(retList);

            //-------------------------------------------------------
            // get last page of results
            //-------------------------------------------------------
            emailToMatch = "TEST_EMAIL_00%@test.com";
            pageIndex = 3; //0 based index
            pageSize = 3;
            retList = _provider.FindUsersByEmail(emailToMatch, pageIndex, pageSize, out totalRecords);
            Assert.IsNotNull(retList);
            Assert.AreEqual<int>(1, retList.Count);
            Assert.AreEqual<int>(10, totalRecords);
            TestUtils.VerifyUserHeaderFields(retList);

            //-------------------------------------------------------
            // negative page index
            //-------------------------------------------------------
            emailToMatch = "TEST_EMAIL_00%@test.com";
            pageIndex = -1; //0 based index
            pageSize = 3;
            try
            {
                retList = _provider.FindUsersByEmail(emailToMatch, pageIndex, pageSize, out totalRecords);
                Assert.Fail("Able to execute FindUsersByEmail with negative page index");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }
            catch (Exception ex)
            {
                string s = ex.ToString();
            }

            //-------------------------------------------------------
            // negative page size
            //-------------------------------------------------------
            emailToMatch = "TEST_EMAIL_00%@test.com";
            pageIndex = 1; //0 based index
            pageSize = -3;
            try
            {
                retList = _provider.FindUsersByEmail(emailToMatch, pageIndex, pageSize, out totalRecords);
                Assert.Fail("Able to execute FindUsersByEmail with negative page size");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }
        }

        [TestMethod]
        public void MembershipUser_FindByUserNameTests()
        {
            Console.WriteLine("Running '{0}' using provider '{1}'",
                MethodInfo.GetCurrentMethod().Name, _provider.Name);
            //TestUtils.CreateUserRecords(_provider); //refresh user records TODO find record that hasn't changed so we don't have to recreate users
            string usernameToMatch;
            int pageIndex;
            int pageSize;
            int totalRecords;
            MembershipUserCollection retList;
           
            //-------------------------------------------------------
            //  null usernameToMatch string - ArgumentNullException
            //-------------------------------------------------------
            usernameToMatch = null;
            pageIndex = 0;
            pageSize = int.MaxValue - 1;
            try
            {
                retList = _provider.FindUsersByName(usernameToMatch, pageIndex, pageSize, out totalRecords);
                Assert.Fail("Able to execute FindUsersByName with NULL as userNameToMatch");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            //  empty usernameToMatch string - ArgumentException
            //-------------------------------------------------------
            usernameToMatch = "";
            pageIndex = 0;
            pageSize = int.MaxValue - 1;
            try
            {
                retList = _provider.FindUsersByName(usernameToMatch, pageIndex, pageSize, out totalRecords);
                Assert.Fail("Able to execute FineUsersByName with empty string ('') as usernameToMatch");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            //  non existant usernameToMatch string (0 item list)
            //-------------------------------------------------------
            usernameToMatch = Guid.NewGuid().ToString();
            pageIndex = 0;
            pageSize = int.MaxValue - 1;
            retList = _provider.FindUsersByName(usernameToMatch, pageIndex, pageSize, out totalRecords);
            Assert.IsNotNull(retList);
            Assert.AreEqual<int>(0, retList.Count);

            //-------------------------------------------------------
            //  expected match usernameToMatch string (1 item list)
            //-------------------------------------------------------
            usernameToMatch = TestUtils.Users[0].Username;
            pageIndex = 0;
            pageSize = int.MaxValue - 1;
            retList = _provider.FindUsersByName(usernameToMatch, pageIndex, pageSize, out totalRecords);
            Assert.IsNotNull(retList);
            Assert.AreEqual<int>(1, retList.Count);
            TestUtils.VerifyUserHeaderFields(retList);

            //-------------------------------------------------------
            // the following tests use wild cards to return a list
            //  of users.  According to MSDN, the wild cards are
            //  provider specific.  These tests are written
            //  for SqlMembershipProvider.  Other providers may
            //  use different wild cards or no wild cards
            //
            //  Format for email addresses are:
            //      TEST_USER:XXX where XXX is a sequence
            //      number.  Generally 000-049 (see TestUtils.CreateUserRecords)
            //-------------------------------------------------------

            //-------------------------------------------------------
            // paging - full page
            //-------------------------------------------------------
            usernameToMatch = "TEST_USER:00%";
            pageIndex = 0;
            pageSize = 10;
            retList = _provider.FindUsersByName(usernameToMatch, pageIndex, pageSize, out totalRecords);
            Assert.IsNotNull(retList);
            Assert.AreEqual<int>(10, retList.Count);
            Assert.AreEqual<int>(10, totalRecords);
            TestUtils.VerifyUserHeaderFields(retList);

            //-------------------------------------------------------
            // page index > total possible pages
            //-------------------------------------------------------
            usernameToMatch = "TEST_USER:00%";
            pageIndex = 1000;
            pageSize = 10;
            retList = _provider.FindUsersByName(usernameToMatch, pageIndex, pageSize, out totalRecords);
            Assert.IsNotNull(retList);
            Assert.AreEqual<int>(0, retList.Count);
            Assert.AreEqual<int>(10, totalRecords);
            TestUtils.VerifyUserHeaderFields(retList);

            //-------------------------------------------------------
            // get second page of results
            //-------------------------------------------------------
            usernameToMatch = "TEST_USER:00%";
            pageIndex = 1; //0 based index
            pageSize = 3;
            retList = _provider.FindUsersByName(usernameToMatch, pageIndex, pageSize, out totalRecords);
            Assert.IsNotNull(retList);
            Assert.AreEqual<int>(3, retList.Count);
            Assert.AreEqual<int>(10, totalRecords);
            TestUtils.VerifyUserHeaderFields(retList);

            //-------------------------------------------------------
            // get last page of results
            //-------------------------------------------------------
            usernameToMatch = "TEST_USER:00%";
            pageIndex = 3; //0 based index
            pageSize = 3;
            retList = _provider.FindUsersByName(usernameToMatch, pageIndex, pageSize, out totalRecords);
            Assert.IsNotNull(retList);
            Assert.AreEqual<int>(1, retList.Count);
            Assert.AreEqual<int>(10, totalRecords);
            TestUtils.VerifyUserHeaderFields(retList);

            //-------------------------------------------------------
            // negative page index - ArgumentException
            //-------------------------------------------------------
            usernameToMatch = "TEST_USER:00%";
            pageIndex = -1; //0 based index
            pageSize = 3;
            try
            {
                retList = _provider.FindUsersByName(usernameToMatch, pageIndex, pageSize, out totalRecords);
                Assert.Fail("Expected ArugmentException not thrown.  Able to execute FindUsersByName with negative page index");
            }
            catch (ArgumentException)
            {
               //ignore expected exception
            }

            //-------------------------------------------------------
            // negative page size - ArgumentException
            //-------------------------------------------------------
            usernameToMatch = "TEST_USER:00%";
            pageIndex = 1; //0 based index
            pageSize = -3;
            try
            {
                retList = _provider.FindUsersByName(usernameToMatch, pageIndex, pageSize, out totalRecords);
                Assert.Fail("Expected ArugmentException not thrown.  Able to execute FindUsersByName with negative page size");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }
        }

        [TestMethod]
        public void MembershipUser_ResetPasswordTests()
        {
            string newpassword;
            string username;
            string answer;
            if (_provider.EnablePasswordReset == false)
            {
                Console.WriteLine("Provider is set to EnablePasswordReset=false.  Reset tests are ignored");
                return;
            }
            //-------------------------------------------------------
            //  valid username & answer
            //-------------------------------------------------------
            username = TestUtils.Users[6].Username;
            answer = TestUtils.Users[6].PasswordAnswer;

            bool validateResult = _provider.ValidateUser(username, TestUtils.Users[6].Password);
            Assert.AreEqual<bool>(true, validateResult);

            newpassword = _provider.ResetPassword(username, answer);

            validateResult = _provider.ValidateUser(username, TestUtils.Users[6].Password); //verify old password doesn't work
            Assert.AreEqual<bool>(false, validateResult);

            validateResult = _provider.ValidateUser(username, newpassword); //verify new password works
            Assert.AreEqual<bool>(true, validateResult);

            //-------------------------------------------------------
            //  invalid username & valid answer
            //-------------------------------------------------------
            username = Guid.NewGuid().ToString();
            answer = TestUtils.Users[4].PasswordAnswer;
            try
            {
                newpassword = _provider.ResetPassword(username, answer);
            }
            catch (ProviderException ex)
            {
                Assert.AreEqual<string>("The user was not found.", ex.Message);
            }

            //-------------------------------------------------------
            //  invalid username & invalid answer
            //-------------------------------------------------------
            username = Guid.NewGuid().ToString();
            answer = Guid.NewGuid().ToString();
            try
            {
                newpassword = _provider.ResetPassword(username, answer);
            }
            catch (ProviderException ex)
            {
                Assert.AreEqual<string>("The user was not found.", ex.Message);
            }

            //-------------------------------------------------------
            //  null username 
            //-------------------------------------------------------
            username = null;
            answer = TestUtils.Users[4].PasswordAnswer;
            try
            {
                newpassword = _provider.ResetPassword(username, answer);
                Assert.Fail("Provider did not throw expected ArgumentNullException when username is null");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            //  empty username
            //-------------------------------------------------------
            username = string.Empty;
            answer = TestUtils.Users[4].PasswordAnswer;
            try
            {
                newpassword = _provider.ResetPassword(username, answer);
                Assert.Fail("Provider did not throw expected ArgumentException when username is empty ''");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            //  null answer
            //-------------------------------------------------------
            username = TestUtils.Users[4].Username;
            answer = null;
            try
            {
                newpassword = _provider.ResetPassword(username, answer);
                Assert.Fail("Provider did not throw expected ArgumentNullException when answer is null");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            //  empty answer
            //-------------------------------------------------------
            username = TestUtils.Users[4].Username;
            answer = string.Empty;
            try
            {
                newpassword = _provider.ResetPassword(username, answer);
                Assert.Fail("Provider did not throw expected ArgumentException when answer is empty");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }


        }
        [TestMethod]
        public void MembershipUser_GetNameByEMailTests()
        {
            //-------------------------------------------------------
            // return a user from email
            //-------------------------------------------------------
            string userName = _provider.GetUserNameByEmail(TestUtils.Users[0].EMail);
            Assert.AreEqual<string>(TestUtils.Users[0].Username, userName);

            //-------------------------------------------------------
            // null email - Null argument exception
            //-------------------------------------------------------
            userName = _provider.GetUserNameByEmail(null);
            Assert.IsNull(userName);         

            //-------------------------------------------------------
            //  empty email parameter
            //-------------------------------------------------------
            userName = _provider.GetUserNameByEmail("");
            Assert.IsNull(userName);

            //-------------------------------------------------------
            // email not found
            //-------------------------------------------------------
            string email = Guid.NewGuid().ToString() + "@" + Guid.NewGuid() + ".com";
            userName = _provider.GetUserNameByEmail(email);
            Assert.IsNull(userName);

            //-------------------------------------------------------
            // SqlMembershipProvider doesn't do thorough validation
            //  on email address format.  Further validation will
            //  be necessary if a different provider does do 
            //  more robust validation 
            //-------------------------------------------------------
        }
        /// <summary>
        /// NOTE:  DO NOT use this method call against a live source (e.g. ActiveDirectory)
        /// because it returns all rows;
        /// </summary>
        [TestMethod]
        public void MembershipUser_GetAllTests()
        {
            MembershipUserCollection users;
            int pageIndex;
            int pageSize;
            int totalRecords;

            //-------------------------------------------------------
            // Normal retrieval
            //-------------------------------------------------------
            pageIndex = 0;
            pageSize = 10;
            users = _provider.GetAllUsers(pageIndex, pageSize, out totalRecords);
            Assert.IsNotNull(users);
            TestUtils.VerifyUserHeaderFields(users);

            //-------------------------------------------------------
            // page index > number of pages
            //-------------------------------------------------------
            pageIndex = totalRecords;
            pageSize = 10;
            users = _provider.GetAllUsers(pageIndex, pageSize, out totalRecords);
            Assert.IsNotNull(users);
            Assert.AreEqual<int>(0, users.Count);

            //-------------------------------------------------------
            // page index < 0
            //-------------------------------------------------------
            pageIndex = -1;
            pageSize = 10;
            try
            {
                users = _provider.GetAllUsers(pageIndex, pageSize, out totalRecords);
                Assert.Fail("GetAllUsers should throw an ArgumentException when pageIndex < 0");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // page size < 0
            //-------------------------------------------------------
            pageIndex = 0;
            pageSize = -1;
            try
            {
                users = _provider.GetAllUsers(pageIndex, pageSize, out totalRecords);
                Assert.Fail("GetAllUsers should throw an ArgumentException when pageSize < 0");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // page size * pageIndex > int.MaxValue
            //-------------------------------------------------------
            pageIndex = int.MaxValue - 1;
            pageSize = 5;
            try
            {
                users = _provider.GetAllUsers(pageIndex, pageSize, out totalRecords);
                Assert.Fail("GetAllUsers should throw an ArgumentException when pageSize*pageIndex > int.MaxValue");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // return a partial page
            //-------------------------------------------------------
            pageSize = 7;
            pageIndex = (int)(totalRecords / pageSize);
            users = _provider.GetAllUsers(pageIndex, pageSize, out totalRecords);
            Assert.AreEqual<int>(totalRecords % pageSize, users.Count);


        }

        [TestMethod]
        public void MembershipUser_UserNameGetByEMailTests()
        {
            string email;
            string userName;

            //-------------------------------------------------------
            // valid email - succeed
            //-------------------------------------------------------
            email = TestUtils.Users[0].EMail;
            userName = _provider.GetUserNameByEmail(email);
            Assert.IsNotNull(userName);
            Assert.AreEqual<string>(TestUtils.Users[0].Username, userName);

            //-------------------------------------------------------
            // valid email but not in database - fail
            //-------------------------------------------------------
            email = "missing.addresss@test.com";
            userName = _provider.GetUserNameByEmail(email);
            Assert.IsNull(userName);

            //-------------------------------------------------------
            // null email address
            //-------------------------------------------------------
            email = null;
            userName = _provider.GetUserNameByEmail(email);
            Assert.IsNull(userName);

            //-------------------------------------------------------
            // empty email address
            //-------------------------------------------------------
            email = "";
            userName = _provider.GetUserNameByEmail(email);
            Assert.IsNull(userName);

            //-------------------------------------------------------
            // verify provider requires a valid email format
            //
            //  NOTE: SqlMembershipProvider will accept any non-null
            //      string.  It is recommended that custom providers
            //      validate an email address format
            //-------------------------------------------------------
            email = "missingdomain";
            userName = _provider.GetUserNameByEmail(email);
            Assert.IsNull(userName);
        }

        [TestMethod]
        public void MembershipUser_GetUserByUserNameAndProviderKeyTests()
        {
            MembershipUser user;
            string userName;
            object providerKey;  //must be a Guid per MSDN docs
            bool updateFlag;

            //-------------------------------------------------------
            //  SqlMembershipProvider sets lastActivityDate equal to
            //      CreateDate when user is created.  Thus we
            //      delay here so we can validate the updateFlag is
            //      really changing the lastActivityDate
            //-------------------------------------------------------
            Thread.Sleep(15000);

            //-------------------------------------------------------
            //  exisiting user name - succeed, do not update activity
            //-------------------------------------------------------
            userName = TestUtils.Users[0].Username;
            updateFlag = false;
            user = _provider.GetUser(userName, updateFlag);
            Assert.IsNotNull(user);
            TestUtils.VerifyUserHeaderFields(user);
            Assert.AreEqual<DateTime>(user.CreationDate, user.LastActivityDate);

            //-------------------------------------------------------
            //  exisiting user name - succeed, update last activity
            //-------------------------------------------------------
            userName = TestUtils.Users[0].Username;
            updateFlag = true;
            user = _provider.GetUser(userName, updateFlag);
            Assert.IsNotNull(user);
            TestUtils.VerifyUserHeaderFields(user);
            Assert.AreNotEqual<DateTime>(user.CreationDate, user.LastActivityDate);

            MembershipUser user2 = _provider.GetUser(userName, false); //verify update was actually persisted
            Assert.AreEqual<DateTime>(user.LastActivityDate, user2.LastActivityDate);

            //-------------------------------------------------------
            //  non existing user name - fail, do not update last activity
            //-------------------------------------------------------
            userName = Guid.NewGuid().ToString();
            updateFlag = false;
            user = _provider.GetUser(userName, updateFlag);
            Assert.IsNull(user, "Able to retrieve user with userName = '{0}'", userName);

            //-------------------------------------------------------
            //  non existing user name - fail, update last activity
            //-------------------------------------------------------
            userName = Guid.NewGuid().ToString();
            updateFlag = true;
            user = _provider.GetUser(userName, updateFlag);
            Assert.IsNull(user, "Able to retrieve user with userName = '{0}'", userName);

            //-------------------------------------------------------
            //  null user name - fail, update last activity
            //-------------------------------------------------------
            userName = null;
            updateFlag = true;
            try
            {
                user = _provider.GetUser(userName, updateFlag);
                Assert.Fail("GetUser did not throw expected ArgumentNullException on null username");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }


            //-------------------------------------------------------
            //  null user name - fail, do not update last activity
            //-------------------------------------------------------
            userName = null;
            updateFlag = false;
            try
            {
                user = _provider.GetUser(userName, updateFlag);
                Assert.Fail("GetUser did not throw expected ArgumentNullException on null username");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            //  empty user name - fail, update last activity
            //-------------------------------------------------------
            userName = string.Empty;
            updateFlag = true;
            user = _provider.GetUser(userName, updateFlag);
            Assert.IsNull(user);

            //-------------------------------------------------------
            //  null user name - fail, do not update last activity
            //-------------------------------------------------------
            userName = string.Empty;
            updateFlag = false;
            user = _provider.GetUser(userName, updateFlag);
            Assert.IsNull(user);

            //-------------------------------------------------------
            //  exisiting provider key - succeed, do not update activity
            //-------------------------------------------------------
            providerKey = TestUtils.Users[1].ProviderKey;
            updateFlag = false;
            user = _provider.GetUser(providerKey, updateFlag);
            Assert.IsNotNull(user);
            TestUtils.VerifyUserHeaderFields(user);
            Assert.AreEqual<DateTime>(user.CreationDate, user.LastActivityDate);

            //-------------------------------------------------------
            //  exisiting provider key - succeed, update last activity
            //-------------------------------------------------------
            providerKey = TestUtils.Users[1].ProviderKey;
            updateFlag = true;
            user = _provider.GetUser(providerKey, updateFlag);
            Assert.IsNotNull(user);
            TestUtils.VerifyUserHeaderFields(user);
            Assert.AreNotEqual<DateTime>(user.CreationDate, user.LastActivityDate);

            user2 = _provider.GetUser(providerKey, false); //verify update was actually persisted
            Assert.AreEqual<DateTime>(user.LastActivityDate, user2.LastActivityDate);

            //-------------------------------------------------------
            //  non existing provider key - fail, do not update last activity
            //-------------------------------------------------------
            providerKey = Guid.NewGuid();
            updateFlag = false;
            user = _provider.GetUser(providerKey, updateFlag);
            Assert.IsNull(user, "Able to retrieve user with userName = '{0}'", providerKey);

            //-------------------------------------------------------
            //  non existing provider key - fail, update last activity
            //-------------------------------------------------------
            providerKey = Guid.NewGuid();
            updateFlag = true;
            user = _provider.GetUser(providerKey, updateFlag);
            Assert.IsNull(user, "Able to retrieve user with userName = '{0}'", providerKey);

            //-------------------------------------------------------
            // non-Guid object - fail do not update last activity
            //-------------------------------------------------------
            providerKey = Guid.NewGuid();
            updateFlag = false;
            try
            {
                user = _provider.GetUser((object)"ThisIsNonGuid", updateFlag);
                Assert.Fail("GetUser(object,bool) failed to throw expected ArgumentException when using non-Guid type");
            }
            catch (FormatException)
            {
                //ignore expected exception
            }

        }

        [TestMethod]
        public void MembershipUser_GetPasswordTests()
        {
            string userName;
            string answer;
            string password;

            if (_provider.EnablePasswordRetrieval == false)
            {
                try
                {
                    password = _provider.GetPassword(TestUtils.Users[3].Username, TestUtils.Users[3].PasswordAnswer);
                    Assert.Fail("GetPassword did not throw expected NotSupportedException when provider configured NOT to return passwords");
                }
                catch (NotSupportedException)
                {
                    //ignore expected exception
                }
                Console.WriteLine("SqlMembershipProvider throws NotSupportedException before parameter validation so remaining GetPassword test cases are not executed");
                return;
            }

            //NOTE:  The following tests are run only when enablePasswordRetrieval=true in .config file on SqlMembershipProvider
            //-------------------------------------------------------
            //  valid username,answer - succeed
            //-------------------------------------------------------
            userName = TestUtils.Users[3].Username;
            answer = TestUtils.Users[3].PasswordAnswer;
            password = _provider.GetPassword(userName, answer);
            Assert.AreEqual<string>(TestUtils.Users[3].Password, password);

            //-------------------------------------------------------
            //  missing username, valid answer - fail
            //-------------------------------------------------------
            userName = Guid.NewGuid().ToString();
            answer = TestUtils.Users[3].PasswordAnswer;
            try
            {
                password = _provider.GetPassword(userName, answer);
            }
            catch (ProviderException ex)
            {
                Assert.AreEqual<string>("The user was not found.", ex.Message);
            }

            //-------------------------------------------------------
            //  valid username, invalid answer - fail
            //-------------------------------------------------------
            userName = TestUtils.Users[3].Username;
            answer = Guid.NewGuid().ToString();
            try
            {
                password = _provider.GetPassword(userName, answer);
            }
            catch (MembershipPasswordException ex)
            {
                Assert.AreEqual<string>("The password-answer supplied is wrong.", ex.Message);
            }

            //-------------------------------------------------------
            //  null username, valid answer - fail
            //-------------------------------------------------------
            userName = null;
            answer = TestUtils.Users[3].PasswordAnswer;
            try
            {
                password = _provider.GetPassword(userName, answer);
                Assert.Fail("Provider did not throw expected ArgumentNullException for null userName");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            //  empty username, valid answer - fail
            //-------------------------------------------------------
            userName = string.Empty;
            answer = TestUtils.Users[3].PasswordAnswer;
            try
            {
                password = _provider.GetPassword(userName, answer);
                Assert.Fail("Provider did not throw expected ArgumentException on empty userName");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // valid username, null answer - fail
            //-------------------------------------------------------
            userName = TestUtils.Users[3].Username;
            answer = null;
            try
            {
                password = _provider.GetPassword(userName, answer);
                Assert.Fail("Provider did not throw expected ArgumentNullException on null answer");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // valid username, empty answer - fail
            //-------------------------------------------------------
            userName = TestUtils.Users[3].Username;
            answer = string.Empty;
            try
            {
                password = _provider.GetPassword(userName, answer);
                Assert.Fail("Provider did not throw expected ArgumentException on empty answer");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }
        }

        [TestMethod]
        public void MembershipUser_ChangePasswordTests()
        {
            bool result;
            string username;
            string oldpassword;
            string newpassword;

            //-------------------------------------------------------
            // change a user's password
            //-------------------------------------------------------
            username = TestUtils.Users[8].Username;
            oldpassword = TestUtils.Users[8].Password;
            newpassword = Guid.NewGuid().ToString();
            result = _provider.ChangePassword(username, oldpassword, newpassword);
            Assert.AreEqual<bool>(true, result);

            //-------------------------------------------------------
            //  null user name
            //-------------------------------------------------------
            username = null;
            oldpassword = TestUtils.Users[9].Password;
            newpassword = Guid.NewGuid().ToString();
            try
            {
                result = _provider.ChangePassword(username, oldpassword, newpassword);
                Assert.Fail("Provider did not throw expected ArgumentNullException on null username");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            //  empty user name
            //-------------------------------------------------------
            username = string.Empty;
            oldpassword = TestUtils.Users[9].Password;
            newpassword = Guid.NewGuid().ToString();
            try
            {
                result = _provider.ChangePassword(username, oldpassword, newpassword);
                Assert.Fail("Provider did not throw expected ArgumentException on empty username");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            //  old password null
            //-------------------------------------------------------
            username = TestUtils.Users[9].Username;
            oldpassword = null;
            newpassword = Guid.NewGuid().ToString();
            try
            {
                result = _provider.ChangePassword(username, oldpassword, newpassword);
                Assert.Fail("Provider did not throw expected ArgumentNullExceptoin on null old password");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            //  old password empty
            //-------------------------------------------------------
            username = TestUtils.Users[9].Username;
            oldpassword = string.Empty;
            newpassword = Guid.NewGuid().ToString();
            try
            {
                result = _provider.ChangePassword(username, oldpassword, newpassword);
                Assert.Fail("Provider did not throw expected ArgumentExceptoin on empty old password");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            //  old password not matching
            //-------------------------------------------------------
            username = TestUtils.Users[9].Username;
            oldpassword = Guid.NewGuid().ToString();
            newpassword = Guid.NewGuid().ToString();

            result = _provider.ChangePassword(username, oldpassword, newpassword);
            Assert.IsFalse(result);

            //-------------------------------------------------------
            //  new password null
            //-------------------------------------------------------
            username = TestUtils.Users[9].Username;
            oldpassword = TestUtils.Users[9].Password;
            newpassword = null;
            try
            {
                result = _provider.ChangePassword(username, oldpassword, newpassword);
                Assert.Fail("Provider did not throw expected ArgumentNullException on null password");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            //  new password empty
            //-------------------------------------------------------
            username = TestUtils.Users[9].Username;
            oldpassword = TestUtils.Users[9].Password;
            newpassword = string.Empty;
            try
            {
                result = _provider.ChangePassword(username, oldpassword, newpassword);
                Assert.Fail("Provider did not throw expected ArgumentException on empty password");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            //  new password complexity check: < min length
            //-------------------------------------------------------
            newpassword = "";
            if (_provider.MinRequiredNonAlphanumericCharacters > 0)
            {
                newpassword = new string('!', _provider.MinRequiredNonAlphanumericCharacters);
            }
            newpassword += new string('p', _provider.MinRequiredPasswordLength);
            newpassword = newpassword.Substring(0, _provider.MinRequiredPasswordLength - 1);
            try
            {
                result = _provider.ChangePassword(username, oldpassword, newpassword);
                Assert.Fail("Provider did not throw expected ArgumentException on password length too short");
            }
            catch (ArgumentException)
            {
                //swallow expected exception
            }

            //-------------------------------------------------------
            //  new password complexity check: > 128
            //-------------------------------------------------------
            newpassword = "";
            if (_provider.MinRequiredNonAlphanumericCharacters > 0)
            {
                newpassword = new string('!', _provider.MinRequiredNonAlphanumericCharacters);
            }
            newpassword += new string('p', 129);
            try
            {
                result = _provider.ChangePassword(username, oldpassword, newpassword);
                Assert.Fail("Provider did not throw expected ArgumentException on password too long");
            }
            catch (ArgumentException)
            {
                //swallow expected exception
            }


            //-------------------------------------------------------
            //  new password complexity check: correct length
            //      missing nonalphanumeric characters
            //-------------------------------------------------------
            newpassword = new string('p', _provider.MinRequiredPasswordLength + 3);
            try
            {
                result = _provider.ChangePassword(username, oldpassword, newpassword);
                Assert.Fail("Provider did not throw expected ArgumentException on password missing nonalphanumeric characters");
            }
            catch (ArgumentException)
            {
                //swallow expected exception
            }
        }
        [TestMethod]
        public void MembershipUser_ChangePasswordQATests()
        {
            string userName;
            string password;
            string newQuestion;
            string newAnswer;
            bool result;

            //-------------------------------------------------------
            // change password question and answer
            //-------------------------------------------------------
            userName = TestUtils.Users[11].Username;
            password = TestUtils.Users[11].Password;
            newAnswer = Guid.NewGuid().ToString();
            newQuestion = Guid.NewGuid().ToString();
            result = _provider.ChangePasswordQuestionAndAnswer(userName, password, newQuestion, newAnswer);
            Assert.AreEqual<bool>(true, result);

            //-------------------------------------------------------
            // null username
            //-------------------------------------------------------
            userName = null;
            password = TestUtils.Users[11].Password;
            newAnswer = Guid.NewGuid().ToString();
            newQuestion = Guid.NewGuid().ToString();
            try
            {
                result = _provider.ChangePasswordQuestionAndAnswer(userName, password, newQuestion, newAnswer);
                Assert.Fail("Provider did not throw expected ArgumenNullException on null username");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // empty username
            //-------------------------------------------------------
            userName = string.Empty;
            password = TestUtils.Users[11].Password;
            newAnswer = Guid.NewGuid().ToString();
            newQuestion = Guid.NewGuid().ToString();
            try
            {
                result = _provider.ChangePasswordQuestionAndAnswer(userName, password, newQuestion, newAnswer);
                Assert.Fail("Provider did not throw expected ArgumentException on empty username");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // invalid username
            //-------------------------------------------------------
            userName = Guid.NewGuid().ToString();
            password = TestUtils.Users[11].Password;
            newAnswer = Guid.NewGuid().ToString();
            newQuestion = Guid.NewGuid().ToString();

            result = _provider.ChangePasswordQuestionAndAnswer(userName, password, newQuestion, newAnswer);
            Assert.AreEqual<bool>(result, false);

            //-------------------------------------------------------
            // null password
            //-------------------------------------------------------
            userName = TestUtils.Users[11].Username;
            password = null;
            newAnswer = Guid.NewGuid().ToString();
            newQuestion = Guid.NewGuid().ToString();
            try
            {
                result = _provider.ChangePasswordQuestionAndAnswer(userName, password, newQuestion, newAnswer);
                Assert.Fail("Provider did not throw expected ArgumentNullException on null password");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // empty password
            //-------------------------------------------------------
            userName = TestUtils.Users[11].Username;
            password = string.Empty;
            newAnswer = Guid.NewGuid().ToString();
            newQuestion = Guid.NewGuid().ToString();
            try
            {
                result = _provider.ChangePasswordQuestionAndAnswer(userName, password, newQuestion, newAnswer);
                Assert.Fail("Provider did not throw expected ArgumentException on empty password");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // invalid password
            //-------------------------------------------------------
            userName = TestUtils.Users[11].Username;
            password = Guid.NewGuid().ToString();
            newAnswer = Guid.NewGuid().ToString();
            newQuestion = Guid.NewGuid().ToString();
            result = _provider.ChangePasswordQuestionAndAnswer(userName, password, newQuestion, newAnswer);
            Assert.AreEqual<bool>(false, result);

            //-------------------------------------------------------
            // null question
            //-------------------------------------------------------
            userName = TestUtils.Users[11].Username;
            password = TestUtils.Users[11].Password;
            newQuestion = null;
            newAnswer = Guid.NewGuid().ToString();
            try
            {
                result = _provider.ChangePasswordQuestionAndAnswer(userName, password, newQuestion, newAnswer);
                Assert.Fail("Provider did not throw expected ArgumentNullException on null question");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // question > 256 characters
            //
            //  NOTE:  for default SqlMembershipProvider
            //-------------------------------------------------------
            userName = TestUtils.Users[11].Username;
            password = TestUtils.Users[11].Password;
            newQuestion = new string('q', 257);
            newAnswer = Guid.NewGuid().ToString();
            try
            {
                result = _provider.ChangePasswordQuestionAndAnswer(userName, password, newQuestion, newAnswer);
                Assert.Fail("Provider did not throw expected ArgumentException on question > 256 characters");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // empty question
            //-------------------------------------------------------
            userName = TestUtils.Users[11].Username;
            password = string.Empty;
            newQuestion = TestUtils.Users[11].PasswordQuestion;
            newAnswer = Guid.NewGuid().ToString();
            try
            {
                result = _provider.ChangePasswordQuestionAndAnswer(userName, password, newQuestion, newAnswer);
                Assert.Fail("Provider did not throw expected ArgumentException on empty question");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // null answer
            //-------------------------------------------------------
            userName = TestUtils.Users[11].Username;
            password = TestUtils.Users[11].Password;
            newQuestion = TestUtils.Users[11].PasswordQuestion;
            newAnswer = null;
            try
            {
                result = _provider.ChangePasswordQuestionAndAnswer(userName, password, newQuestion, newAnswer);
                Assert.Fail("Provider did not throw expected ArgumentNullException on null answer");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // empty answer
            //-------------------------------------------------------
            userName = TestUtils.Users[11].Username;
            password = TestUtils.Users[11].Password;
            newQuestion = TestUtils.Users[11].PasswordQuestion;
            newAnswer = string.Empty;
            try
            {
                result = _provider.ChangePasswordQuestionAndAnswer(userName, password, newQuestion, newAnswer);
                Assert.Fail("Provider did not throw expected ArgumentException on empty '' answer");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // answer > 128 characters
            //
            //  NOTE: SqlMembershipProvider specific
            //-------------------------------------------------------
            userName = TestUtils.Users[11].Username;
            password = TestUtils.Users[11].Password;
            newQuestion = TestUtils.Users[11].PasswordQuestion;
            newAnswer = new string('a', 129);
            try
            {
                result = _provider.ChangePasswordQuestionAndAnswer(userName, password, newQuestion, newAnswer);
                Assert.Fail("Provider did not throw expected ArgumentException on answer > 129");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // same answer
            //-------------------------------------------------------
            userName = TestUtils.Users[11].Username;
            password = TestUtils.Users[11].Password;
            newQuestion = TestUtils.Users[11].PasswordQuestion;
            newAnswer = TestUtils.Users[11].PasswordAnswer;

            result = _provider.ChangePasswordQuestionAndAnswer(userName, password, newQuestion, newAnswer);
            Assert.AreEqual<bool>(true, result, "Provider (SqlMembershipProvider) allows old answer to be used as new answer");
        }
        [TestMethod]
        public void MembershipUser_CreateTests()
        {

            Console.WriteLine("Running '{0}' using provider '{1}'",
                MethodInfo.GetCurrentMethod().Name, _provider.Name);

            int x = 200;
            string username = "TEST_USER:" + x.ToString("000");
            string email = "TEST_EMAIL:" + x.ToString("000");
            string question = "TEST_QUESTION:" + x.ToString("000");
            string answer = "TEST_ANSWER:" + x.ToString("000");
            string password = "TEST_PASSWORD:" + x.ToString("000");
            bool isActive = false; //only even numbers are active
            Guid providerKey = Guid.NewGuid();
            MembershipCreateStatus status;
            //-------------------------------------------------------
            // create (success)
            //-------------------------------------------------------
            _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.Success, status);

            //-------------------------------------------------------
            // duplicate user name (DuplicateUserName)
            //-------------------------------------------------------
            x = 201; //doesn't matter if this number is used below
            email = "TEST_EMAIL:" + x.ToString("000");
            question = "TEST_QUESTION:" + x.ToString("000");
            answer = "TEST_ANSWER:" + x.ToString("000");
            password = "TEST_PASSWORD:" + x.ToString("000");
            isActive = false; //only even numbers are active
           // providerKey = Guid.NewGuid();
            _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.DuplicateUserName, status);

            //-------------------------------------------------------
            // null user name (InvalidUserName)
            //-------------------------------------------------------
            x = 201;
            username = null;
            email = "TEST_EMAIL:" + x.ToString("000");
            question = "TEST_QUESTION:" + x.ToString("000");
            answer = "TEST_ANSWER:" + x.ToString("000");
            password = "TEST_PASSWORD:" + x.ToString("000");
            isActive = false;
            providerKey = Guid.NewGuid();
            MembershipUser user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidUserName, status);
            Assert.IsNull(user);

            //-------------------------------------------------------
            // empty user name (InvalidUserName)
            //-------------------------------------------------------
            x = 202;
            username = "";
            email = "TEST_EMAIL:" + x.ToString("000");
            question = "TEST_QUESTION:" + x.ToString("000");
            answer = "TEST_ANSWER:" + x.ToString("000");
            password = "TEST_PASSWORD:" + x.ToString("000");
            isActive = false;
            providerKey = Guid.NewGuid();
            user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidUserName, status);
            Assert.IsNull(user);

            //-------------------------------------------------------
            // username > 256 characters (InvalidUserName)
            //
            //  Default: AspNetSqlMembershipProvider
            //-------------------------------------------------------
            x = 203;
            username = new string('u', 257);
            email = "TEST_EMAIL:" + x.ToString("000");
            question = "TEST_QUESTION:" + x.ToString("000");
            answer = "TEST_ANSWER:" + x.ToString("000");
            password = "TEST_PASSWORD:" + x.ToString("000");
            isActive = false;
            providerKey = Guid.NewGuid();
            user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidUserName, status);
            Assert.IsNull(user);

            //-------------------------------------------------------
            // null password (InvalidPassword)
            //-------------------------------------------------------
            x = 204;
            username = "TEST_USER:" + x.ToString("000");
            email = "TEST_EMAIL:" + x.ToString("000");
            question = "TEST_QUESTION:" + x.ToString("000");
            answer = "TEST_ANSWER:" + x.ToString("000");
            password = null;
            isActive = false;
            providerKey = Guid.NewGuid();
            user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidPassword, status);
            Assert.IsNull(user);

            //-------------------------------------------------------
            // empty password (InvalidPassword)
            //-------------------------------------------------------
            x = 204;
            username = "TEST_USER:" + x.ToString("000");
            email = "TEST_EMAIL:" + x.ToString("000");
            question = "TEST_QUESTION:" + x.ToString("000");
            answer = "TEST_ANSWER:" + x.ToString("000");
            password = "";
            isActive = false;
            providerKey = Guid.NewGuid();
            user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidPassword, status);
            Assert.IsNull(user);

            //-------------------------------------------------------
            // password shorter than required (InvalidPassword)
            //-------------------------------------------------------
            x = 205;
            username = "TEST_USER:" + x.ToString("000");
            email = "TEST_EMAIL:" + x.ToString("000");
            question = "TEST_QUESTION:" + x.ToString("000");
            answer = "TEST_ANSWER:" + x.ToString("000");
            password = "";
            if (_provider.MinRequiredNonAlphanumericCharacters > 0)
            {
                password = new string('!', _provider.MinRequiredNonAlphanumericCharacters);
            }
            password += new string('p', _provider.MinRequiredPasswordLength);
            password = password.Substring(0, _provider.MinRequiredPasswordLength - 1);
            isActive = false;
            providerKey = Guid.NewGuid();
            user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidPassword, status);
            Assert.IsNull(user);

            //-------------------------------------------------------
            // password missing required non-alpha (InvalidPassword)
            //-------------------------------------------------------
            x = 206;
            username = "TEST_USER:" + x.ToString("000");
            email = "TEST_EMAIL:" + x.ToString("000");
            question = "TEST_QUESTION:" + x.ToString("000");
            answer = "TEST_ANSWER:" + x.ToString("000");
            password = new string('p', _provider.MinRequiredPasswordLength + 3);
            isActive = false;
            providerKey = Guid.NewGuid();
            user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidPassword, status);
            Assert.IsNull(user);

            //-------------------------------------------------------
            // password longer > 128 (InvaildPassword)
            //
            //  Default: AspNetSqlMembershipProvider
            //-------------------------------------------------------
            x = 207;
            username = "TEST_USER:" + x.ToString("000");
            email = "TEST_EMAIL:" + x.ToString("000");
            question = "TEST_QUESTION:" + x.ToString("000");
            answer = "TEST_ANSWER:" + x.ToString("000");
            password = "";
            if (_provider.MinRequiredNonAlphanumericCharacters > 0)
            {
                password = new string('!', _provider.MinRequiredNonAlphanumericCharacters);
            }
            password += new string('p', 129);
            isActive = false;
            providerKey = Guid.NewGuid();
            user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidPassword, status);
            Assert.IsNull(user);

            //-------------------------------------------------------
            // Password Regular Expresson (InvaildPassword)
            //
            //  This is configuration specific so you need to create
            //      a negative test here
            //-------------------------------------------------------
            //x = 208;
            //username = "TEST_USER:" + x.ToString("000");
            //email = "TEST_EMAIL:" + x.ToString("000");
            //question = "TEST_QUESTION:" + x.ToString("000");
            //answer = "TEST_ANSWER:" + x.ToString("000");
            //password = "";
            //if (_provider.MinRequiredNonAlphanumericCharacters > 0)
            //{
            //    password = new string('!', _provider.MinRequiredNonAlphanumericCharacters);
            //}
            //password += new string('p', 129);
            //isActive = false;
            //providerKey = Guid.NewGuid();
            //user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            //Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidPassword, status);
            //Assert.IsNull(user);

            #region Requires Question Answer Tests
            if (_provider.RequiresQuestionAndAnswer == true)
            {
                //-------------------------------------------------------
                // question is null (InvalidQuestion)
                //-------------------------------------------------------
                x = 209;
                username = "TEST_USER:" + x.ToString("000");
                email = "TEST_EMAIL:" + x.ToString("000");
                question = null;
                answer = "TEST_ANSWER:" + x.ToString("000");
                password = "";
                if (_provider.MinRequiredNonAlphanumericCharacters > 0)
                {
                    password = new string('!', _provider.MinRequiredNonAlphanumericCharacters);
                }
                password += new string('p', _provider.MinRequiredPasswordLength - _provider.MinRequiredNonAlphanumericCharacters - 1);
                isActive = false;
                providerKey = Guid.NewGuid();
                user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
                Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidQuestion, status);
                Assert.IsNull(user);

                //-------------------------------------------------------
                // question is empty string (InvalidQuestion)
                //-------------------------------------------------------
                x = 210;
                username = "TEST_USER:" + x.ToString("000");
                email = "TEST_EMAIL:" + x.ToString("000");
                question = "";
                answer = "TEST_ANSWER:" + x.ToString("000");
                password = "";
                if (_provider.MinRequiredNonAlphanumericCharacters > 0)
                {
                    password = new string('!', _provider.MinRequiredNonAlphanumericCharacters);
                }
                password += new string('p', _provider.MinRequiredPasswordLength - _provider.MinRequiredNonAlphanumericCharacters + 1);
                isActive = false;
                providerKey = Guid.NewGuid();
                user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
                Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidQuestion, status);
                Assert.IsNull(user);

                //-------------------------------------------------------
                // question > 256 (InvalidQuestion)
                //-------------------------------------------------------
                x = 211;
                username = "TEST_USER:" + x.ToString("000");
                email = "TEST_EMAIL:" + x.ToString("000");
                question = new string('q', 257);
                answer = "TEST_ANSWER:" + x.ToString("000");
                password = "";
                if (_provider.MinRequiredNonAlphanumericCharacters > 0)
                {
                    password = new string('!', _provider.MinRequiredNonAlphanumericCharacters);
                }
                password += new string('p', _provider.MinRequiredPasswordLength - _provider.MinRequiredNonAlphanumericCharacters + 1);
                isActive = false;
                providerKey = Guid.NewGuid();
                user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
                Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidQuestion, status);
                Assert.IsNull(user);

                //-------------------------------------------------------
                // answer is null (InvalidAnswer)
                //-------------------------------------------------------
                x = 212;
                username = "TEST_USER:" + x.ToString("000");
                email = "TEST_EMAIL:" + x.ToString("000");
                question = "TEST_QUESTION:" + x.ToString("000");
                answer = null;
                password = "";
                if (_provider.MinRequiredNonAlphanumericCharacters > 0)
                {
                    password = new string('!', _provider.MinRequiredNonAlphanumericCharacters);
                }
                password += new string('p', _provider.MinRequiredPasswordLength - _provider.MinRequiredNonAlphanumericCharacters + 1);
                isActive = false;
                providerKey = Guid.NewGuid();
                user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
                Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidAnswer, status);
                Assert.IsNull(user);

                //-------------------------------------------------------
                // answer is empty string (InvalidAnswer)
                //-------------------------------------------------------
                x = 213;
                username = "TEST_USER:" + x.ToString("000");
                email = "TEST_EMAIL:" + x.ToString("000");
                question = "TEST_QUESTION:" + x.ToString("000");
                answer = "";
                password = "";
                if (_provider.MinRequiredNonAlphanumericCharacters > 0)
                {
                    password = new string('!', _provider.MinRequiredNonAlphanumericCharacters);
                }
                password += new string('p', _provider.MinRequiredPasswordLength - _provider.MinRequiredNonAlphanumericCharacters + 1);
                isActive = false;
                providerKey = Guid.NewGuid();
                user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
                Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidAnswer, status);
                Assert.IsNull(user);

                //-------------------------------------------------------
                // answer > 128 (InvalidQuestion)
                //-------------------------------------------------------
                x = 214;
                username = "TEST_USER:" + x.ToString("000");
                email = "TEST_EMAIL:" + x.ToString("000");
                question = new string('q', 257);
                answer = new string('a', 129);
                password = "";
                if (_provider.MinRequiredNonAlphanumericCharacters > 0)
                {
                    password = new string('!', _provider.MinRequiredNonAlphanumericCharacters);
                }
                password += new string('p', _provider.MinRequiredPasswordLength - _provider.MinRequiredNonAlphanumericCharacters + 1);
                isActive = false;
                providerKey = Guid.NewGuid();
                user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
                Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidAnswer, status);
                Assert.IsNull(user);
            }

            #endregion Question Answer Tests

            //-------------------------------------------------------
            // email is null (InvalidEMail)
            //-------------------------------------------------------
            x = 215;
            username = "TEST_USER:" + x.ToString("000");
            email = null;
            question = "TEST_QUESTION:" + x.ToString("000");
            answer = "TEST_ANSWER:" + x.ToString("000");
            password = "";
            if (_provider.MinRequiredNonAlphanumericCharacters > 0)
            {
                password = new string('!', _provider.MinRequiredNonAlphanumericCharacters);
            }
            password += new string('p', _provider.MinRequiredPasswordLength - _provider.MinRequiredNonAlphanumericCharacters + 1);
            isActive = false;
            providerKey = Guid.NewGuid();
            user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidEmail, status);
            Assert.IsNull(user);

            //-------------------------------------------------------
            // email is empty string (InvalidEMail)
            //-------------------------------------------------------
            x = 216;
            username = "TEST_USER:" + x.ToString("000");
            email = "";
            question = "TEST_QUESTION:" + x.ToString("000");
            answer = "TEST_ANSWER:" + x.ToString("000");
            password = "";
            if (_provider.MinRequiredNonAlphanumericCharacters > 0)
            {
                password = new string('!', _provider.MinRequiredNonAlphanumericCharacters);
            }
            password += new string('p', _provider.MinRequiredPasswordLength - _provider.MinRequiredNonAlphanumericCharacters + 1);
            isActive = false;
            providerKey = Guid.NewGuid();
            user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidEmail, status);
            Assert.IsNull(user);

            //-------------------------------------------------------
            // no '@' symbol (InvalidEMail)
            // NOTE: AspNetSqlProvider does not do robust enough email validation
            //-------------------------------------------------------
            //x = 217;
            //username = "TEST_USER:" + x.ToString("000");
            //email = "testemail";
            //question = "TEST_QUESTION:" + x.ToString("000");
            //answer = "TEST_ANSWER:" + x.ToString("000");
            //password = "TEST_PASSWORD:000";// +x.ToString("000");
            //isActive = false;
            //providerKey = Guid.NewGuid();
            //user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            //Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidEmail, status);
            //Assert.IsNull(user);

            //-------------------------------------------------------
            // spaces in address (InvalidEMail)
            // NOTE: AspNetSqlProvider does not do robust enough email validation
            //-------------------------------------------------------
            //x = 218;
            //username = "TEST_USER:" + x.ToString("000");
            //email = "test email@mail.com";
            //question = "TEST_QUESTION:" + x.ToString("000");
            //answer = "TEST_ANSWER:000";
            //password = "TEST_PASSWORD:" + x.ToString("000");
            //isActive = false;
            //providerKey = Guid.NewGuid();
            //user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            //Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidEmail, status);
            //Assert.IsNull(user);

            //-------------------------------------------------------
            // missing TLD-top level domain (e.g. .com, .tv) (InvalidEMail)
            // NOTE: AspNetSqlProvider does not do robust enough email validation
            //-------------------------------------------------------
            //x = 219;
            //username = "TEST_USER:" + x.ToString("000");
            //email = "testemail@mail";
            //question = "TEST_QUESTION:" + x.ToString("000");
            //answer = "TEST_ANSWER:000";
            //password = "TEST_PASSWORD:" + x.ToString("000");
            //isActive = false;
            //providerKey = Guid.NewGuid();
            //user = _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            //Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidEmail, status);
            //Assert.IsNull(user);
            if (_provider.RequiresUniqueEmail == true)
            {
                //-------------------------------------------------------
                // duplicate email address (DuplicateEMail)
                //-------------------------------------------------------
                x = 220;
                username = "TEST_USER:" + x.ToString("000");
                email = "TEST_EMAIL_dup@dup.com";
                question = "TEST_QUESTION:" + x.ToString("000");
                answer = "TEST_ANSWER:" + x.ToString("000");
                password = "";
                if (_provider.MinRequiredNonAlphanumericCharacters > 0)
                {
                    password = new string('!', _provider.MinRequiredNonAlphanumericCharacters);
                }
                password += new string('p', _provider.MinRequiredPasswordLength - _provider.MinRequiredNonAlphanumericCharacters + 1);
                isActive = false; //only even numbers are active
                providerKey = Guid.NewGuid();

                _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
                Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.Success, status);
                x = 221;
                username = "TEST_USER:" + x.ToString("000");
                email = "TEST_EMAIL_dup@dup.com"; //duplicate email from above
                question = "TEST_QUESTION:" + x.ToString("000");
                answer = "TEST_ANSWER:" + x.ToString("000");
                password = "";
                if (_provider.MinRequiredNonAlphanumericCharacters > 0)
                {
                    password = new string('!', _provider.MinRequiredNonAlphanumericCharacters);
                }
                password += new string('p', _provider.MinRequiredPasswordLength - _provider.MinRequiredNonAlphanumericCharacters + 1);
                isActive = false; //only even numbers are active
                providerKey = Guid.NewGuid();
                _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
                Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.DuplicateEmail, status);
            } //requires uniuqe email

            //-------------------------------------------------------
            // non guid provider user key (InvalidProviderUserKey)
            //-------------------------------------------------------
            x = 222;
            username = "TEST_USER:" + x.ToString("000");
            email = "TEST_EMAIL_dup@dup.com"; //duplicate email from above
            question = "TEST_QUESTION:" + x.ToString("000");
            answer = "TEST_ANSWER:" + x.ToString("000");
            password = "TEST_PASSWORD:000";
            isActive = false; //only even numbers are active
            providerKey = Guid.NewGuid();
            _provider.CreateUser(username, password, email, question, answer, isActive, providerKey.ToString(), out status);
            Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidProviderUserKey, status);

            //-------------------------------------------------------
            // duplicate guid (DuplicateProviderUserKey)
            //-------------------------------------------------------
            x = 223;
            username = "TEST_USER:" + x.ToString("000");
            email = string.Format("TEST_EMAIL_{0}@email.com", x);
            question = "TEST_QUESTION:" + x.ToString("000");
            answer = "TEST_ANSWER:" + x.ToString("000");
            password = "TEST_PASSWORD:000";
            isActive = false; //only even numbers are active
            providerKey = Guid.NewGuid();
            _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.Success, status);
            x = 224;
            username = "TEST_USER:" + x.ToString("000"); //attempt duplicate
            email = string.Format("TEST_EMAIL_{0}@email.com", x);
            question = "TEST_QUESTION:" + x.ToString("000");
            answer = "TEST_ANSWER:" + x.ToString("000");
            password = "TEST_PASSWORD:000";
            isActive = false;

            _provider.CreateUser(username, password, email, question, answer, isActive, providerKey, out status);
            Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.DuplicateProviderUserKey, status);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// http://msdn.microsoft.com/en-us/library/system.web.security.sqlmembershipprovider.deleteuser.aspx
        /// </remarks>
        [TestMethod]
        public void MembershipUser_DeleteTests()
        {
            // reset user records
            TestUtils.CreateUserRecords(_provider);

            string username;
            bool relatedData;
            bool result;

            //-------------------------------------------------------
            // delete user records, related=true
            //
            //  NOTE:  Membership subsystem will delete all realted
            //          roles, web parts, and application settings
            //          for the user.
            //-------------------------------------------------------
            username = TestUtils.Users[14].Username;
            relatedData = true;
            result = _provider.DeleteUser(username, relatedData);
            Assert.AreEqual<bool>(true, result);

            //-------------------------------------------------------
            // delete user records, related=false
            //-------------------------------------------------------
            username = TestUtils.Users[15].Username;
            relatedData = true;
            result = _provider.DeleteUser(username, relatedData);
            Assert.AreEqual<bool>(true, result);
            result = _provider.DeleteUser(username, relatedData); //try to delete a second time to verify user is gone.
            Assert.AreEqual<bool>(false, result);

            //-------------------------------------------------------
            // username=null, related=false
            //-------------------------------------------------------
            username = null;
            relatedData = true;
            try
            {
                result = _provider.DeleteUser(username, relatedData);
                Assert.Fail("Provider did not throw expected ArgumentNullException on null username");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // username=empty, related=false
            //-------------------------------------------------------
            username = string.Empty;
            relatedData = true;
            try
            {
                result = _provider.DeleteUser(username, relatedData);
                Assert.Fail("Provider did not throw expected ArgumentException on empty '' username");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // username=non-existant user, related=false
            //-------------------------------------------------------
            username = Guid.NewGuid().ToString();
            relatedData = true;
            result = _provider.DeleteUser(username, relatedData);
            Assert.AreEqual<bool>(false, result);
        }

        [TestMethod]
        public void MembershipUser_UnlockTests()
        {
            string username;
            bool result;

            //-------------------------------------------------------
            // unlock user who is *NOT* locked
            //-------------------------------------------------------
            username = TestUtils.Users[14].Username;
            result = _provider.UnlockUser(username);

            //-------------------------------------------------------
            // unlock user is is locked
            //-------------------------------------------------------
            username = TestUtils.Users[14].Username;
            result = _provider.ValidateUser(username, TestUtils.Users[14].Password);
            Assert.IsTrue(result);

            for (int x = 0; x <= _provider.MaxInvalidPasswordAttempts; x++)
            {
                result = _provider.ValidateUser(username, Guid.NewGuid().ToString());
            }
            result = _provider.ValidateUser(username, TestUtils.Users[14].Password);
            Assert.IsFalse(result);
            MembershipUser user = _provider.GetUser(username, false);
            Assert.IsTrue(user.IsLockedOut);

            // user is now locked out so attempt to unlock
            result = _provider.UnlockUser(username);
            Assert.IsTrue(result);
            user = _provider.GetUser(username, false); //double check unlock status on member record
            Assert.IsFalse(user.IsLockedOut);

            //-------------------------------------------------------
            // non-existant username
            //-------------------------------------------------------
            username = Guid.NewGuid().ToString();
            result = _provider.UnlockUser(username);
            Assert.AreEqual<bool>(false, result);

            //-------------------------------------------------------
            // null username
            //-------------------------------------------------------
            username = null;
            try
            {
                result = _provider.UnlockUser(username);
                Assert.Fail("Provider did not throw expected ArgumentNullException on username is null");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // empty username
            //-------------------------------------------------------
            username = string.Empty;
            try
            {
                result = _provider.UnlockUser(username);
                Assert.Fail("Provider did not throw expected ArgumentException on username is empty ''");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

        }

        [TestMethod]
        public void MembershipUser_UpdateUsersTests()
        {
            string username = TestUtils.Users[26].Username;
            string password = TestUtils.Users[26].Password;

            MembershipUser user = _provider.GetUser(username, false);
            bool result;

            //-------------------------------------------------------
            // update an existing user
            //-------------------------------------------------------
            string comment = "TEST Comment" + Guid.NewGuid().ToString();
            string email = "TEST Email" + Guid.NewGuid().ToString();
            bool isApproved = true;
            DateTime lastActivityDate = new DateTime(2000, 10, 10, 10, 10, 10);
            DateTime lastLoginDate = new DateTime(1999, 9, 9, 9, 9, 9);

            user.Comment = comment;
            user.Email = email;
            user.IsApproved = isApproved;
            user.LastActivityDate = lastActivityDate;
            user.LastLoginDate = lastLoginDate;

            _provider.UpdateUser(user);

            user = null;
            user = _provider.GetUser(username, false);
            Assert.AreEqual<string>(comment, user.Comment);
            Assert.AreEqual<string>(email, user.Email);
            Assert.AreEqual<bool>(isApproved, user.IsApproved);
            Assert.AreEqual<DateTime>(lastActivityDate, user.LastActivityDate);
            Assert.AreEqual<DateTime>(lastLoginDate, user.LastLoginDate);

            result = _provider.ValidateUser(username, password);  //user is approved
            Assert.IsTrue(result);

            //-------------------------------------------------------
            // update an existing user, null comment, isApproved = false
            //-------------------------------------------------------
            comment = null;
            email = "TEST Email" + Guid.NewGuid().ToString();
            isApproved = false;
            lastActivityDate = new DateTime(2000, 10, 10, 10, 10, 10);
            lastLoginDate = new DateTime(1999, 9, 9, 9, 9, 9);

            user.Comment = comment;
            user.Email = email;
            user.IsApproved = isApproved;
            user.LastActivityDate = lastActivityDate;
            user.LastLoginDate = lastLoginDate;

            _provider.UpdateUser(user);

            user = null;
            user = _provider.GetUser(username, false);
            Assert.AreEqual<string>(comment, user.Comment);
            Assert.AreEqual<string>(email, user.Email);
            Assert.AreEqual<bool>(isApproved, user.IsApproved);
            Assert.AreEqual<DateTime>(lastActivityDate, user.LastActivityDate);
            Assert.AreEqual<DateTime>(lastLoginDate, user.LastLoginDate);

            result = _provider.ValidateUser(username, password);  //user is NOT approved
            Assert.IsFalse(result);

            //-------------------------------------------------------
            // update an existing user, null email
            //-------------------------------------------------------
            comment = null;
            email = null;
            isApproved = true;
            lastActivityDate = new DateTime(2000, 10, 10, 10, 10, 10);
            lastLoginDate = new DateTime(1999, 9, 9, 9, 9, 9);

            user.Comment = comment;
            user.Email = email;
            user.IsApproved = isApproved;
            user.LastActivityDate = lastActivityDate;
            user.LastLoginDate = lastLoginDate;
            try
            {
                _provider.UpdateUser(user);
                Assert.Fail("Provider did not throw expected ArgumentNullException on user.EMail is null");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // update an existing user, empty email
            //-------------------------------------------------------
            comment = null;
            email = string.Empty;
            isApproved = true;
            lastActivityDate = new DateTime(2000, 10, 10, 10, 10, 10);
            lastLoginDate = new DateTime(1999, 9, 9, 9, 9, 9);

            user.Comment = comment;
            user.Email = email;
            user.IsApproved = isApproved;
            user.LastActivityDate = lastActivityDate;
            user.LastLoginDate = lastLoginDate;
            try
            {
                _provider.UpdateUser(user);
                Assert.Fail("Provider did not throw expected ArgumentException on user.EMail is empty ''");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }


            //-------------------------------------------------------
            // update an existing user, email > 256
            //
            //  SqlMembershipProvider
            //-------------------------------------------------------
            comment = null;
            email = new string('e', 257);
            isApproved = true;
            lastActivityDate = new DateTime(2000, 10, 10, 10, 10, 10);
            lastLoginDate = new DateTime(1999, 9, 9, 9, 9, 9);

            user.Comment = comment;
            user.Email = email;
            user.IsApproved = isApproved;
            user.LastActivityDate = lastActivityDate;
            user.LastLoginDate = lastLoginDate;
            try
            {
                _provider.UpdateUser(user);
                Assert.Fail("Provider did not throw expected ArgumentNullException on user.EMail is empty ''");
            }
            catch (ArgumentException)
            {
                //ignore expected exception
            }

            //-------------------------------------------------------
            // update an existing user, email is duplicate
            //-------------------------------------------------------
            if (_provider.RequiresUniqueEmail == true)
            {
                comment = null;
                email = TestUtils.Users[20].EMail; //get duplicate email
                isApproved = true;
                lastActivityDate = new DateTime(2000, 10, 10, 10, 10, 10);
                lastLoginDate = new DateTime(1999, 9, 9, 9, 9, 9);

                user.Comment = comment;
                user.Email = email;
                user.IsApproved = isApproved;
                user.LastActivityDate = lastActivityDate;
                user.LastLoginDate = lastLoginDate;
                try
                {
                    _provider.UpdateUser(user);
                    Assert.Fail("Provider did not throw expected ProviderException on user.EMail is duplicate");
                }
                catch (ProviderException ex)
                {
                    Assert.AreEqual<string>("The E-mail supplied is invalid.", ex.Message);
                }

            } //if RequiresUniqueEMail

            //-------------------------------------------------------
            // called with null parameter
            //-------------------------------------------------------
            try
            {
                _provider.UpdateUser(null);
                Assert.Fail("Provider did not throw expected ArugmentNullException on user is null");
            }
            catch (ArgumentNullException)
            {
                //ignore expected exception
            }

        }
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// http://msdn.microsoft.com/en-us/library/system.web.security.membership.getnumberofusersonline.aspx
        /// </remarks>
        [Ignore]
        [TestMethod]
        public void MembershipUser_UsersOnLineTestsTimed()
        {
            int result;
            int sleepTime = ((Membership.UserIsOnlineTimeWindow * 60) * 1000);// +10000;
            Console.WriteLine("{0}: Sleeping {1} seconds to expire any user records marked currently online",
                MethodInfo.GetCurrentMethod().Name, sleepTime / 1000);

            Thread.Sleep(sleepTime); //wait for virutal user sessions to expire userIsOnlineTimeWindow of 1 min (see .config)

            //-------------------------------------------------------
            // default should be 0
            //-------------------------------------------------------
            result = _provider.GetNumberOfUsersOnline();
            Assert.AreEqual<int>(0, result);

            //-------------------------------------------------------
            // validate (log in) as user to update on-line count
            //-------------------------------------------------------
            string username = TestUtils.Users[24].Username; //only even numbered users are 'active' and available for logging in
            string password = TestUtils.Users[24].Password;
            bool validateResult = _provider.ValidateUser(username, password);
            Assert.AreEqual<bool>(true, validateResult);

            result = _provider.GetNumberOfUsersOnline();

            Assert.AreEqual<int>(result, 1);

        }

        [TestMethod]
        public void MembershipUser_ValidateTests()
        {
            bool result;
            string username;
            string password;

            //-------------------------------------------------------
            //  valid user and password (active)
            //-------------------------------------------------------
            username = TestUtils.Users[18].Username;
            password = TestUtils.Users[18].Password;

            result = _provider.ValidateUser(username, password);
            Assert.IsTrue(result);

            //-------------------------------------------------------
            //  valid user invalid password (active)
            //-------------------------------------------------------
            username = TestUtils.Users[18].Username;
            password = Guid.NewGuid().ToString();

            result = _provider.ValidateUser(username, password);
            Assert.IsFalse(result);

            //-------------------------------------------------------
            //  valid user, invlaid password (active)
            //-------------------------------------------------------
            username = TestUtils.Users[18].Username;
            password = Guid.NewGuid().ToString();

            result = _provider.ValidateUser(username, password);
            Assert.IsFalse(result);

            //-------------------------------------------------------
            //  valid user, valid password (inactive)
            //-------------------------------------------------------
            username = TestUtils.Users[19].Username;
            password = TestUtils.Users[19].Password;

            result = _provider.ValidateUser(username, password);
            Assert.IsFalse(result);

            //-------------------------------------------------------
            //  null username
            //-------------------------------------------------------
            username = null;
            password = TestUtils.Users[18].Password;

            result = _provider.ValidateUser(username, password);
            Assert.IsFalse(result);

            //-------------------------------------------------------
            //  empty username
            //-------------------------------------------------------
            username = string.Empty;
            password = TestUtils.Users[18].Password;

            result = _provider.ValidateUser(username, password);
            Assert.IsFalse(result);

            //-------------------------------------------------------
            //  null password
            //-------------------------------------------------------
            username = TestUtils.Users[18].Username;
            password = null;

            result = _provider.ValidateUser(username, password);
            Assert.IsFalse(result);

            //-------------------------------------------------------
            //  empty password
            //-------------------------------------------------------
            username = TestUtils.Users[18].Username;
            password = string.Empty;
        
            result = _provider.ValidateUser(username, password);
            Assert.IsFalse(result);

            //-------------------------------------------------------
            //  account locked - validation fails
            //-------------------------------------------------------            
            username = TestUtils.Users[18].Username;
            password = TestUtils.Users[18].Password;
            result = _provider.ValidateUser(username, password);
            Assert.IsTrue(result);

            for (int x = 0; x <= _provider.MaxInvalidPasswordAttempts; x++) //lock account by invalid logins
            {
                result = _provider.ValidateUser(username, Guid.NewGuid().ToString());
            }
            result = _provider.ValidateUser(username, password);
            Assert.IsFalse(result);
            MembershipUser user = _provider.GetUser(username, false);
            Assert.IsTrue(user.IsLockedOut);

        }

        /// <summary>
        /// NOTE:  These tests using embedded timer(s) so they may take longer to execute than expected
        /// </summary>
        [Ignore]
        [TestMethod]
        public void MembershipUser_ValidateAccountTimedTests()
        {
            string username;
            string password;
            bool result;

            //-------------------------------------------------------
            // attempt invalid logins
            //-------------------------------------------------------
            username = TestUtils.Users[20].Username;
            password = TestUtils.Users[20].Password;
            result = _provider.ValidateUser(username, password);
            Assert.IsTrue(result);

            for (int x = 0; x < _provider.MaxInvalidPasswordAttempts - 1; x++)
            {
                result = _provider.ValidateUser(username, Guid.NewGuid().ToString());
                Assert.IsFalse(result);
            }

            //-------------------------------------------------------
            //  wait for password attempt window to expire
            //-------------------------------------------------------
            int sleepTime = (_provider.PasswordAttemptWindow * 60000) + 10000;
            
            Thread.Sleep(sleepTime);

            //-------------------------------------------------------
            //  attempt to login.  If provider is ignoring attempt window
            //      these logins will lock the account
            //-------------------------------------------------------
            result = _provider.ValidateUser(username, Guid.NewGuid().ToString()); //these invalid login are after expiration of attempt window
            Assert.IsFalse(result);

            result = _provider.ValidateUser(username, Guid.NewGuid().ToString()); 
            Assert.IsFalse(result);

            //-------------------------------------------------------
            //  this is the last attempt before the account is locked
            //      so we use valid credentials and it should validate
            //-------------------------------------------------------
            result = _provider.ValidateUser(username, password);
            Assert.IsTrue(result);

        }

        /// <summary>
        /// NOTE:  These tests using embedded timer(s) so they may take longer to execute than expected
        /// </summary>
        [Ignore]
        [TestMethod]
        public void MembershipUser_PasswordAnswerTimedTests()
        {
            string username;
            string answer;
            bool result;
            if (_provider.RequiresQuestionAndAnswer == false)
            {
                Console.WriteLine("Question & Answers must be true if answer attempts are tracked.  These tests are ignored");
                return;
            }

            //-------------------------------------------------------
            // attempt invalid logins
            //-------------------------------------------------------
            username = TestUtils.Users[20].Username;
            answer = TestUtils.Users[20].PasswordAnswer;
            result = _provider.ValidateUser(username,answer);
            Assert.IsTrue(result);

            for (int x = 0; x < _provider.MaxInvalidPasswordAttempts - 1; x++)
            {
                result = _provider.ValidateUser(username, Guid.NewGuid().ToString());
                Assert.IsFalse(result);
            }

            //-------------------------------------------------------
            //  wait for password attempt window to expire
            //-------------------------------------------------------
            int sleepTime = (_provider.PasswordAttemptWindow * 60000) + 10000;

            Thread.Sleep(sleepTime);

            //-------------------------------------------------------
            //  attempt to login.  If provider is ignoring attempt window
            //      these logins will lock the account
            //-------------------------------------------------------
            result = _provider.ValidateUser(username, Guid.NewGuid().ToString()); //these invalid login are after expiration of attempt window
            Assert.IsFalse(result);

            result = _provider.ValidateUser(username, Guid.NewGuid().ToString());
            Assert.IsFalse(result);

            //-------------------------------------------------------
            //  this is the last attempt before the account is locked
            //      so we use valid credentials and it should validate
            //-------------------------------------------------------
            result = _provider.ValidateUser(username, answer);
            Assert.IsTrue(result);

        }

        private bool _passwordCancel;
        private bool _validateFired;
        private string _validateUsername;
        private string _validatePassword;

        [TestMethod,Ignore]
        public void MembershipUser_ValidatePasswordEventTests()
        {
            try
            {
                _provider.ValidatingPassword += _provider_ValidatingPassword;
                string username;
                string answer;
                bool result;
                string newPassword;
                string oldPassword;
                if (_provider.EnablePasswordReset == true)
                {
                    //---------------------------------------------------
                    // reset password (validate = true)
                    //---------------------------------------------------
                    username = TestUtils.Users[32].Username;
                    answer = TestUtils.Users[32].PasswordAnswer;
                    _passwordCancel = false;
                    newPassword = _provider.ResetPassword(username, answer);
                    Assert.IsTrue(_validateFired);
                    result = _provider.ValidateUser(username, newPassword); //verify password was changed
                    Assert.IsTrue(result);

                    //---------------------------------------------------
                    // reset password (validate = false)
                    //---------------------------------------------------
                    username = TestUtils.Users[34].Username;
                    answer = TestUtils.Users[34].PasswordAnswer;
                    oldPassword = TestUtils.Users[34].Password;
                    _passwordCancel = true;
                    try
                    {
                        newPassword = _provider.ResetPassword(username, answer);
                        Assert.Fail("ResetPassword::Provider did not throw expected ProviderException on custom validator failing");
                    }
                    catch (ProviderException ex)
                    {
                        Assert.AreEqual<string>("The custom password validation failed.", ex.Message);
                    }
                    result = _provider.ValidateUser(username, oldPassword); //verify password wasn't changed
                    Assert.IsTrue(result);
                }

                //---------------------------------------------------
                // change password (validate = false)
                //---------------------------------------------------
                username = TestUtils.Users[36].Username;
                oldPassword = TestUtils.Users[36].Password;
                newPassword = TestUtils.Users[36].Password + "new";
                _passwordCancel = true;
                _validateFired = false;
                _validateUsername = null;
                _validatePassword = null;
                try
                {
                    result = _provider.ChangePassword(username, oldPassword, newPassword);
                    Assert.Fail("Provided did not throw expected ProviderException when custom password validation requests cancel");
                }
                catch (ArgumentException)
                {
                  //  Assert.AreEqual<string>("The custom password validation failed.", ex.Message);
                    //ignore expected exception
                }
                Assert.IsTrue(_validateFired);
                Assert.AreEqual<string>(username, _validateUsername);
                Assert.AreEqual<string>(newPassword, _validatePassword);
                result = _provider.ValidateUser(username, oldPassword); // verify password didn't change
                Assert.IsTrue(result);


                //---------------------------------------------------
                // change password (validate = true)
                //---------------------------------------------------
                username = TestUtils.Users[36].Username;
                oldPassword = TestUtils.Users[36].Password;
                newPassword = TestUtils.Users[36].Password + "new";
                _passwordCancel = false;
                _validateFired = false;
                result = _provider.ChangePassword(username, oldPassword, newPassword);
                Assert.IsTrue(result);
                Assert.IsTrue(_validateFired);
                Assert.AreEqual<string>(username, _validateUsername);
                Assert.AreEqual<string>(newPassword, _validatePassword);

                //---------------------------------------------------
                // create user (validate = true)
                //---------------------------------------------------
                MembershipCreateStatus status;
                username = "TEST_USERTRIGGER";
                oldPassword = "TEST USERTRIGGER123!";
                _passwordCancel = false;
                _validateFired = false;
                _provider.CreateUser(username,  oldPassword,"TEST3@TEST.COM", "ANSWER TO LIFE?", "42", true, Guid.NewGuid(), out status);
                Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.Success, status);
                Assert.IsTrue(_validateFired);
                Assert.AreEqual<string>(username, _validateUsername);
                Assert.AreEqual<string>(oldPassword, _validatePassword);
                result = _provider.ValidateUser(username, oldPassword);  //verify account was created
                Assert.IsTrue(result);

                //---------------------------------------------------
                // create user (validate = false)
                //---------------------------------------------------
                status = MembershipCreateStatus.UserRejected; ;
                username = "TEST_USERTRIGGER2";
                oldPassword = "TEST USERTRIGGER123!";
                _passwordCancel = true;
                _validateFired = false;
                _provider.CreateUser(username, oldPassword, "TEST4@TEST.COM", "ANSWER TO LIFE?", "42", true, Guid.NewGuid(), out status);
                Assert.AreEqual<MembershipCreateStatus>(MembershipCreateStatus.InvalidPassword, status);
                Assert.IsTrue(_validateFired);
                Assert.AreEqual<string>(username, _validateUsername);
                Assert.AreEqual<string>(oldPassword, _validatePassword);
                result = _provider.ValidateUser(username, oldPassword);  //verify account was created
                Assert.IsFalse(result);

            }
            finally  //always remove event handler so it won't fire on other tests 
            {
                _provider.ValidatingPassword -= _provider_ValidatingPassword;
            }
        }

        void _provider_ValidatingPassword(object sender, ValidatePasswordEventArgs e)
        {
            _validateFired = true;
            _validatePassword = e.Password;
            _validateUsername = e.UserName;
            e.Cancel = _passwordCancel;
            
        }
    }
}
