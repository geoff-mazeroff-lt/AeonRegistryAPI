using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCustomSwagger();

// Configure for Postgres
var connectionString = DataUtility.GetConnectionString(builder.Configuration);
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // needed for images

app.MapGet("/api/welcome", () =>
    {
        var response = new
        {
            Message = "Welcome to the Aeon Registry API", 
            Version = "1.0.0",
            TimeOnly = DateTime.Now.ToString("T")
        };
        return Results.Ok(response);
    })
    .WithName("WelcomeMessage");

app.Run();