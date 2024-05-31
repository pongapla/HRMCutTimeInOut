using System;
using System.DirectoryServices.AccountManagement;

namespace HRMCutTimeInOut
{
    public class ActiveDirectoryService
    {
        private readonly string _domain;
        private readonly string _username;
        private readonly string _password;

        public ActiveDirectoryService(string domain, string username, string password)
        {
            _domain = domain;
            _username = username;
            _password = password;
        }

        public bool IsUserValid(string username, string password)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domain, _username, _password))
                {
                    return context.ValidateCredentials(username, password);
                }
            }
            catch (Exception ex)
            {
                // Handle exception (e.g., log it)
                return false;
            }
        }
    }
}
