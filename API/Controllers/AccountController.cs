using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTO;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenInterface _iTokenService;
        public AccountController(DataContext context, ITokenInterface iTokenService){
            _context = context;
            _iTokenService = iTokenService;
        }
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDTO registerDTO){
            using var hmac = new HMACSHA512();
            
            if( await UserExits(registerDTO.Username)) return BadRequest("Username is taken.");

            var user = new AppUser {
                UserName = registerDTO.Username,
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDTO.Password)),
                PasswordSalt = hmac.Key
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserDto{
                Username = user.UserName,
                Token = _iTokenService.CreateToken(user)
            };
        }

        private async Task<bool> UserExits(string username){
            return await _context.Users.AnyAsync(x => x.UserName == username);
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto){
            var user = await _context.Users.
                SingleOrDefaultAsync(x => x.UserName == loginDto.Username);
            
            if (user == null) return Unauthorized("Username invalid");

            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for(int i = 0; i < computeHash.Length; i++){
                if(computeHash[i] != user.PasswordHash[i]) return Unauthorized("Password invalid");
            }

            return new UserDto{
                Username = user.UserName,
                Token = _iTokenService.CreateToken(user)
            };
        }
    }
}