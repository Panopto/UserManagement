/// <summary>
/// Sample C# that uses the Panopto PublicAPI
/// This sample shows how to creates an user, a group and grant access to a folder with the Panopto PublicAPI
/// </summary>
namespace UserManagementExample
{
    using PanoptoAccessManagement;
    using PanoptoUserManagement;
    using PanoptoSessionManagement;
    using System;
    public class Program
    {
        static void Main(string[] args)
        {
            // Set Panopto server authentication details
            string userKey = "user";
            string password = "password";

            // Set the new group name
            string newGroup = "MyGroupName";

            // The Id of the Panopto folder to the new user will be granted access
            string folderName = "MyFolderName";
            
            // The id of a Panopto external provider
            string externalProviderId = "PanoptoExternalProviderId";

            // The id of the group used by the external provider
            string externalFolderId = "externalFolderId";
            
            // Permissions for user management
            PanoptoUserManagement.AuthenticationInfo userAuth = new PanoptoUserManagement.AuthenticationInfo()
            {
                UserKey = userKey,
                Password = password
            };

            // Permissions for access management
            PanoptoAccessManagement.AuthenticationInfo accessAuthInfo = new PanoptoAccessManagement.AuthenticationInfo()
            {
                UserKey = userKey,
                Password = password
            };

            // Permissions for session management
            PanoptoSessionManagement.AuthenticationInfo sessionAuthInfo = new PanoptoSessionManagement.AuthenticationInfo()
            {
                UserKey = userKey,
                Password = password
            };

            Guid panoptoFolderId = Guid.Empty;

            if (!String.IsNullOrWhiteSpace(folderName))
            {
                panoptoFolderId = CreateFolder(sessionAuthInfo, folderName);
            }

            if (panoptoFolderId != Guid.Empty)
            {
                // Creates a new user
                Guid idUser = CreateUser(userAuth, "UserKey", "UserFirstName", "UserLastName", "user@mail.com");

                Group group = null;

                if (idUser != Guid.Empty)
                {
                    // Creates group and adds user
                    group = CreateAndAddUserToGroup(userAuth, newGroup, idUser, externalProviderId, externalFolderId);
                }

                // Sets creator privilege to the created group over the folder
                if (group != null && group.Id != Guid.Empty)
                {
                    SetPrivilegesForFolder(accessAuthInfo, panoptoFolderId, group.Id, AccessRole.Creator);
                }
            }
            else
            {
                Console.WriteLine("Unable to create a folder with folder name: " + folderName);
            }

            Console.ReadKey();
        }

        private static Guid CreateFolder(PanoptoSessionManagement.AuthenticationInfo auth, string folderName)
        {
            Guid folderId = Guid.Empty;
            ISessionManagement sessionMgmt = new SessionManagementClient();

            try
            {
                Folder folder = sessionMgmt.AddFolder(auth, folderName, null, false);
                folderId = folder.Id;
                Console.WriteLine("Created a folder with folder name: " + folderName);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to create a folder with folder name: " + folderName);
                Console.WriteLine(e.Message);
            }

            return folderId;
        }

        /// <summary>
        /// Creates a new user in Panopto server.
        /// </summary>
        /// <param name="userAuth">Authentification info.</param>
        /// <param name="userKey">User name</param>
        /// <param name="firstName">User first name.</param>
        /// <param name="lastName">User last name.</param>
        /// <param name="email">User email.</param>
        /// <returns>Guid associated to the created user.</returns>
        public static Guid CreateUser(PanoptoUserManagement.AuthenticationInfo userAuth, string userKey, string firstName, string lastName, string email)
        {
            Guid userID = Guid.Empty;
            IUserManagement userMgr = new UserManagementClient();
            
            try
            {   
                // Tries to get the user by key
                User panUser = userMgr.GetUserByKey(userAuth, userKey);
                
                //Checks if the user exist
                if (panUser != null && !panUser.UserId.Equals(Guid.Empty))
                {
                    userID = panUser.UserId;
                    Console.WriteLine("The user {0} already exists", userID.ToString());
                }
                else
                {
                    // Checks new user data
                    if (String.IsNullOrEmpty(firstName) || String.IsNullOrEmpty(lastName) ||
                        String.IsNullOrEmpty(email))
                    {
                        Console.WriteLine("{0} doesn't have enough details to make a user account", userKey);
                    }
                    else
                    {
                        // Creates a new user
                        panUser = new User()
                        {
                            UserKey = userKey,
                            FirstName = firstName,
                            LastName = lastName,
                            SystemRole = UserManagementExample.PanoptoUserManagement.SystemRole.None,
                            UserBio = String.Empty,
                            Email = email,
                            EmailSessionNotifications = false
                        };

                        // Creates the user in Panopto Server
                        userID = userMgr.CreateUser(userAuth, panUser, String.Empty);

                        Console.WriteLine("The user {0} was created", userID.ToString());
                        Console.WriteLine(" First name: {0}", firstName);
                        Console.WriteLine(" Last name: {0}", lastName);
                        Console.WriteLine(" Email: {0}", email);
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return userID;
        }

        /// <summary>
        /// Adds a user to a group. If the group doesn't exists, the method creates it and the user is added to the group.
        /// </summary>
        /// <param name="userAuth">Authentification info.</param>
        /// <param name="panAdGroupName">Name of the Panopto group.</param>
        /// <param name="userId">The Guid of the Panopto user that will be added to the group.</param>
        /// <param name="externalProviderId">The external provider the group is associated with.</param>
        /// <param name="externalFolderId">The id of the group used by the external provider.</param>
        /// <returns>Panopto Group with the user added.</returns>
        public static Group CreateAndAddUserToGroup(PanoptoUserManagement.AuthenticationInfo userAuth, string panAdGroupName, Guid userId, string externalProviderId, string externalFolderId)
        {
            IUserManagement userMgr = new UserManagementClient();
            Group[] panGroupArray = userMgr.GetGroupsByName(userAuth, panAdGroupName);
            Group panGroup = new Group();
            User[] newUser = userMgr.GetUsers(userAuth, new Guid[] {userId});
            try
            {
                // if the group doesn't exist
                if (panGroupArray.Length == 0)
                {
                    // Creates external group
                    panGroup = userMgr.CreateInternalGroup(userAuth, panAdGroupName, new Guid[] { userId });
                    Console.WriteLine("The group {0} was created", panAdGroupName);
                }
                else
                {
                    Console.WriteLine("The group {0} already exists", panGroup.Id.ToString());
                    panGroup = panGroupArray[0];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return panGroup;
        }

        /// <summary>
        /// Sets an access role privilege to a group for an specific folder.
        /// </summary>
        /// <param name="accessAuthInfo">Authentification info.</param>
        /// <param name="folderId">Folder Guid.</param>
        /// <param name="groupId">Group Guid.</param>
        /// <param name="accessRole">Access role type.</param>
        public static void SetPrivilegesForFolder(PanoptoAccessManagement.AuthenticationInfo accessAuthInfo, Guid folderId, Guid groupId, AccessRole accessRole)
        {
            IAccessManagement accessMgr = new AccessManagementClient();

            Console.WriteLine("Adding group {0} as {1} of folder {2}", groupId, accessRole.ToString(), folderId);
            try
            {
                // Grant access to the folder
                accessMgr.GrantGroupAccessToFolder(accessAuthInfo, folderId, groupId, accessRole);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
