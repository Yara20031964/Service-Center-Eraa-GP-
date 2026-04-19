using KHDMA.Application.DTOs.Auth.Request;
using KHDMA.Application.DTOs.Auth.Response;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KHDMA.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<ApplicationUser> userManager, AppDbContext context, IConfiguration configuration)
        {
            this.userManager = userManager;
            _context = context;
            _configuration = configuration;
        }


        //Register for Customer
        public async Task<AuthResponseDto> RegisterCustomerAsync(RegisterCustomerDto registerDto)
        {
            var existingUser = userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return AuthResponseDto.Fail("Email already exists");
            }

            var user = new ApplicationUser
            {
                FullName = registerDto.FullName,
                Email = registerDto.Email,
                UserName = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber,
                DateOfBirth = registerDto.DateOfBirth,
                Role = UserRole.Customer,
                Status = UserStatus.Active
            };
            //save user with hashed password using user manager and identity framework
            var result = await userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                return AuthResponseDto.Fail(result.Errors.First().Description);
            }

            await userManager.AddToRoleAsync(user, "Customer");

            var customer = new Customer
            {
                ApplicationUserId = user.Id,
            };
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();

            return await GenerateTokensAsync(user);


        }

        //Register for Provider
        public async Task<AuthResponseDto> RegisterProviderAsync(RegisterProviderDto registerDto)
        {
             
            var existingUser = await userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return AuthResponseDto.Fail("Email already exists");
            }

            var user = new ApplicationUser
            {
                FullName = registerDto.FullName,
                Email = registerDto.Email,
                UserName = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber,
                DateOfBirth = registerDto.DateOfBirth,
                Role = UserRole.Provider,
                Status = UserStatus.Active
            };
            var result = await userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                return AuthResponseDto.Fail(result.Errors.First().Description);
            }

            await userManager.AddToRoleAsync(user, "Provider");

            var provider = new Provider
            {
                ApplicationUserId = user.Id,
                HourlyRate = registerDto.HourlyRate,
                ServiceArea = registerDto.ServiceArea,
                State = ProviderState.Pending,
                AvailabilityStatus = AvailabilityStatus.Offline

            };
            await _context.Providers.AddAsync(provider);
            await _context.SaveChangesAsync();

            return await GenerateTokensAsync(user);
        }
        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await userManager.FindByEmailAsync(loginDto.Email);
            if(user == null)
            {
                return AuthResponseDto.Fail("Invalid email or password");
            }

            var isPasswordValid = await userManager.CheckPasswordAsync(user, loginDto.Password);

            if (!isPasswordValid)
            {
                return AuthResponseDto.Fail("Invalid email or password");
            }

            if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Banned)
            {
                return AuthResponseDto.Fail("Your account is suspended. Please contact support.");
            }

            if(user.IsDeleted)
            {
                return AuthResponseDto.Fail("Your account has been deleted. Please contact support.");
            }
            return await GenerateTokensAsync(user);
        }
    }
