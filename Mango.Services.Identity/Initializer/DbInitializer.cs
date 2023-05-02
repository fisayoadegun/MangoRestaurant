using IdentityModel;
using Mango.Services.Identity.DbContexts;
using Mango.Services.Identity.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Mango.Services.Identity.Initializer
{
	public class DbInitializer : IDbInitializer
	{
		private readonly ApplicationDbContext _db;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;

        public DbInitializer(ApplicationDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
			_db = db;
			_userManager = userManager;
			_roleManager = roleManager;
		}
        public void Initialize()
		{
			if (_roleManager.FindByNameAsync(SD.Admin).Result == null)
			{
				_roleManager.CreateAsync(new IdentityRole(SD.Admin)).GetAwaiter().GetResult();
				_roleManager.CreateAsync(new IdentityRole(SD.Customer)).GetAwaiter().GetResult();
			}
			else { return; }

			ApplicationUser adminUser = new ApplicationUser()
			{
				UserName = "fisayo@gmail.com",
				Email = "fisayo@gmail.com",
				EmailConfirmed = true,
				PhoneNumber = "1111111111111",
				FirstName = "Fisayo",
				LastName = "Adegun"
			};
			_userManager.CreateAsync(adminUser, "Admin@123").GetAwaiter().GetResult();
			_userManager.AddToRoleAsync(adminUser, SD.Admin).GetAwaiter().GetResult();

			var temp = _userManager.AddClaimsAsync(adminUser, new Claim[]
			{
				new Claim(JwtClaimTypes.Name, adminUser.FirstName+" "+ adminUser.LastName),
				new Claim(JwtClaimTypes.GivenName, adminUser.FirstName),				
				new Claim(JwtClaimTypes.FamilyName, adminUser.LastName),
				new Claim(JwtClaimTypes.Role, SD.Admin),
			}).Result;

			ApplicationUser customerUser = new ApplicationUser()
			{
				UserName = "customer@gmail.com",
				Email = "customer@gmail.com",
				EmailConfirmed = true,
				PhoneNumber = "1111111111111",
				FirstName = "Ben",
				LastName = "Cust"
			};
			_userManager.CreateAsync(customerUser, "Customer@123").GetAwaiter().GetResult();
			_userManager.AddToRoleAsync(customerUser, SD.Customer).GetAwaiter().GetResult();

			var temp2 = _userManager.AddClaimsAsync(customerUser, new Claim[]
			{
				new Claim(JwtClaimTypes.Name, customerUser.FirstName+" "+ customerUser.LastName),
				new Claim(JwtClaimTypes.GivenName, customerUser.FirstName),				
				new Claim(JwtClaimTypes.FamilyName, customerUser.LastName),
				new Claim(JwtClaimTypes.Role, SD.Customer),
			}).Result;
		}
	}
}
