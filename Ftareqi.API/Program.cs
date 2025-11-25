using DripOut.Application.Common.Settings;
using FluentValidation;
using Ftareqi.Application.Common;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.Orchestrators;
using Ftareqi.Application.Validators.Auth;
using Ftareqi.Domain.Models;
using Ftareqi.Infrastructure.Implementation;
using Ftareqi.Infrastructure.Services;
using Ftareqi.Persistence;
using Ftareqi.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;

namespace Ftareqi.API
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);
			//configure IsModelValidated Otptions 
			builder.Services.Configure<ApiBehaviorOptions>(options =>
			{
				options.SuppressModelStateInvalidFilter = true;
			});

			// Bind JWT Settings
			var jwtSettings = builder.Configuration.GetSection("JWTSettings").Get<JWTSettings>();
			builder.Services.Configure<JWTSettings>(builder.Configuration.GetSection("JWTSettings"));

			// Add Authentication with JWT
			builder.Services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				options.SaveToken = true;
				options.RequireHttpsMetadata = false; // true in production
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = jwtSettings!.Issuer,
					ValidAudience = jwtSettings.Audience,
					IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SignInKey!)),
					ClockSkew = TimeSpan.Zero 
				};
			});
			
			// Add Authorization
			builder.Services.AddAuthorization();

			// Inject GlobalErrorHandler
			builder.Services.AddExceptionHandler<GlobalErrorHandler>();
			builder.Services.AddProblemDetails();

			// Initialize FluentValidators
			builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();

			// Configuration for Seq (Serilog)
			builder.Host.UseSerilog((context, configuration) =>
				configuration.ReadFrom.Configuration(context.Configuration));

			// Configuration for Sql Server Connection
			builder.Services.AddDbContext<ApplicationDbContext>(options =>
				options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

			// Configuration for Identity
			builder.Services.AddIdentity<User, IdentityRole>(options => {
				options.Password.RequireDigit = true;
				options.Password.RequireLowercase = true;
				options.Password.RequireUppercase = true;
				options.Password.RequireNonAlphanumeric = true;
				options.Password.RequiredLength = 8;
				options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
				options.Lockout.MaxFailedAccessAttempts = 5;
				options.Lockout.AllowedForNewUsers = true;
			})
				.AddEntityFrameworkStores<ApplicationDbContext>()
				.AddDefaultTokenProviders();

			// Add services to the container
			builder.Services.AddControllers();

			// Register Services
			builder.Services.AddScoped<ITokensService, TokensService>();
			builder.Services.AddScoped<IUserService, UserService>();
			builder.Services.AddScoped<IAuthOrchestrator, AuthOrchestrator>();
			builder.Services.AddScoped<IOtpService, OtpService>();
			builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
			builder.Services.AddScoped<IUserClaimsService, UserClaimsService>();

			// Register Repositories
			builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
			builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

			// Swagger configuration with JWT support
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen(options =>
			{
				options.SwaggerDoc("v1", new OpenApiInfo
				{
					Version = "v1",
					Title = "Ftareqi API",
					Description = "An ASP.NET Core Web API for Carpooling "
				});

				// Add JWT Authentication to Swagger
				options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					Name = "Authorization",
					Type = SecuritySchemeType.Http,
					Scheme = "Bearer",
					BearerFormat = "JWT",
					In = ParameterLocation.Header,
					Description = "Enter 'Bearer' followed by a space and your JWT token.\n\nExample: 'Bearer eyJhbGc...'"
				});

				options.AddSecurityRequirement(new OpenApiSecurityRequirement
				{
					{
						new OpenApiSecurityScheme
						{
							Reference = new OpenApiReference
							{
								Type = ReferenceType.SecurityScheme,
								Id = "Bearer"
							}
						},
						Array.Empty<string>()
					}
				});

				var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
				options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
			});

			// CORS services
			builder.Services.AddCors(options =>
			{
				options.AddPolicy("_myAllowedOrigins", policy =>
				{
					policy
						.AllowAnyOrigin()
						.AllowAnyHeader()
						.AllowAnyMethod();
				});
			});
			var app = builder.Build();

			// Add ExceptionHandler to pipeline
			app.UseExceptionHandler();

			// Configure the HTTP request pipeline
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseCors("_myAllowedOrigins");

			// IMPORTANT: Authentication must come before Authorization
			app.UseAuthentication();
			app.UseAuthorization();

			app.MapControllers();

			try
			{
				Log.Information("Starting Ftareqi web application");
				Log.Information("Serilog/Seq test message: application started");
				app.Run();
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, "Ftareqi application terminated unexpectedly");
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}
	}
}