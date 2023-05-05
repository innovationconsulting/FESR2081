namespace ICWebApp.Classes.MyCivis
{
    public class AuthenticatedUser : System.Security.Principal.IIdentity
    {
        public AuthenticatedUser(string authenticationType, bool isAuthenticated, string name)
        {
            AuthenticationType = authenticationType;
            IsAuthenticated = isAuthenticated;
            Name = name;
        }
        public string AuthenticationType { get; }
        public bool IsAuthenticated { get; }
        public string Name { get; }
    }
}
