using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using ZitaDataSystem.Models;
using Microsoft.Extensions.Configuration;
using ZitaDataSystem.Services;
using BCrypt.Net;

namespace ZitaDataSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly AuthService _authService;

        public AuthController(IConfiguration configuration, AuthService authService)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("PrimaryDatabase")
                ?? throw new InvalidOperationException("PrimaryDatabase connection string is missing.");
            _authService = authService;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Role))
            {
                return BadRequest("Missing required fields (name, email, password, or role).");
            }

            try
            {
                string normalizedRole = request.Role.Trim().ToLower();

                if (normalizedRole == "administrator")
                {
                    if (string.IsNullOrWhiteSpace(request.OrganizationName))
                        request.OrganizationName = "Org_" + Guid.NewGuid().ToString("N").Substring(0, 6);

                    if (string.IsNullOrWhiteSpace(request.OrganizationId))
                        request.OrganizationId = "org_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                }
                else if (normalizedRole == "developer")
                {
                    // Developers must provide an AccessCode to join an existing organization.
                    if (string.IsNullOrWhiteSpace(request.AccessCode))
                        return BadRequest("Developer must provide an Access Code.");

                    // Look up the access code in the companycodes table to get the OrganizationId.
                    using (var conn = new NpgsqlConnection(_connectionString))
                    {
                        conn.Open();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
                                SELECT organizationid
                                FROM companycodes
                                WHERE code = @Code
                                LIMIT 1;
                            ";
                            cmd.Parameters.AddWithValue("@Code", request.AccessCode.Trim());

                            var result = cmd.ExecuteScalar();
                            if (result == null)
                                return BadRequest("Invalid Access Code.");
                            
                            // Use the OrganizationId that corresponds to the access code.
                            request.OrganizationId = result.ToString();
                        }
                    }
                }
                // You can add more role-specific logic here if needed.

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                string newUserId = Guid.NewGuid().ToString();

                using var conn2 = new NpgsqlConnection(_connectionString);
                conn2.Open();
                using var cmd2 = conn2.CreateCommand();
                cmd2.CommandText = @"
                    INSERT INTO users 
                    (id, name, email, password, role, organizationid, organizationname)
                    VALUES 
                    (@Id, @Name, @Email, @Password, @Role, @OrgId, @OrgName)
                    RETURNING id;
                ";
                cmd2.Parameters.AddWithValue("@Id", newUserId);
                cmd2.Parameters.AddWithValue("@Name", request.Name.Trim());
                cmd2.Parameters.AddWithValue("@Email", request.Email.Trim().ToLower());
                cmd2.Parameters.AddWithValue("@Password", passwordHash);
                cmd2.Parameters.AddWithValue("@Role", normalizedRole);
                cmd2.Parameters.AddWithValue("@OrgId", (object?)request.OrganizationId ?? DBNull.Value);
                cmd2.Parameters.AddWithValue("@OrgName", (object?)request.OrganizationName ?? DBNull.Value);

                string insertedId = (string)cmd2.ExecuteScalar();
                return Ok(new
                {
                    UserId = insertedId,
                    Email = request.Email,
                    Role = request.Role,
                    OrganizationId = request.OrganizationId,
                    OrganizationName = request.OrganizationName,
                    Message = "Account created successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error creating account: " + ex.Message);
            }
        }


        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Missing email or password");

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id, password, role, organizationid, organizationname FROM users WHERE email = @Email";
                cmd.Parameters.AddWithValue("@Email", request.Email.Trim().ToLower());

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string userId = reader.GetString(0);
                    string storedPassword = reader.GetString(1);
                    string role = reader.GetString(2);
                    string organizationId = reader.IsDBNull(3) ? null : reader.GetString(3);
                    string organizationName = reader.IsDBNull(4) ? null : reader.GetString(4);

                    if (BCrypt.Net.BCrypt.Verify(request.Password, storedPassword))
                    {
                        string token = _authService.GenerateToken(userId, role);
                        return Ok(new
                        {
                            userId,
                            email = request.Email,
                            role,
                            token,
                            organizationId,
                            organizationName,
                            message = "Login successful."
                        });
                    }

                    return Unauthorized("Invalid password.");
                }

                return NotFound("User not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
        }

        [HttpPost("change-password")]
        public IActionResult ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest("Missing current or new password.");

            string userEmail = Request.Query["email"];
            if (string.IsNullOrWhiteSpace(userEmail))
                return BadRequest("Missing user email in query params.");

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();

                string userId = null;
                string storedPasswordHash = null;

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT id, password FROM users WHERE email = @Email";
                    cmd.Parameters.AddWithValue("@Email", userEmail.ToLower());

                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        userId = reader.GetString(0);
                        storedPasswordHash = reader.GetString(1);
                    }
                    else return NotFound("User not found.");
                }

                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, storedPasswordHash))
                    return Unauthorized("Current password is incorrect.");

                string newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

                using var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = "UPDATE users SET password = @NewPassword WHERE id = @UserId";
                updateCmd.Parameters.AddWithValue("@NewPassword", newHash);
                updateCmd.Parameters.AddWithValue("@UserId", userId);

                int rows = updateCmd.ExecuteNonQuery();
                if (rows == 0) return StatusCode(500, "Password update failed.");

                return Ok(new { Message = "Password changed successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error changing password: {ex.Message}");
            }
        }

        [HttpGet("profile/{id}")]
        public IActionResult GetUserProfile(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("User ID is required.");

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id, name, email, role, organizationid, organizationname FROM users WHERE id = @Id";
                cmd.Parameters.AddWithValue("@Id", id);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var user = new
                    {
                        UserId = reader.GetString(0),
                        Name = reader.GetString(1),
                        Email = reader.GetString(2),
                        Role = reader.GetString(3),
                        OrganizationId = reader.IsDBNull(4) ? null : reader.GetString(4),
                        OrganizationName = reader.IsDBNull(5) ? null : reader.GetString(5)
                    };
                    return Ok(user);
                }

                return NotFound("User not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving profile: " + ex.Message);
            }
        }

        [HttpPatch("profile/{id}")]
        public IActionResult UpdateUserProfile(string id, [FromBody] UpdateProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("User ID and new name are required.");

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE users SET name = @Name WHERE id = @Id
                    RETURNING id, name, email, role, organizationid, organizationname;
                ";
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Name", request.Name.Trim());

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var updatedUser = new
                    {
                        UserId = reader.GetString(0),
                        Name = reader.GetString(1),
                        Email = reader.GetString(2),
                        Role = reader.GetString(3),
                        OrganizationId = reader.IsDBNull(4) ? null : reader.GetString(4),
                        OrganizationName = reader.IsDBNull(5) ? null : reader.GetString(5)
                    };

                    return Ok(updatedUser);
                }

                return NotFound("User not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error updating profile: " + ex.Message);
            }
        }

        [HttpGet("users")]
        public IActionResult GetUsers([FromQuery] string? organizationId)
        {
            var users = new List<object>();

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = string.IsNullOrWhiteSpace(organizationId)
                ? "SELECT id, name, email, role, organizationid FROM users"
                : "SELECT id, name, email, role, organizationid FROM users WHERE organizationid = @OrgId";

            Console.WriteLine($"[DEBUG] Received organizationId: {organizationId}");

            if (!string.IsNullOrWhiteSpace(organizationId))
                cmd.Parameters.AddWithValue("@OrgId", organizationId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new
                {
                    Id = reader.GetString(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2),
                    Role = reader.GetString(3),
                    OrganizationId = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }

            return Ok(users);
        }

       [HttpGet("projects")]
        public IActionResult GetProjects([FromQuery] string? organizationId)
        {
            var projects = new List<object>();

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
                cmd.CommandText = string.IsNullOrWhiteSpace(organizationId)
                ? "SELECT id, name, description, status, assignedto, organizationid FROM projects"
                : "SELECT id, name, description, status, assignedto, organizationid FROM projects WHERE organizationid = @OrgId";

            if (!string.IsNullOrWhiteSpace(organizationId))
                cmd.Parameters.AddWithValue("@OrgId", organizationId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                projects.Add(new
                {
                    // Use GetGuid and convert it to string.
                    Id = reader.GetGuid(0).ToString(),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Status = reader.GetString(3),
                    AssignedTo = reader.IsDBNull(4) ? null : reader.GetString(4),
                    OrganizationId = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }

            return Ok(projects);
        }

        [HttpGet("company-code")]
        public IActionResult GetCompanyCode([FromQuery] string organizationId)
        {
            if (string.IsNullOrWhiteSpace(organizationId))
                return BadRequest("organizationId is required.");

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT code FROM companycodes WHERE organizationid = @OrgId LIMIT 1";
            cmd.Parameters.AddWithValue("@OrgId", organizationId);

            var result = cmd.ExecuteScalar();
            return Ok(new[] { new { Code = result?.ToString() ?? "" } });
        }

      [HttpPost("projects")]
        public IActionResult CreateProject([FromBody] CreateProjectRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.OrganizationId))
            {
                return BadRequest("Missing required fields: Name and OrganizationId.");
            }

            try
            {
                // Create a new Guid directly.
                Guid newProjectId = Guid.NewGuid();

                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO projects (id, name, description, status, assignedto, organizationid)
                    VALUES (@Id, @Name, @Description, @Status, @AssignedTo, @OrganizationId)
                    RETURNING id;
                ";

                // Pass the Guid directly
                cmd.Parameters.AddWithValue("@Id", newProjectId);
                cmd.Parameters.AddWithValue("@Name", request.Name.Trim());
                cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Status", (object?)request.Status ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AssignedTo", (object?)request.AssignedTo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OrganizationId", request.OrganizationId.Trim());

                // ExecuteScalar should now return a Guid.
                object result = cmd.ExecuteScalar();
                if(result == null)
                {
                    return StatusCode(500, "Error creating project: no id returned.");
                }

                return Ok(new 
                {
                    Id = result.ToString(), 
                    Message = "Project created successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error creating project: " + ex.Message);
            }
        }
    }
}
