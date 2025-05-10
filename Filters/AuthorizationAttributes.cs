using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvcExample.Filters;

namespace MvcExample.Filters
{
    /// <summary>
    /// Authorization attribute that ensures the current user owns the resource or is an admin
    /// </summary>
    public class OwnerOrAdminAttribute : TypeFilterAttribute
    {
        public OwnerOrAdminAttribute(string userIdParameterName = "id", bool allowAdmin = true)
            : base(typeof(ResourceOwnerAuthorizationFilter))
        {
            Arguments = new object[] { userIdParameterName, allowAdmin };
        }
    }
    
    /// <summary>
    /// Authorization attribute that ensures only the current user has access to the resource (admins excluded)
    /// </summary>
    public class OwnerOnlyAttribute : TypeFilterAttribute
    {
        public OwnerOnlyAttribute(string userIdParameterName = "id")
            : base(typeof(ResourceOwnerAuthorizationFilter))
        {
            Arguments = new object[] { userIdParameterName, false };
        }
    }
}
