using Microsoft.OpenApi.Models;
using Phoenix_AzureAPI.Services;
using Phoenix_AzureAPI.Services.FHIR;
using Phoenix_AzureAPI.Controllers;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use port 5300
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5300);
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Phoenix-Azure API", Version = "v1" });
});

// Register application services
builder.Services.AddScoped<PatientDataService>();

// Register FHIR services
builder.Services.AddScoped<IFhirService, FhirService>();
builder.Services.AddScoped<IPatientFhirMapper, PatientFhirMapper>();
builder.Services.AddScoped<IPracticeFhirMapper, PracticeFhirMapper>();
builder.Services.AddScoped<PatientController>();

// Register Practice service
builder.Services.AddScoped<IPracticeService, PracticeService>();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Disposition");
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply CORS before other middleware
app.UseCors("AllowAll");

// Serve static files from wwwroot
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
