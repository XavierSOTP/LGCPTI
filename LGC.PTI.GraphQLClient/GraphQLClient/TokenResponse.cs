namespace LGC.PTI.Infrastructure.GraphQLClient
{
    public class TokenReponse
    {
        public LoginData Login { get; set; }

        public class LoginData
        {
            public string Token { get; set; }
        }
    }
}
