// Services/AuthService.cs
using BCrypt.Net; // or any other password hashing library
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;

namespace ZitaDataSystem.Services
{
    public class AuthService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;

        public AuthService(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("PrimaryDatabase")
                ?? throw new InvalidOperationException("Missing DB connection string.");
        }

        // Simple method to register a user in the DB
        public int RegisterUser(string email, string password, string role, string? orgCode)
        {
            // Hash the password
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Users (Email, PasswordHash, Role, OrgCode)
                VALUES (@Email, @PasswordHash, @Role, @OrgCode)
                RETURNING Id;
            ";
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmd.Parameters.AddWithValue("@Role", role);
            cmd.Parameters.AddWithValue("@OrgCode", (object?)orgCode ?? DBNull.Value);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // Simple method to validate user credentials and return user ID if valid
        public (int userId, string role, string? orgCode)? ValidateCredentials(string email, string password)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, PasswordHash, Role, OrgCode FROM Users WHERE Email = @Email;";
            cmd.Parameters.AddWithValue("@Email", email);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            int userId = reader.GetInt32(0);
            string hash = reader.GetString(1);
            string role = reader.GetString(2);
            string? orgCode = reader.IsDBNull(3) ? null : reader.GetString(3);

            // Compare hashed password
            if (!BCrypt.Net.BCrypt.Verify(password, hash))
                return null;

            return (userId, role, orgCode);
        }

        // Generate a simple token (stub). Replace with real JWT logic for production.
        public string GenerateToken(string userId, string role)
        {
            return $"{userId}:{role}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        }

        // Parse and validate the stub token
        public (int userId, string role)? ValidateToken(string token)
        {
            // Example format: "userId:role:timestamp"
            var parts = token.Split(':');
            if (parts.Length != 3) return null;

            if (!int.TryParse(parts[0], out int userId))
                return null;

            string role = parts[1];
            // optional: check if token is expired

            return (userId, role);
        }
    }
}
