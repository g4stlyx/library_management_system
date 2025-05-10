using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace MvcExample.Utils
{
    public static class AuthUtils
    {
        // Check if the current user is the owner of a resource or an admin
        public static bool IsOwnerOrAdmin(ClaimsPrincipal user, int ownerId)
        {
            if (user == null)
                return false;
                
            // Check if user is admin
            if (user.IsInRole("Admin"))
                return true;
                
            // Check if user is the owner
            var userIdClaim = user.Claims.FirstOrDefault(c => c.Type == "userId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId == ownerId;
            }
            
            return false;
        }
        
        // Get the current user's ID from claims
        public static bool TryGetUserId(ClaimsPrincipal user, out int userId)
        {
            userId = 0;
            var userIdClaim = user.Claims.FirstOrDefault(c => c.Type == "userId");
            
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedUserId))
            {
                userId = parsedUserId;
                return true;
            }
            
            return false;
        }
        
        // Return unauthorized if user doesn't have access, otherwise null
        public static IActionResult EnsureUserAccess(Controller controller, ClaimsPrincipal user, int resourceOwnerId)
        {
            if (!IsOwnerOrAdmin(user, resourceOwnerId))
            {
                controller.TempData["ErrorMessage"] = "You don't have permission to access this resource.";
                return controller.RedirectToAction("Index", "Home");
            }
            
            return null;
        }
    }
}
