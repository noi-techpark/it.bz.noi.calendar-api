using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace it.bz.noi.calendar_api
{
    public class Settings
    {
        private readonly string serviceUri;
        private readonly string clientId;
        private readonly string tenantId;
        private readonly string clientSecret;
        private readonly string[] scopes;
        private readonly string authority;

        private static Lazy<Settings> settingsInstance = new Lazy<Settings>(() => new Settings());

        private Settings()
        {
            serviceUri = GetEnv("SERVICE_URL");
            clientId = GetEnv("CLIENT_ID")!;
            tenantId = GetEnv("TENANT_ID")!;
            clientSecret = GetEnv("CLIENT_SECRET")!;
            scopes = new[] { GetEnv("SERVICE_SCOPE")! };
            authority = GetEnv("OPENID_AUTHORITY")!;
        }

        private static string GetEnv(string key)
        {
            return Environment.GetEnvironmentVariable(key) ?? throw new Exception($"Environment variable {key} not set.");
        }

        public string ServiceUri => serviceUri;

        public string ClientId => clientId;

        public string TenantId => tenantId;

        public string ClientSecret => clientSecret;

        public string[] Scopes => scopes;

        public string OpenIdAuthority => authority;

        public static Settings Initialize()
        {
            return settingsInstance.Value;
        }
    }
}
