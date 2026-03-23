using DripOut.Application.Common.Settings;
using FirebaseAdmin;
using FluentValidation;
using Ftareqi.API.Configurations;
using Ftareqi.API.Filters;
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
using Ftareqi.Infrastructure.SignalR;
using Ftareqi.Persistence;
using Ftareqi.Persistence.Repositories;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using StackExchange.Redis;
using System.Text;
using static TokenBucketMiddleware;

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
			//Controllers 
			// ---------------------
			builder.Services.AddControllers()
				.AddNewtonsoftJson(options =>
				{
					options.SerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
					options.SerializerSettings.MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead;
					options.SerializerSettings.Converters.Add(new StringEnumConverter());
					options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
					options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
				});

			builder.Services.Configure<ApiBehaviorOptions>(o =>
			{
				o.SuppressModelStateInvalidFilter = true;
			});

			// ---------------------
			// SignalR Configuration 
			// ---------------------
			builder.Services.AddSignalR(options =>
			{
				options.HandshakeTimeout = TimeSpan.FromSeconds(15);
				options.KeepAliveInterval = TimeSpan.FromSeconds(30);
			})
			.AddNewtonsoftJsonProtocol(options =>
			{
				options.PayloadSerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
				options.PayloadSerializerSettings.MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead;
				options.PayloadSerializerSettings.Converters.Add(new StringEnumConverter());
				options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
				options.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
			});

			// ---------------------
			// Settings & Configuration
			// ---------------------
			builder.Services.Configure<JWTSettings>(builder.Configuration.GetSection("JWTSettings"));
			builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
			builder.Services.Configure<PaymobSettings>(builder.Configuration.GetSection("PaymobSettings"));
			

			// In "Application Services" section:
			builder.Services.AddScoped<IFcmService, FcmService>();
			// ---------------------
			// Database Context
			// ---------------------
			builder.Services.AddDbContext<ApplicationDbContext>(options =>
				options.UseSqlServer(
					builder.Configuration.GetConnectionString("DefaultConnection"),
					x=>x.UseNetTopologySuite())
				);

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
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
						Encoding.UTF8.GetBytes(builder.Configuration["JWTSettings:SignInKey"]!)
					),
					ValidateLifetime = true,
					ClockSkew = TimeSpan.Zero
				};

				options.Events = new JwtBearerEvents
				{
					OnMessageReceived = context =>
					{
						var accessToken = context.Request.Query["access_token"];
						if (!string.IsNullOrEmpty(accessToken) &&
							(context.HttpContext.WebSockets.IsWebSocketRequest || context.Request.Headers["Connection"] == "Upgrade"))
						{
							context.Token = accessToken;
						}
						return Task.CompletedTask;
					}
				};
			});

			// ---------------------
			// Background Jobs (Hangfire)
			// ---------------------
			builder.Services.AddHangfire(config =>
			{
				config.UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"));
			});
			builder.Services.AddHangfireServer();

			// ---------------------
			// Validators, Exception Handling, CORS
			// ---------------------
			builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestDtoValidator>();
			builder.Services.AddExceptionHandler<GlobalErrorHandler>();
			builder.Services.AddProblemDetails();

			builder.Services.AddCors(options =>
			{
				options.AddPolicy("FlexiblePolicy", policy =>
				{
					policy.SetIsOriginAllowed(_ => true)
						  .AllowAnyHeader()
						  .AllowAnyMethod()
						  .AllowCredentials();
				});
			});

			//
			//Redis Service
			//

			builder.Services.AddStackExchangeRedisCache(options =>
			{
				options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
				options.InstanceName = "Ftareqi:";
			});
			builder.Services.AddSingleton<IConnectionMultiplexer>(
				ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("RedisConnection")!)
			);

			// Rate Limit Options
			builder.Services.AddSingleton<AuthTokenBucketOptions>(new AuthTokenBucketOptions
			{
				Capacity = 20,             
				RefillRatePerSecond = 5
			});

			builder.Services.AddSingleton<UnauthTokenBucketOptions>(new UnauthTokenBucketOptions
			{
				Capacity = 5,
				RefillRatePerSecond = 1
			});

			// ---------------------
			// Application Services
			// ---------------------
			builder.Services.AddScoped<ITokensService, TokensService>();
			builder.Services.AddScoped<IUserService, UserService>();
			builder.Services.AddScoped<IOtpService, OtpService>();
			builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
			builder.Services.AddScoped<IUserClaimsService, UserClaimsService>();
			builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
			builder.Services.AddScoped<IFileMapper, FileMapper>();
			builder.Services.AddScoped<IWalletService, WalletService>();
			builder.Services.AddHttpClient<IPaymentGateway, PaymobPaymentGateway>();
			builder.Services.AddScoped<IAuthOrchestrator, AuthOrchestrator>();
			builder.Services.AddScoped<IDriverOrchestrator, DriverOrchestrator>();
			builder.Services.AddScoped<IUserOrchestrator, UserOrchestrator>();
			builder.Services.AddScoped<IRideService, RideService>();
			builder.Services.AddScoped<IBookingService, BookingService>();
			builder.Services.AddScoped<INotificationOrchestrator, NotificationOrchestrator>();
			builder.Services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();
			builder.Services.AddScoped<ICarJobs, CarJobs>();
			builder.Services.AddScoped<IUserJobs, UserJobs>();
			builder.Services.AddScoped<IDriverJobs, DriverJobs>();
			builder.Services.AddScoped<DriverJobs>();
			builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
			builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
			builder.Services.AddScoped<INotificationBuilder, NotificationBuilder>();
			builder.Services.AddScoped<INotificationService, NotificationService>();
			builder.Services.AddScoped<IFcmService, FcmService>();
			builder.Services.AddScoped<IFcmTokenService, FcmTokenService>();
			builder.Services.AddScoped<IDistributedCachingService, RedisCachingService>();
			builder.Services.AddScoped<IRideOrchestrator, RideOrchestrator>();
			builder.Services.AddAuthorization(options =>
			{
				options.AddPolicy("DriverOnly", policy =>
				{
					policy.RequireClaim(CustomClaimTypes.IsDriver, CustomClaimTypes.True);
				});
			});
			////FCM Configurations 
			builder.Services.Configure<FirebaseSettings>(builder.Configuration.GetSection("FirebaseSettings"));
			var firebaseSettings = builder.Configuration
			.GetSection("FirebaseSettings")
			.Get<FirebaseSettings>();

			if (firebaseSettings != null && !string.IsNullOrEmpty(firebaseSettings.ProjectId))
			{
				try
				{
					var json = JsonConvert.SerializeObject(new
					{
						type = "service_account",
						project_id = firebaseSettings.ProjectId,
						private_key_id = firebaseSettings.PrivateKeyId,
						private_key = firebaseSettings.PrivateKey?.Replace("\\n", "\n"),
						client_email = firebaseSettings.ClientEmail,
						client_id = firebaseSettings.ClientId,
						auth_uri = firebaseSettings.AuthUri,
						token_uri = firebaseSettings.TokenUri,
						auth_provider_x509_cert_url = firebaseSettings.AuthProviderX509CertUrl,
						client_x509_cert_url = firebaseSettings.ClientX509CertUrl
					});
					FirebaseApp.Create(new AppOptions
					{
						Credential = GoogleCredential.FromJson(json)
					});

					Console.WriteLine("? Firebase initialized successfully");
				}
				catch (Exception ex)
				{
					Console.WriteLine($" Firebase initialization failed: {ex.Message}");
					// Don't throw - let app run without FCM
				}
			}
			else
			{
				Console.WriteLine(" Firebase configuration not found");
			}

			// ---------------------
			// Swagger
			// ---------------------
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen(options =>
			{
				options.SwaggerDoc("v1", new OpenApiInfo { Title = "Ftareqi API", Version = "v1" });
				
				// Configure enums to be displayed as strings in Swagger
				options.SchemaFilter<EnumSchemaFilter>();
				
				options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
				{
					Name = "Authorization",
					Type = SecuritySchemeType.Http,
					Scheme = "Bearer",
					In = ParameterLocation.Header
				});
				options.AddSecurityRequirement(new OpenApiSecurityRequirement {
					{
						new OpenApiSecurityScheme {
							Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
						}, Array.Empty<string>()
					}
				});
			});

			var app = builder.Build();
			BackgroundJobsConfig.RegisterJobs(app);
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
			app.UseCors("FlexiblePolicy");
			app.UseAuthentication();
			app.UseMiddleware<TokenBucketMiddleware>();
			app.UseAuthorization();

			app.MapHub<NotificationHub>("/notificationHub");
			app.MapControllers();

			app.Run();
		}
	}
}