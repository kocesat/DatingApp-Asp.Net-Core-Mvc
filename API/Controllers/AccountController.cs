using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
  public class AccountController : BaseApiController
  {
    private readonly DataContext _context;
    private readonly ITokenService _tokenService;
    public AccountController(DataContext context, ITokenService tokenService)
    {
      _tokenService = tokenService;
      _context = context;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AppUser>> Register(RegisterDto registerDto)
    {
      if (await UserExists(registerDto.Username))
        return BadRequest("Username is taken");

      using var hmac = new HMACSHA512();

      var user = new AppUser()
      {
        UserName = registerDto.Username.ToLower(),
        PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
        PasswordSalt = hmac.Key
      };

      _context.Users.Add(user);
      await _context.SaveChangesAsync();
      return user;
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
      // Check if the same username is in the database
      var user = await _context.Users
          .SingleOrDefaultAsync(u => u.UserName == loginDto.Username);

      // check if the checking the username return value is not null
      if (user == null)
        return Unauthorized("Invalid username");

      // to salt the password we do not use random key, we use already fixed random key when registering
      using var hmac = new HMACSHA512(user.PasswordSalt);

      // compute the hash of the username with salt and get the byte array
      var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

      // for each byte check if it is the same as hash for the same user in db.
      for (int i = 0; i < computedHash.Length; i++)
      {
        if (computedHash[i] != user.PasswordHash[i])
        {
          return Unauthorized("Invalid password");
        }
      }

      return new UserDto
      {
          Username = user.UserName,
          Token = _tokenService.CreateToken(user)   
      };
    }

    private async Task<bool> UserExists(string username)
    {
      return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());
    }

  }
}