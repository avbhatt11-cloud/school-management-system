using System;
using System.Web;
using System.Web.Mvc;

namespace SchoolManagementSystem.Filters
{
    /// <summary>
    /// Custom Authorization Attribute to protect controllers/actions by role
    /// Usage: [RoleAuthorize(Roles = "Admin")] or [RoleAuthorize(Roles = "Admin,Staff")]
    /// </summary>
    public class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext.Session["Role"] == null)
            {
                return false;
            }

            string userRole = httpContext.Session["Role"].ToString();
            string loginType = httpContext.Session["LoginType"]?.ToString();

            // Check if user's role matches any of the allowed roles
            if (!string.IsNullOrEmpty(Roles))
            {
                string[] allowedRoles = Roles.Split(',');
                foreach (string role in allowedRoles)
                {
                    if (userRole.Trim().Equals(role.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        // Extra security: Verify LoginType matches Role
                        if (loginType != null && loginType.Equals(userRole, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.Session["Role"] == null)
            {
                // Not logged in - redirect to home
                filterContext.Result = new RedirectResult("~/Home/Index");
            }
            else
            {
                // Logged in but wrong role - show access denied
                filterContext.Result = new RedirectResult("~/Error/AccessDenied");
            }
        }
    }
}
