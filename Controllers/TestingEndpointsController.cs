using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace ZitaDataSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestingEndpointsController : ControllerBase
    {
        private readonly string _connectionString;

        public TestingEndpointsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("PrimaryDatabase")
                ?? throw new InvalidOperationException("PrimaryDatabase connection string is missing.");
        }

        // POST: api/TestingEndpoints/trigger
        [HttpPost("trigger")]
        public async Task<IActionResult> TriggerEndpoint([FromBody] object payload)
        {
            // Serialize the payload into JSON
            string payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);

            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    using (var cmd = conn.CreateCommand())
                    {
                        // Insert the JSON payload into testingEndpoints table.
                        // Assumes testingEndpoints has columns: id (serial/UUID), info (text or jsonb), and createdat (timestamp)
                        cmd.CommandText = @"
                            INSERT INTO testingEndpoints (info, createdat)
                            VALUES (@info, @createdat)";
                        cmd.Parameters.Add(new Npgsql.NpgsqlParameter("@info", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = payloadJson });
                        cmd.Parameters.Add(new Npgsql.NpgsqlParameter("@createdat", NpgsqlTypes.NpgsqlDbType.TimestampTz) { Value = DateTime.UtcNow });
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Ok("Payload saved successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error saving payload: {ex.Message}");
            }
        }
    }
}
