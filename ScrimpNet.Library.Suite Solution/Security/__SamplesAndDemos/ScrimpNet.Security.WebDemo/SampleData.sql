SET NOCOUNT ON

-- ==================================================================
--	Set this variable to be some unique application name. 
--
--	NOTE:  This name MUST match resolved application name in
--		   Membership provider on the WCF host project
-- ==================================================================
declare @applicationName as varchar(100) = 'WcfDemo'

-- see if application already exists or get a new id
declare @applicationId as uniqueidentifier
select @applicationId = ApplicationId from aspnet_applications where applicationName = @applicationName
if @applicationId is null
begin
	set @applicationId = newid()
end

Print 'Purging records for: '+@applicationName
delete aspnet_UsersInRoles where userid in (select UserId from aspnet_Users where ApplicationId = @applicationId)
delete aspnet_Membership where ApplicationId = @applicationId

delete dbo.aspnet_Roles where ApplicationId = @applicationId
delete dbo.aspnet_Profile where UserId in (select UserId from aspnet_Users where ApplicationId = @applicationid)
delete dbo.aspnet_PersonalizationPerUser where UserId in (select UserId from aspnet_Users where ApplicationId = @applicationId)

delete dbo.aspnet_Users where ApplicationId = @applicationId

delete dbo.aspnet_PersonalizationAllUsers
delete aspnet_Paths where  ApplicationId = @applicationId
delete aspnet_Applications where applicationid = @applicationId

