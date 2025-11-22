using System;
using Microsoft.Owin;
using Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security; // for AuthenticationMode

[assembly: OwinStartup(typeof(GameHub.Startup))]
namespace GameHub
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Primary application cookie (Session-based app still uses ASP.NET Session, this is optional)
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "ApplicationCookie",
                LoginPath = new PathString("/Account/Login"),
                CookieHttpOnly = true,
                ExpireTimeSpan = TimeSpan.FromHours(12),
                SlidingExpiration = true
            });

            // Passive external sign?in cookie (replacement for UseExternalSignInCookie)
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "ExternalCookie",
                AuthenticationMode = AuthenticationMode.Passive,
                CookieHttpOnly = true
            });

            var clientId = System.Configuration.ConfigurationManager.AppSettings["GoogleClientId"];
            var clientSecret = System.Configuration.ConfigurationManager.AppSettings["GoogleClientSecret"];
            if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret))
            {
                var googleOptions = new GoogleOAuth2AuthenticationOptions
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    CallbackPath = new PathString("/signin-google"),
                    SignInAsAuthenticationType = "ExternalCookie",
                    Provider = new GoogleOAuth2AuthenticationProvider
                    {
                        OnApplyRedirect = ctx =>
                        {
                            var uri = ctx.RedirectUri;
                            // Force Google account chooser each time
                            if (!uri.Contains("prompt="))
                            {
                                uri += (uri.Contains("?") ? "&" : "?") + "prompt=select_account";
                            }
                            ctx.Response.Redirect(uri);
                        }
                    }
                };
                app.UseGoogleAuthentication(googleOptions);
            }
        }
    }
}
