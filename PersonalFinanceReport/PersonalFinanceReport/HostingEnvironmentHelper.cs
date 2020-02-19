using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PersonalFinanceReport
{
    public static class HostingEnvironmentHelper
    {
        public static class HostingEnvironmentDefaults
        {
            // default names
            public const string Development = "Development";
            public const string Staging = "Staging";
            public const string Production = "Production";

            // custom names
            public const string DevelopmentLocalhost = "DevelopmentLocalhost";
        }

        public static string Environment
        {
            get
            {
                // for web projects
                string env = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                // for console projects
                if (String.IsNullOrEmpty(env))
                {
                    env = System.Environment.GetEnvironmentVariable("Environment");
                }

                if (String.IsNullOrEmpty(env))
                {
                    // if we get here then Environment is possibly set in hostsettings.json and can't be accessed
                    // only using 'hostingContext.HostingEnvironment.EnvironmentName', so it is not in
                    // 'Environment' env variable
                    throw new Exception("Neither ASPNETCORE_ENVIRONMENT nor Environment is set! Recheck startup configuration.");
                }

                return env;
            }
        }

        public static bool IsDevelopmentLocalhost(string environment)
        {
            return environment == HostingEnvironmentDefaults.DevelopmentLocalhost;
        }

        public static bool IsDevelopmentLocalhost()
        {
            return Environment == HostingEnvironmentDefaults.DevelopmentLocalhost;
        }

        /// <summary>
        /// Returns true if current environment is Development or any custom type like DevelopmentLocalhost, DevelopmentDocker, Development[anything]
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        public static bool IsDevelopmentAny(string environment)
        {
            var regex = new Regex(@"^Development[a-zA-Z0-9_-]{0,}$");
            return regex.IsMatch(environment);
        }

        public static bool IsDevelopmentAny()
        {
            return IsDevelopmentAny(Environment);
        }

        public static bool IsTestingAny(string environment)
        {
            var regex = new Regex(@"^Testing[a-zA-Z0-9_-]{0,}$");
            return regex.IsMatch(environment);
        }

        public static bool IsTestingAny()
        {
            return IsTestingAny(Environment);
        }

        public static bool IsProductionAny(string environment)
        {
            var regex = new Regex(@"^Production[a-zA-Z0-9_-]{0,}$");
            return regex.IsMatch(environment);
        }

        public static bool IsProductionAny()
        {
            return IsProductionAny(Environment);
        }
    }
}
