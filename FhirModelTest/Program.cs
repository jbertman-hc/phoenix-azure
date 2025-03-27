using Hl7.Fhir.Model;
using System.Reflection;

// Create a test program to understand the FHIR model structure
Console.WriteLine("FHIR Model Test Program");
Console.WriteLine("======================");

// Examine the ResourceComponent class
Console.WriteLine("\nExamining CapabilityStatement.ResourceComponent:");
var resourceComponentType = typeof(CapabilityStatement.ResourceComponent);
var typeProperty = resourceComponentType.GetProperty("Type");
var profileProperty = resourceComponentType.GetProperty("Profile");
var documentationProperty = resourceComponentType.GetProperty("Documentation");

Console.WriteLine($"Type property: {typeProperty?.PropertyType.FullName}");
Console.WriteLine($"Profile property: {profileProperty?.PropertyType.FullName}");
Console.WriteLine($"Documentation property: {documentationProperty?.PropertyType.FullName}");

// Try to create a ResourceComponent with the correct types
try
{
    Console.WriteLine("\nCreating a ResourceComponent with the correct types:");
    var resourceComponent = new CapabilityStatement.ResourceComponent();
    
    // Directly access the property to see what it is
    var type = resourceComponent.Type;
    Console.WriteLine($"Default Type property value: {type}, Type: {type.GetType().FullName}");
    
    // Try to set the Type property using reflection
    Console.WriteLine("Setting Type property using reflection");
    typeProperty?.SetValue(resourceComponent, ResourceType.Patient);
    Console.WriteLine($"Type property set successfully: {resourceComponent.Type}");
    
    // Directly access the Profile property to see what it is
    var profile = resourceComponent.Profile;
    Console.WriteLine($"Default Profile property value: {(profile == null ? "null" : string.Join(", ", profile))}, Type: {(profile == null ? "null" : profile.GetType().FullName)}");
    
    // Try to set the Profile property using reflection
    Console.WriteLine("Setting Profile property using reflection");
    profileProperty?.SetValue(resourceComponent, new string[] { "http://hl7.org/fhir/StructureDefinition/Patient" });
    Console.WriteLine($"Profile property set successfully: {string.Join(", ", resourceComponent.Profile)}");
    
    // Directly access the Documentation property to see what it is
    var documentation = resourceComponent.Documentation;
    Console.WriteLine($"Default Documentation property value: {documentation}, Type: {(documentation == null ? "null" : documentation.GetType().FullName)}");
    
    // Try to set the Documentation property using reflection
    Console.WriteLine("Setting Documentation property using reflection");
    documentationProperty?.SetValue(resourceComponent, new Markdown("This is a test documentation"));
    Console.WriteLine($"Documentation property set successfully: {resourceComponent.Documentation}");
    
    Console.WriteLine("ResourceComponent created successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"Error creating ResourceComponent: {ex.Message}");
}

// Check the version of the FHIR library
var assembly = Assembly.GetAssembly(typeof(CapabilityStatement));
Console.WriteLine($"\nFHIR Library Version: {assembly?.GetName().Version}");

// Let's also check the ResourceType enum
Console.WriteLine("\nExamining ResourceType enum:");
Console.WriteLine($"ResourceType.Patient value: {(int)ResourceType.Patient}");
Console.WriteLine($"ResourceType.Patient string representation: {ResourceType.Patient}");

Console.WriteLine("\nTest completed successfully");
