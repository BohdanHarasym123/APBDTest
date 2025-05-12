using APBDTest.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddScoped<IDbService, DbService>();

var app = builder.Build();

app.MapControllers();
app.Run();