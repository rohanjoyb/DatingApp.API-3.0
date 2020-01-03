/* This repository is reponsible for querying the database via EF and inject datacontext */
using System;
using System.Threading.Tasks;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DataContext _context;
        public AuthRepository(DataContext context)
        {
            this._context = context;

        }
        /* Check the username and hashed password and compare it with the database  */
        public async Task<User> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Username == username);

            if(username == null){
                return null;
            }
            if(!VerifyPasswordHash(password,user.PasswordHash,user.PasswordSalt)){
                return null;
            }

            return user;
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using(var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt)) //Anything inside the using block will be disposed as soon as we are finished with it.
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for(int i=0; i< computedHash.Length; i++){
                    if(computedHash[i] != passwordHash[i])
                        return false;
                }
                return true;
            }
        }

        public async Task<User> Register(User user, string password)
        {
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt); //Out keyword is for we are passing the reference and not the value of the passwordHash and passwordSalt. If the reference is updated the values will also be updated.

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return user;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            /* To Compare the hash generated from the brower on login with the has stored in database */
            using(var hmac = new System.Security.Cryptography.HMACSHA512()) //Anything inside the using block will be disposed as soon as we are finished with it.
            {
                passwordSalt = hmac.Key; //A random generated key
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        public async Task<bool> USerExists(string username)
        {
            if(await _context.Users.AnyAsync(x => x.Username == username)){
                return true;
            }
            return false;
        }
    }
}