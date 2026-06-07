using DripOut.Application.Common.Settings;
using FirebaseAdmin;
using FluentValidation;
using Ftareqi.API.Configurations;
using Ftareqi.API.Filters;
using Ftareqi.API.Middlewares;
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
using Ftareqi.Infrastructure.HealthChecks;
using Ftareqi.Infrastructure.Implementation;
using Ftareqi.Infrastructure.Services;
using Ftareqi.Infrastructure.SignalR.Hubs;
using Ftareqi.Persistence;
using Ftareqi.Persistence.Repositories;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.SqlServer;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; 
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using StackExchange.Redis;
using System.Reflection;
using System.Text;
using Twilio.Clients;
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
            // Controllers
            // ---------------------
            builder.Services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
                    options.SerializerSettings.MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead;
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Include;
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
            builder.Services.Configure<TwilioSettings>(builder.Configuration.GetSection("TwilioSettings"));
            builder.Services.AddSingleton<ITwilioRestClient>(sp =>
            {
                var twilioOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TwilioSettings>>().Value;

                if (string.IsNullOrWhiteSpace(twilioOptions.AccountSID) || string.IsNullOrWhiteSpace(twilioOptions.AuthToken))
                {
                    throw new InvalidOperationException("TwilioSettings must include AccountSID and AuthToken.");
                }

                return new TwilioRestClient(twilioOptions.AccountSID, twilioOptions.AuthToken);
            });

            builder.Services.AddScoped<IFcmService, FcmService>();

            // ---------------------
            // Database Context
            // ---------------------
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    x => x.UseNetTopologySuite())
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
                options.RequireHttpsMetadata = false;
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

            // -----------------------------------------------------------------
            // Automated Provisioning: hangfire
            // -----------------------------------------------------------------
            var hangfireConnString = builder.Configuration.GetConnectionString("HangfireConnection");
            if (!string.IsNullOrWhiteSpace(hangfireConnString))
            {
                try
                {
                    var dbBuilder = new SqlConnectionStringBuilder(hangfireConnString);
                    var dbName = dbBuilder.InitialCatalog;

                    dbBuilder.InitialCatalog = "master";

                    using (var connection = new SqlConnection(dbBuilder.ConnectionString))
                    {
                        connection.Open();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = $@"
                                IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{dbName}')
                                BEGIN
                                    CREATE DATABASE [{dbName}];
                                END";
                            command.ExecuteNonQuery();
                        }
                    }
                    Console.WriteLine($"[Hangfire Setup]: Verified or created database '{dbName}' successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Hangfire Setup Error]: Could not pre-create database. Reason: {ex.Message}");
                }
            }

            // ---------------------
            // Background Jobs (Hangfire)
            // ---------------------
            builder.Services.AddHangfire(config =>
            {
                config.UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"),
                    new SqlServerStorageOptions
                    {
                        PrepareSchemaIfNecessary = true,
                        SchemaName = "HangFire",
                    });
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

            // ---------------------
            // Redis
            // ---------------------
            var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection");
            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                throw new InvalidOperationException("RedisConnection is not configured.");
            }

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "Ftareqi:";
            });
            builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(options);
            });

            // ---------------------
            // Rate Limiting
            // ---------------------
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
            builder.Services.AddScoped<ICsvExportService, CsvExportService>();
            builder.Services.AddHttpClient<IPaymentGateway, PaymobPaymentGateway>();
            builder.Services.AddScoped<IAuthOrchestrator, AuthOrchestrator>();
            builder.Services.AddScoped<IDriverOrchestrator, DriverOrchestrator>();
            builder.Services.AddScoped<IUserOrchestrator, UserOrchestrator>();
            builder.Services.AddScoped<IWalletOrchestrator, WalletOrchestrator>();
            builder.Services.AddScoped<IRideService, RideService>();
            builder.Services.AddScoped<IBookingService, BookingService>();
            builder.Services.AddScoped<IReviewService, ReviewService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<INotificationOrchestrator, NotificationOrchestrator>();
            builder.Services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();
            builder.Services.AddScoped<IBookingJobs, BookingJobs>();
            builder.Services.AddScoped<ICarJobs, CarJobs>();
            builder.Services.AddScoped<IUserJobs, UserJobs>();
            builder.Services.AddScoped<IDriverJobs, DriverJobs>();
            builder.Services.AddScoped<IRideJobs, RideJobs>();
            builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<INotificationBuilder, NotificationBuilder>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IFcmService, FcmService>();
            builder.Services.AddScoped<IFcmTokenService, FcmTokenService>();
            builder.Services.AddScoped<IDistributedCachingService, RedisCachingService>();
            builder.Services.AddScoped<IRideOrchestrator, RideOrchestrator>();
            builder.Services.AddScoped<ISmsService, TwilioSmsService>();

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("DriverOnly", policy =>
                {
                    policy.RequireClaim(CustomClaimTypes.IsDriver, CustomClaimTypes.True);
                });
            });

            // ---------------------
            // Health Checks
            // ---------------------
            builder.Services.AddHealthChecks()
                .AddCheck<CloudinaryHealthCheck>(
                    "cloudinary-api",
                    failureStatus: HealthStatus.Degraded
                )
                .AddCheck<TwilioHealthCheck>(
                    "twilio-api",
                    failureStatus: HealthStatus.Degraded
                )
                .AddCheck<PaymobHealthCheck>(
                    "paymob-api",
                    failureStatus: HealthStatus.Degraded
                )
                .AddSqlServer(
                    connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
                    name: "SqlServer-db"
                )
                .AddSqlServer(
                    connectionString: builder.Configuration.GetConnectionString("HangfireConnection")!,
                    name: "SqlServer-hangfire"
                )
                .AddRedis(
                    builder.Configuration.GetConnectionString("RedisConnection")!,
                    name: "redis"
                );

            // ---------------------
            // Firebase (FCM)
            // ---------------------
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

                    Console.WriteLine(" Firebase initialized successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" Firebase initialization failed: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine(" Firebase configuration not found");
            }

            var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";

            builder.Services.AddOpenTelemetry()
                .WithTracing(tracing => tracing
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("ftareqi-backend"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation()
                    .AddRedisInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    }));

            // ---------------------
            // Swagger
            // ---------------------
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Ftareqi API", Version = "v1" });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }

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

            // ═════════════════════════════════════════════
            // Build & configure the middleware pipeline
            // ═════════════════════════════════════════════
            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();
            }

            BackgroundJobsConfig.RegisterJobs(app);
            app.UseSerilogRequestLogging();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseHttpMetrics();
            app.UseExceptionHandler();
            app.UseHangfireDashboard("/hangfire");

            app.UseRouting();
            app.UseCors("FlexiblePolicy");
            app.UseAuthentication();
            app.UseMiddleware<TokenBucketMiddleware>();
            app.UseMiddleware<IdempotencyMiddleware>();
            app.UseAuthorization();

            app.MapHub<NotificationHub>("/notificationHub");
            app.MapHub<LiveTrackingHub>("/LiveTrackingHub");

            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapMetrics("/metrics");
            app.MapControllers();

            app.Run();
        }
    }
}