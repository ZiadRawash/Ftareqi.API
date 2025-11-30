using DripOut.Application.Common.Settings;
using FluentValidation;
using Ftareqi.Application.Common;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

			// ---------------------
			// Serilog
			// ---------------------
			builder.Host.UseSerilog((context, services, configuration) =>
				configuration.ReadFrom.Configuration(context.Configuration)
							 .ReadFrom.Services(services));

			builder.Services.Configure<ApiBehaviorOptions>(o =>
			{
				o.SuppressModelStateInvalidFilter = true;
			});

			// ---------------------
			// JWT Settings
			// ---------------------
			var jwtSettings = builder.Configuration.GetSection("JWTSettings").Get<JWTSettings>();
			builder.Services.Configure<JWTSettings>(builder.Configuration.GetSection("JWTSettings"));

			builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));

			// ---------------------
			// Authentication
			// ---------------------
			builder.Services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				options.SaveToken = true;
				options.RequireHttpsMetadata = false;
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

			// ---------------------
			// Hangfire 
			// ---------------------
			builder.Services.AddHangfire(config =>
			{
				config.UseSqlServerStorage(
					builder.Configuration.GetConnectionString("HangfireConnection"));
			});

			// Enable Hangfire server inside the API
			builder.Services.AddHangfireServer();

			builder.Services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();
			builder.Services.AddScoped<IDriverImageUploadJob, DriverImageUploadJob>();

			// ---------------------
			// Authorization
			// ---------------------
			builder.Services.AddAuthorization();

			builder.Services.AddExceptionHandler<GlobalErrorHandler>();
			builder.Services.AddProblemDetails();

			builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();

			// ---------------------
			// DbContext
			// ---------------------
			builder.Services.AddDbContext<ApplicationDbContext>(options =>
				options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

			// ---------------------
			// Identity
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

			// ---------------------
			// Services
			// ---------------------
			builder.Services.AddControllers();

			builder.Services.AddScoped<ITokensService, TokensService>();
			builder.Services.AddScoped<IUserService, UserService>();
			builder.Services.AddScoped<IAuthOrchestrator, AuthOrchestrator>();
			builder.Services.AddScoped<IOtpService, OtpService>();
			builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
			builder.Services.AddScoped<IUserClaimsService, UserClaimsService>();
			builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
			builder.Services.AddScoped<IFileMapper, FileMapper>();
			builder.Services.AddScoped<IDriverOrchestrator, DriverOrchestrator>();

			builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
			builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

			// ---------------------
			// Swagger
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

			app.UseSerilogRequestLogging();

			app.UseExceptionHandler();

			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHangfireDashboard("/hangfire"); // Dashboard enabled

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseCors("_myAllowedOrigins");

			app.UseAuthentication();
			app.UseAuthorization();

			app.MapControllers();

			try
			{
				Log.Information("Starting Ftareqi web application");
				app.Run();
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, "Ftareqi app terminated unexpectedly");
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}
	}
}
