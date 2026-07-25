var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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