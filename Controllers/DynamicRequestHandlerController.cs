using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ZitaDataSystem.Models;
using ZitaDataSystem.Services;

namespace ZitaDataSystem.Controllers
{
    [ApiController]
    [Route("api/Dynamic")]
    public class DynamicRequestHandlerController : ControllerBase
    {
        private readonly EndpointsService _endpointsService;
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public DynamicRequestHandlerController(EndpointsService endpointsService, IConfiguration config)
        {
            _endpointsService = endpointsService;
            _config = config;
            _connectionString = config.GetConnectionString("PrimaryDatabase")
                ?? throw new InvalidOperationException("PrimaryDatabase connection string is missing.");
        }

        [HttpPost("{endpointName}")]
        public IActionResult HandlePost(string endpointName, [FromBody] Dictionary<string, object> payload)
        {
            string apiKey = Request.Headers["x-api-key"];
            if (string.IsNullOrEmpty(apiKey))
                return BadRequest("Missing API key header.");

            // Now include _connectionString in the GetEndpoint call
            var definition = _endpointsService.GetEndpoint(endpointName, "POST", apiKey, _connectionString);
            if (definition == null)
                return BadRequest("No matching dynamic endpoint found.");

            var (parsedSql, parameters) = ParseSql(definition.SqlCommand, payload);

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = parsedSql;
                for (int i = 0; i < parameters.Count; i++)
                {
                    cmd.Parameters.AddWithValue($"@p{i}", parameters[i]);
                }
                cmd.ExecuteNonQuery();
                return Ok("Insert successful.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
        }

         // PUT: Update (dynamic)
        [HttpPut("{endpointName}")]
        public IActionResult HandlePut(string endpointName, [FromBody] Dictionary<string, object> payload)
        {
            string apiKey = Request.Headers["x-api-key"];
            if (string.IsNullOrEmpty(apiKey))
                return BadRequest("Missing API key header.");

            var definition = _endpointsService.GetEndpoint(endpointName, "PUT", apiKey, _connectionString);
            if (definition == null)
                return BadRequest("No matching dynamic endpoint found for PUT.");

            var (parsedSql, parameters) = ParseSql(definition.SqlCommand, payload);

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = parsedSql;
                for (int i = 0; i < parameters.Count; i++)
                {
                    cmd.Parameters.AddWithValue($"@p{i}", parameters[i]);
                }
                cmd.ExecuteNonQuery();
                return Ok("Update successful.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
        }

        // DELETE: Delete (dynamic)
        [HttpDelete("{endpointName}")]
        public IActionResult HandleDelete(string endpointName, [FromBody] Dictionary<string, object> payload)
        {
            string apiKey = Request.Headers["x-api-key"];
            if (string.IsNullOrEmpty(apiKey))
                return BadRequest("Missing API key header.");

            var definition = _endpointsService.GetEndpoint(endpointName, "DELETE", apiKey, _connectionString);
            if (definition == null)
                return BadRequest("No matching dynamic endpoint found for DELETE.");

            var (parsedSql, parameters) = ParseSql(definition.SqlCommand, payload);

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = parsedSql;
                for (int i = 0; i < parameters.Count; i++)
                {
                    cmd.Parameters.AddWithValue($"@p{i}", parameters[i]);
                }
                cmd.ExecuteNonQuery();
                return Ok("Deletion successful.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
        }

        // Helper: parse placeholders ($param$) from the SQL string and map them to payload values
        private (string, List<object>) ParseSql(string sql, Dictionary<string, object> payload)
        {
            var parameters = new List<object>();
            string pattern = @"\$(.*?)\$";
            var regex = new Regex(pattern);
            string parsedSql = sql;

            foreach (Match match in regex.Matches(sql))
            {
                string placeholder = match.Value;
                string paramName = match.Groups[1].Value;
                object value = payload.ContainsKey(paramName) ? payload[paramName] : "";
                parameters.Add(value);
                int index = parameters.Count - 1;
                parsedSql = parsedSql.Replace(placeholder, $"@p{index}");
            }
            return (parsedSql, parameters);
        }
    }
}
