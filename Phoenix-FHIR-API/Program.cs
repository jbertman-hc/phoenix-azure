using Microsoft.AspNetCore.Mvc;
using Phoenix_FHIR_API.Services;
using Phoenix_FHIR_API.Mappers;
using Phoenix_FHIR_API.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    });

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Phoenix FHIR API", Version = "v1" });
    c.EnableAnnotations();
});

// Register services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ILegacyApiService, LegacyApiService>();

// Register mappers
builder.Services.AddSingleton<IPatientFhirMapper, PatientFhirMapper>();
builder.Services.AddSingleton<IPractitionerFhirMapper, PractitionerFhirMapper>();
builder.Services.AddSingleton<IOrganizationFhirMapper, OrganizationFhirMapper>();
builder.Services.AddSingleton<ILocationFhirMapper, LocationFhirMapper>();
builder.Services.AddSingleton<IAllergyIntoleranceFhirMapper, AllergyIntoleranceFhirMapper>();
builder.Services.AddSingleton<IConditionFhirMapper, ConditionFhirMapper>();
builder.Services.AddSingleton<IMedicationStatementFhirMapper, MedicationStatementFhirMapper>();
builder.Services.AddSingleton<IDocumentReferenceFhirMapper, DocumentReferenceFhirMapper>();

// Register validators
builder.Services.AddSingleton<IFhirResourceValidator, FhirResourceValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
