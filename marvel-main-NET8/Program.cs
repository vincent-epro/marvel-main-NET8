
using marvel_main_NET8.Models;
using Microsoft.EntityFrameworkCore;

namespace marvel_main_NET8
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
      //      builder.Services.AddEndpointsApiExplorer();
      //      builder.Services.AddSwaggerGen();


            // Add DbContext
            builder.Services.AddDbContext<ScrmDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
           //     app.UseSwagger();
           //     app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
