using GameReleases.Core.Interfaces;
using GameReleases.Core.Models;

using Microsoft.AspNetCore.Mvc;

namespace GameReleases.WebApi.Controllers
{
    // TODO: добавить версионирование API
    [Route("api/v1/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IJwtService jwtService, ILogger<AuthController> logger)
        {
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("login")]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Простая заглушка проверки. Заменить на реальную проверку (БД/Identity) при необходимости.
            if (!IsValidUser(request.Username, request.Password))
                return Unauthorized(new { error = "Invalid credentials" });

            var token = _jwtService.GenerateToken(request.Username);
            _logger.LogInformation("User {User} logged in", request.Username);

            return Ok(new LoginResponse
            {
                Token = token,
                Expires = DateTime.UtcNow.AddMinutes(60) // синхронизируй с JwtSettings:ExpirationMinutes
            });
        }

        /// <summary>
        /// Временно: один тестовый пользователь
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private static bool IsValidUser(string username, string password)
        {

            return username == "admin" && password == "admin123";
        }
    }
}
