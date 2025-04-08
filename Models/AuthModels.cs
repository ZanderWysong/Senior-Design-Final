// Models/AuthModels.cs
using System.ComponentModel.DataAnnotations;

namespace ZitaDataSystem.Models
{

    public class SaveFlowRequest
    {
        public List<NodeData> Nodes { get; set; }
        public List<EdgeData> Edges { get; set; }
    }

    public class NodeData
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public object Data { get; set; }
        public object Position { get; set; }
    }

    public class EdgeData
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public string SourceHandle { get; set; }
        public string TargetHandle { get; set; }
    }

    public class CreateProjectRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? AssignedTo { get; set; }
        public string OrganizationId { get; set; }
    }
    public class UserProfileResponse
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string Name { get; set; }
    }
     public class RegisterRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; }
        // For administrators:
        public string? OrganizationName { get; set; }
        // For developers/members:
        public string? OrganizationId { get; set; }

        // Developer can type this code at sign-up
        public string? AccessCode { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class AuthResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string? OrgCode { get; set; }
    }

    public class OrgCodeValidationRequest
    {
        public string OrgCode { get; set; } = null!;
    }
        // New models for dynamic endpoints
    public class DynamicEndpoint
    {
        public int Id { get; set; }
        public string EndpointName { get; set; }
        public string Method { get; set; }
        public string ApiKey { get; set; }
        public string SqlCommand { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateDynamicEndpointRequest
    {
        public string EndpointName { get; set; }
        public string Method { get; set; }
        public string ApiKey { get; set; }
        public string SqlCommand { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
