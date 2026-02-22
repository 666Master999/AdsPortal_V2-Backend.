using AdsPortal_V2.DTOs;
using AdsPortal_V2.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdsPortal_V2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IUserService users, IJwtService jwt) : ControllerBase
    {
        private readonly IUserService _users = users;
        private readonly IJwtService _jwt = jwt;

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Login) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("Login and password required.");

            if (await _users.UserExistsAsync(dto.Login))
                return Conflict("User already exists.");

            var user = await _users.RegisterAsync(dto);
            var token = _jwt.GenerateToken(user);
            return Ok(new { token });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Login) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("Login and password required.");

            var user = await _users.AuthenticateAsync(dto);
            if (user == null) return Unauthorized(new { error = "Неверный логин или пароль" });
            var token = _jwt.GenerateToken(user);
            return Ok(new { token });
        }
    }
}