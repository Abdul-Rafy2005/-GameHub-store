using System;
using System.Web;
using System.Web.Mvc;

namespace GameHub.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null)
            {
                return false;
            }

            var session = httpContext.Session;
            if (session == null)
            {
                return false;
            }

            // Check if authenticated
            var isAuth = session["IsAuthenticated"] as bool?;
            if (isAuth != true)
            {
                return false;
            }

            // Check if admin
            var userType = session["UserType"] as string;
            return string.Equals(userType, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var httpContext = filterContext.HttpContext;
            var session = httpContext?.Session;
            
            // Check if user is logged in
            var isAuth = session?["IsAuthenticated"] as bool?;
            
            if (isAuth != true)
            {
                // Not logged in - redirect to login with return URL
                filterContext.Controller.TempData["LoginError"] = "Please sign in to access this page.";
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Account" },
                        { "action", "Login" },
                        { "returnUrl", httpContext.Request.RawUrl }
                    });
            }
            else
            {
                // Logged in but not admin - show access denied page
                filterContext.Controller.TempData["ToastError"] = "Admin access required. You do not have permission to access this page.";
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Games" },
                        { "action", "Index" }
                    });
            }
        }
    }
}
