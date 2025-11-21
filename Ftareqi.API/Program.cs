using Ftareqi.Application.Interfaces.Repositories;
using Ftareqi.Domain.Models;
using Ftareqi.Persistence;
using Ftareqi.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
namespace Ftareqi.API
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);


			//inject GlobalErrorHandler
			builder.Services.AddExceptionHandler<GlobalErrorHandler>();
			builder.Services.AddProblemDetails();

			// configuration for Seq 
			builder.Host.UseSerilog((context, configuration) =>
				configuration.ReadFrom.Configuration(context.Configuration));

			// configuration for Sql Server Connection
			builder.Services.AddDbContext<ApplicationDbContext>(options =>
				options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

			//configuration for identity
			builder.Services.AddIdentity<User, IdentityRole>()
			.AddEntityFrameworkStores<ApplicationDbContext>()
			.AddDefaultTokenProviders();

			//builder.Services.AddExceptionHandler<GlobalErrorHandler>();
			// Add services to the container.
			builder.Services.AddControllers();

			// Swagger configuration
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen(options =>
			{
				options.SwaggerDoc("v1", new OpenApiInfo
				{
					Version = "v1",
					Title = "Ftareqi API",
					Description = "An ASP.NET Core Web API for Carpooling"
				});

				var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
				options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
			});

			builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
			builder.Services.AddScoped<IUnitOfWork,UnitOfWork>();

			var app = builder.Build();

			//add ExceptionHandler to pipeline 
			app.UseExceptionHandler();
			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();//IApplicationBuilder
			app.UseAuthorization();
			app.MapControllers();//EndPointBuilder

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