Print 'Creating Application '''+@applicationName+''''
Insert Into aspnet_applications (ApplicationName, LoweredApplicationName, ApplicationId, [Description])
	Values (@applicationName, lower(@applicationName), @applicationId, @ApplicationName + 'Default Records')
	
Print 'Creating Roles'
DECLARE @role_adminId uniqueidentifier = newid()
DECLARE @role_userId uniqueidentifier = newid()
DECLARE @role_powerUserId uniqueidentifier = newid()
Print '...Admin'
INSERT INTO aspnet_Roles (ApplicationId, RoleId, RoleName, LoweredRoleName,[Description])
	VALUES	(@ApplicationId, @role_adminId, 'Admin','admin','Role: Admin');
Print '...User'
INSERT INTO aspnet_Roles (ApplicationId, RoleId, RoleName, LoweredRoleName,[Description])
	VALUES	(@ApplicationId, @role_userId, 'User','user','Role: User');
Print '...PowerUser'	
INSERT INTO aspnet_Roles (ApplicationId, RoleId, RoleName, LoweredRoleName,[Description])
	VALUES	(@ApplicationId, @role_powerUserId, 'PowerUser','poweruser','Role: Power User');
	
Print 'Creating Users'
DECLARE @username as nvarchar(256) = 'S2'
DECLARE @userId_S2 uniqueidentifier = newid()
DECLARE @userId_Mary uniqueidentifier = newid()
DECLARE @userId_Sam uniqueidentifier = newid()
DECLARE @userId_Jane uniqueidentifier = newid()
DECLARE @userId_Harry uniqueidentifier = newid()

INSERT INTO dbo.aspnet_Users (ApplicationId, UserId, UserName, LoweredUserName, MobileAlias, IsAnonymous, LastActivityDate)
	VALUES (@ApplicationId, @userid_S2, 'S2', lower('S2'),'Mobile: '+'S2',0, '12/31/2010')
INSERT INTO dbo.aspnet_Users (ApplicationId, UserId, UserName, LoweredUserName, MobileAlias, IsAnonymous, LastActivityDate)
	VALUES (@ApplicationId, @userid_Mary, 'Mary', lower('Mary'),'Mobile: '+'Mary',0, '12/31/2010')
INSERT INTO dbo.aspnet_Users (ApplicationId, UserId, UserName, LoweredUserName, MobileAlias, IsAnonymous, LastActivityDate)
	VALUES (@ApplicationId, @userid_Sam, 'Sam', lower('Sam'),'Mobile: '+'Sam',0, '12/31/2010')
INSERT INTO dbo.aspnet_Users (ApplicationId, UserId, UserName, LoweredUserName, MobileAlias, IsAnonymous, LastActivityDate)
	VALUES (@ApplicationId, @userid_Jane, 'Jane', lower('Jane'),'Mobile: '+'Jane',0, '12/31/2010')
INSERT INTO dbo.aspnet_Users (ApplicationId, UserId, UserName, LoweredUserName, MobileAlias, IsAnonymous, LastActivityDate)
	VALUES (@ApplicationId, @userid_Harry, 'Harry', lower('Harry'),'Mobile: '+'Harry',0, '12/31/2010')

declare @userid uniqueidentifier	
set @username = 'S2'
set @userid = @userid_s2;
Print '...Setting password for: '+@username+'(pwd: '+lower(@UserName)+'!)'
INSERT INTO [ScrimpNet].[dbo].[aspnet_Membership]
           ([ApplicationId]
           ,[UserId]
           ,[Password]
           ,[PasswordFormat]
           ,[PasswordSalt]
           ,[MobilePIN]
           ,[Email]
           ,[LoweredEmail]
           ,[PasswordQuestion]
           ,[PasswordAnswer]
           ,[IsApproved]
           ,[IsLockedOut]
           ,[CreateDate]
           ,[LastLoginDate]
           ,[LastPasswordChangedDate]
           ,[LastLockoutDate]
           ,[FailedPasswordAttemptCount]
           ,[FailedPasswordAttemptWindowStart]
           ,[FailedPasswordAnswerAttemptCount]
           ,[FailedPasswordAnswerAttemptWindowStart]
           ,[Comment])
     VALUES
           (@applicationId
           ,@userid
           ,lower(@username)+'!'
           ,0
           ,''
           ,'Pin:'+@username
           ,@username+'@m.com'
           ,lower(@username)+'@m.com'
           ,'What is my login name?'
           ,@username
           ,1
           ,0
           ,getdate()
           ,getdate()
           ,getdate()
           ,'12/31/2001'
           ,0
           ,getdate()
           ,0
           ,getdate()
           ,'AutoGenerated for '+@applicationName)

set @username = 'Mary'
set @userid = @userid_Mary;

Print '...Setting password for: '+@username+'(pwd: '+lower(@UserName)+'!)'
INSERT INTO [ScrimpNet].[dbo].[aspnet_Membership]
           ([ApplicationId]
           ,[UserId]
           ,[Password]
           ,[PasswordFormat]
           ,[PasswordSalt]
           ,[MobilePIN]
           ,[Email]
           ,[LoweredEmail]
           ,[PasswordQuestion]
           ,[PasswordAnswer]
           ,[IsApproved]
           ,[IsLockedOut]
           ,[CreateDate]
           ,[LastLoginDate]
           ,[LastPasswordChangedDate]
           ,[LastLockoutDate]
           ,[FailedPasswordAttemptCount]
           ,[FailedPasswordAttemptWindowStart]
           ,[FailedPasswordAnswerAttemptCount]
           ,[FailedPasswordAnswerAttemptWindowStart]
           ,[Comment])
     VALUES
           (@applicationId
           ,@userid
           ,lower(@username)+'!'
           ,0
           ,''
           ,'Pin:'+@username
           ,@username+'@m.com'
           ,lower(@username)+'@m.com'
           ,'What is my login name?'
           ,@username
           ,1
           ,0
           ,getdate()
           ,getdate()
           ,getdate()
           ,'12/31/2001'
           ,0
           ,getdate()
           ,0
           ,getdate()
           ,'AutoGenerated for '+@applicationName)
           

set @username = 'Jane'
set @userid = @userid_Jane;

Print '...Setting password for: '+@username+'(pwd: '+lower(@UserName)+'!)'
INSERT INTO [ScrimpNet].[dbo].[aspnet_Membership]
           ([ApplicationId]
           ,[UserId]
           ,[Password]
           ,[PasswordFormat]
           ,[PasswordSalt]
           ,[MobilePIN]
           ,[Email]
           ,[LoweredEmail]
           ,[PasswordQuestion]
           ,[PasswordAnswer]
           ,[IsApproved]
           ,[IsLockedOut]
           ,[CreateDate]
           ,[LastLoginDate]
           ,[LastPasswordChangedDate]
           ,[LastLockoutDate]
           ,[FailedPasswordAttemptCount]
           ,[FailedPasswordAttemptWindowStart]
           ,[FailedPasswordAnswerAttemptCount]
           ,[FailedPasswordAnswerAttemptWindowStart]
           ,[Comment])
     VALUES
           (@applicationId
           ,@userid
           ,lower(@username)+'!'
           ,0
           ,''
           ,'Pin:'+@username
           ,@username+'@m.com'
           ,lower(@username)+'@m.com'
           ,'What is my login name?'
           ,@username
           ,1
           ,0
           ,getdate()
           ,getdate()
           ,getdate()
           ,'12/31/2001'
           ,0
           ,getdate()
           ,0
           ,getdate()
           ,'AutoGenerated for '+@applicationName)           
           

set @username = 'Harry'
set @userid = @userid_Harry;

Print '...Setting password for: '+@username+'(pwd: '+lower(@UserName)+'!)'
INSERT INTO [ScrimpNet].[dbo].[aspnet_Membership]
           ([ApplicationId]
           ,[UserId]
           ,[Password]
           ,[PasswordFormat]
           ,[PasswordSalt]
           ,[MobilePIN]
           ,[Email]
           ,[LoweredEmail]
           ,[PasswordQuestion]
           ,[PasswordAnswer]
           ,[IsApproved]
           ,[IsLockedOut]
           ,[CreateDate]
           ,[LastLoginDate]
           ,[LastPasswordChangedDate]
           ,[LastLockoutDate]
           ,[FailedPasswordAttemptCount]
           ,[FailedPasswordAttemptWindowStart]
           ,[FailedPasswordAnswerAttemptCount]
           ,[FailedPasswordAnswerAttemptWindowStart]
           ,[Comment])
     VALUES
           (@applicationId
           ,@userid
           ,lower(@username)+'!'
           ,0
           ,''
           ,'Pin:'+@username
           ,@username+'@m.com'
           ,lower(@username)+'@m.com'
           ,'What is my login name?'
           ,@username
           ,1
           ,0
           ,getdate()
           ,getdate()
           ,getdate()
           ,'12/31/2001'
           ,0
           ,getdate()
           ,0
           ,getdate()
           ,'AutoGenerated for '+@applicationName)   
           
set @username = 'Sam'
set @userid = @userid_Sam;
Print '...Setting password for: '+@username+'(pwd: '+lower(@UserName)+'!)'
INSERT INTO [ScrimpNet].[dbo].[aspnet_Membership]
           ([ApplicationId]
           ,[UserId]
           ,[Password]
           ,[PasswordFormat]
           ,[PasswordSalt]
           ,[MobilePIN]
           ,[Email]
           ,[LoweredEmail]
           ,[PasswordQuestion]
           ,[PasswordAnswer]
           ,[IsApproved]
           ,[IsLockedOut]
           ,[CreateDate]
           ,[LastLoginDate]
           ,[LastPasswordChangedDate]
           ,[LastLockoutDate]
           ,[FailedPasswordAttemptCount]
           ,[FailedPasswordAttemptWindowStart]
           ,[FailedPasswordAnswerAttemptCount]
           ,[FailedPasswordAnswerAttemptWindowStart]
           ,[Comment])
     VALUES
           (@applicationId
           ,@userid
           ,lower(@username)+'!'
           ,0
           ,''
           ,'Pin:'+@username
           ,@username+'@m.com'
           ,lower(@username)+'@m.com'
           ,'What is my login name?'
           ,@username
           ,1
           ,0
           ,getdate()
           ,getdate()
           ,getdate()
           ,'12/31/2001'
           ,0
           ,getdate()
           ,0
           ,getdate()
           ,'AutoGenerated for '+@applicationName)          
           
Print 'Placing Users Into Roles'
DECLARE @RoleName as nvarchar(255)
DECLARE @RoleId as UniqueIdentifier
Set @RoleName = 'Admin'
Set @RoleId = @role_adminid

Print '...Role: Admin'
Print '.....S2'
INSERT INTO aspnet_UsersInRoles (RoleId, UserId)
	VALUES (@RoleId,@UserId_s2)

set @roleId = @role_poweruserid	
Print '...Role: PowerUsers'
Print '.....S2'
INSERT INTO aspnet_UsersInRoles (RoleId, UserId)
	VALUES (@RoleId,@UserId_s2)	
Print '.....Sam'	
INSERT INTO aspnet_UsersInRoles (RoleId, UserId)
	VALUES (@RoleId,@UserId_Sam)		
Print '.....Jane'	
INSERT INTO aspnet_UsersInRoles (RoleId, UserId)
	VALUES (@RoleId,@UserId_Jane)			

Set @RoleId = @role_userid	
Print '...Role: Users'
Print '.....S2'
INSERT INTO aspnet_UsersInRoles (RoleId, UserId)
	VALUES (@RoleId,@UserId_s2)	
Print '.....Sam'	
INSERT INTO aspnet_UsersInRoles (RoleId, UserId)
	VALUES (@RoleId,@UserId_Sam)		
Print '.....Jane'	
INSERT INTO aspnet_UsersInRoles (RoleId, UserId)
	VALUES (@RoleId,@UserId_Jane)				
Print '.....Mary'	
INSERT INTO aspnet_UsersInRoles (RoleId, UserId)
	VALUES (@RoleId,@UserId_Mary)		
Print '.....Harry'	
INSERT INTO aspnet_UsersInRoles (RoleId, UserId)
	VALUES (@RoleId,@UserId_Harry)	