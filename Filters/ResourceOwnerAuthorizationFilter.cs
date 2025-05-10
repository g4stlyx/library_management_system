using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MvcExample.Utils;

namespace MvcExample.Filters
{
    public class ResourceOwnerAuthorizationFilter : IAsyncActionFilter
    {
        private readonly string _userIdParameterName;
        private readonly bool _allowAdmin;
        
        /// <summary>
        /// Creates a filter that ensures the current user owns the resource or is an admin (if allowAdmin is true)
        /// </summary>
        /// <param name="userIdParameterName">Name of the route or query parameter that contains the resource owner's ID</param>
        /// <param name="allowAdmin">Whether to allow admin users to access the resource regardless of ownership</param>
        public ResourceOwnerAuthorizationFilter(string userIdParameterName = "id", bool allowAdmin = true)
        {
            _userIdParameterName = userIdParameterName;
            _allowAdmin = allowAdmin;
        }
        
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var controller = context.Controller as Controller;
            var user = context.HttpContext.User;
            
            // Allow admins if specified
            if (_allowAdmin && user.IsInRole("Admin"))
            {
                await next();
                return;
            }
            
            // Get the user ID from claims
            if (!AuthUtils.TryGetUserId(user, out int currentUserId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            
            // Get the resource owner ID from route or query parameters
            if (!context.ActionArguments.TryGetValue(_userIdParameterName, out var ownerIdObj) || ownerIdObj == null)
            {
                context.Result = new BadRequestResult();
                return;
            }
            
            // Try to convert the resource owner ID to an integer
            int ownerId;
            if (ownerIdObj is int intOwnerId)
            {
                ownerId = intOwnerId;
            }
            else if (int.TryParse(ownerIdObj.ToString(), out int parsedOwnerId))
            {
                ownerId = parsedOwnerId;
            }
            else
            {
                context.Result = new BadRequestResult();
                return;
            }
            
            // Check if the current user is the owner
            if (currentUserId != ownerId)
            {
                if (controller != null)
                {
                    controller.TempData["ErrorMessage"] = "You don't have permission to access this resource.";
                }
                context.Result = new ForbidResult();
                return;
            }
            
            await next();
        }
    }
}
