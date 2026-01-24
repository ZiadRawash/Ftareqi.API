using DripOut.Application.Common.Settings;
using FluentValidation;
using Ftareqi.API.Configurations;
using Ftareqi.Application.Common;
using Ftareqi.Application.Common.Consts;
using Ftareqi.Application.Common.Settings;
using Ftareqi.Application.Interfaces.BackgroundJobs;
using Ftareqi.Application.Interfaces.Orchestrators;
using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Application.Interfaces.Services;
using Ftareqi.Application.Orchestrators;
using Ftareqi.Application.Validators.Auth;
using Ftareqi.Domain.Models;
using Ftareqi.Infrastructure.BackgroundJobs;
using Ftareqi.Infrastructure.Implementation;
using Ftareqi.Infrastructure.Services;
using Ftareqi.Persistence;
using Ftareqi.Persistence.Repositories;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace Ftareqi.API
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// ---------------------
			// Logging (Serilog)
			// ---------------------
			builder.Host.UseSerilog((context, services, configuration) =>
				configuration.ReadFrom.Configuration(context.Configuration)
							 .ReadFrom.Services(services));

			// ---------------------
			// MVC & Controllers
			// ---------------------
			builder.Services.AddControllers()
				.AddJsonOptions(options =>
				{
					options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
				});

			// Suppress default model state validation to use FluentValidation or custom handling
			builder.Services.Configure<ApiBehaviorOptions>(o =>
			{
				o.SuppressModelStateInvalidFilter = true;
			});

			// ---------------------
			// Settings & Configuration
			// ---------------------
			var jwtSettings = builder.Configuration.GetSection("JWTSettings").Get<JWTSettings>();
			builder.Services.Configure<JWTSettings>(builder.Configuration.GetSection("JWTSettings"));
			builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));

			// ---------------------
			// Database Context
			// ---------------------
			builder.Services.AddDbContext<ApplicationDbContext>(options =>
				options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

			// ---------------------
			// Identity & Authentication
			// ---------------------
			builder.Services.AddIdentity<User, IdentityRole>(options =>
			{
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

			builder.Services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme =
				options.DefaultChallengeScheme =
				options.DefaultForbidScheme =
				options.DefaultScheme =
				options.DefaultSignInScheme =
				options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				options.RequireHttpsMetadata = true;
				options.SaveToken = true;
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidIssuer = builder.Configuration["JWTSettings:Issuer"],
					ValidateAudience = true,
					ValidAudience = builder.Configuration["JWTSettings:Audience"],
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(
						System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWTSettings:SignInKey"]!)
					),
					ValidateLifetime = true,
					ClockSkew = TimeSpan.Zero
				};
			});

			// ---------------------
			// Background Jobs (Hangfire)
			// ---------------------
			builder.Services.AddHangfire(config =>
			{
				config.UseSqlServerStorage(
					builder.Configuration.GetConnectionString("HangfireConnection"));
			});

			// Enable Hangfire server inside the API
			builder.Services.AddHangfireServer();

			// ---------------------
			// Validators, Exception Handling, CORS
			// ---------------------
			builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();

			// The custom exception handler 
			builder.Services.AddExceptionHandler<GlobalErrorHandler>(); 
			builder.Services.AddProblemDetails();

			builder.Services.AddCors(options =>
			{
				options.AddPolicy("_myAllowedOrigins", policy =>
				{
					policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
				});
			});


			// ---------------------
			// Application Services (Repositories, Services, Orchestrators, Jobs)
			// ---------------------
			// Services
			builder.Services.AddScoped<ITokensService, TokensService>();
			builder.Services.AddScoped<IUserService, UserService>();
			builder.Services.AddScoped<IOtpService, OtpService>();
			builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
			builder.Services.AddScoped<IUserClaimsService, UserClaimsService>();
			builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
			builder.Services.AddScoped<IFileMapper, FileMapper>();

			// Orchestrators
			builder.Services.AddScoped<IAuthOrchestrator, AuthOrchestrator>();
			builder.Services.AddScoped<IDriverOrchestrator, DriverOrchestrator>();
			builder.Services.AddScoped<IUserOrchestrator, UserOrchestrator>();


			// Background Job Implementations
			builder.Services.AddScoped<DriverJobs>();

			builder.Services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();
			builder.Services.AddScoped<ICarJobs, CarJobs>();
			builder.Services.AddScoped<IUserJobs, UserJobs>();
			builder.Services.AddScoped<IDriverJobs, DriverJobs>();

			// Repositories & UoW
			builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
			builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();



			//Customed policies 
			builder.Services.AddAuthorization(options =>
			{
				options.AddPolicy("DriverOnly", policy =>
				{
					policy.RequireClaim(CustomClaimTypes.IsDriver, CustomClaimTypes.True);
				});
			});


			// ---------------------
			// Swagger/OpenAPI
			// ---------------------
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen(options =>
			{
				options.SwaggerDoc("v1", new OpenApiInfo
				{
					Version = "v1",
					Title = "Ftareqi API",
					Description = "An ASP.NET Core Web API for Carpooling"
				});

				options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					Name = "Authorization",
					Type = SecuritySchemeType.Http,
					Scheme = "Bearer",
					BearerFormat = "JWT",
					In = ParameterLocation.Header,
					Description = "Enter: Bearer {your token}"
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

			builder.Services.AddCors(options =>
			{
				options.AddPolicy("_myAllowedOrigins", policy =>
				{
					policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
				});
			});


			var app = builder.Build();
			BackgroundJobsConfig.RegisterJobs(app);

			// Serilog request logging should be early in the pipeline
			app.UseSerilogRequestLogging();

			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			 app.UseExceptionHandler(); 

			app.UseHangfireDashboard("/hangfire");

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseCors("_myAllowedOrigins");

			app.UseAuthentication();
			app.UseAuthorization();

			app.MapControllers();

			app.Run();
		}
	}
}