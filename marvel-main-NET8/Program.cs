
using marvel_main_NET8.Models;
using Microsoft.EntityFrameworkCore;

namespace marvel_main_NET8
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            // Add DbContext using environment variable
            var connectionString_SCRM = Environment.GetEnvironmentVariable("ConnectionString_SCRM");
            if (string.IsNullOrEmpty(connectionString_SCRM))
            {
                throw new InvalidOperationException("The 'ConnectionString_XXXXXX' environment variable is not set.");
            }


            // Add DbContext
            builder.Services.AddDbContext<ScrmDbContext>(options =>
                options.UseSqlServer(connectionString_SCRM));


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                //   UseSwagger

            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
