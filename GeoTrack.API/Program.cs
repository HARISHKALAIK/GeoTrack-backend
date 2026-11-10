using GeoTrack.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register Application Services
builder.Services.AddSingleton<MongoDBService>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<OtpService>();
builder.Services.AddSingleton<EmailService>();
var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();