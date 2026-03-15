using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Register encoding provider for Windows-1255 and other code pages
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS to allow the frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BLS Signature API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors();
app.UseStaticFiles(); // Serve HTML files from wwwroot
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// Redirect root to index.html
app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();
