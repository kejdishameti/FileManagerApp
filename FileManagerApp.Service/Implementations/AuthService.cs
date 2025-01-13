﻿using AutoMapper;
using FileManagerApp.Data.UnitOfWork;
using FileManagerApp.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FileManagerApp.Service.DTOs.Auth;
using FileManagerApp.Service.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace FileManagerApp.Service.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public AuthService(IUnitOfWork unitOfWork, IConfiguration config, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _config = config;
            _mapper = mapper;
        }

        public async Task<UserDTO> RegisterAsync(RegisterDTO registerDto)
        {
            // Check if email is already taken
            var existingUser = await _unitOfWork.Users.GetByEmailAsync(registerDto.Email);
            if (existingUser != null)
                throw new ArgumentException("Email is already registered");

            // Create new user
            var user = User.Create(registerDto.Email, registerDto.Username);
            user.SetPassword(registerDto.Password);

            // Save to database
            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Create token and return UserDTO
            var token = await CreateTokenAsync(user);
            var userDto = _mapper.Map<UserDTO>(user);
            userDto.Token = token;

            return userDto;
        }

        public async Task<UserDTO> LoginAsync(LoginDTO loginDto)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(loginDto.Email);

            if (user == null || !user.VerifyPassword(loginDto.Password))
                throw new ArgumentException("Invalid email or password");

            var token = await CreateTokenAsync(user);
            var userDto = _mapper.Map<UserDTO>(user);
            userDto.Token = token;

            return userDto;
        }

        public async Task<string> CreateTokenAsync(User user)
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["TokenKey"]));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}