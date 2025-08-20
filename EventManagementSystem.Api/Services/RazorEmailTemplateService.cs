using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core.DTOs.Email;
using System.Text;

namespace EventManagementSystem.Api.Services
{
    public class RazorEmailTemplateService : IEmailTemplateService
    {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RazorEmailTemplateService> _logger;

        public RazorEmailTemplateService(
            IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider,
            ILogger<RazorEmailTemplateService> logger)
        {
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<string> RenderTemplateAsync<T>(string templateName, T model) where T : BaseEmailModel
        {
            try
            {
                _logger.LogInformation("🎨 Rendering Razor template: {TemplateName}", templateName);

                using var scope = _serviceProvider.CreateScope();
                
                // Create a mock ActionContext for Razor rendering
                var httpContext = new DefaultHttpContext
                {
                    RequestServices = scope.ServiceProvider
                };

                var actionContext = new ActionContext(
                    httpContext,
                    new RouteData(),
                    new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
                );

                // Find the view using the correct method for Razor view engine
                // Use GetView instead of FindView for explicit paths
                ViewEngineResult? viewResult = null;
                
                // Try using GetView with the exact path first
                var exactPath = $"/EmailTemplates/{templateName}.cshtml";
                viewResult = _razorViewEngine.GetView("", exactPath, false);
                
                if (viewResult.Success)
                {
                    _logger.LogInformation("✅ Found template using exact path: {ViewPath}", exactPath);
                }
                else
                {
                    // Fallback to FindView with just the template name
                    viewResult = _razorViewEngine.FindView(actionContext, templateName, false);
                    if (viewResult.Success)
                    {
                        _logger.LogInformation("✅ Found template using FindView: {TemplateName}", templateName);
                    }
                }

                if (!viewResult.Success)
                {
                    var searchedLocations = string.Join(", ", viewResult.SearchedLocations ?? new string[0]);
                    var errorMessage = $"Template '{templateName}' not found. Searched locations: {searchedLocations}";
                    _logger.LogError("❌ {ErrorMessage}", errorMessage);
                    throw new ArgumentException(errorMessage);
                }

                _logger.LogInformation("✅ Found Razor template at: {ViewName}", viewResult.View.Path);

                // Render the view to string
                using var stringWriter = new StringWriter();
                
                var viewData = new ViewDataDictionary(
                    new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                    new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                {
                    Model = model
                };

                var tempData = new TempDataDictionary(actionContext.HttpContext, _tempDataProvider);

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewData,
                    tempData,
                    stringWriter,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                var html = stringWriter.ToString();

                _logger.LogInformation("✅ Razor template rendered successfully. Length: {Length}", html.Length);
                return html;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error rendering Razor template {TemplateName}", templateName);
                throw;
            }
        }

        public Task<bool> ValidateTemplateAsync(string templateName)
        {
            try
            {
                var templatePath = Path.Combine("EmailTemplates", $"{templateName}.cshtml");
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), templatePath);
                var exists = File.Exists(fullPath);
                
                _logger.LogInformation("Template validation for {TemplateName}: {Exists}", templateName, exists);
                return Task.FromResult(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating template {TemplateName}", templateName);
                return Task.FromResult(false);
            }
        }

        public List<string> GetAvailableTemplates()
        {
            try
            {
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates");
                
                if (!Directory.Exists(templatePath))
                {
                    _logger.LogWarning("EmailTemplates directory not found at: {Path}", templatePath);
                    return new List<string>();
                }

                var templates = Directory.GetFiles(templatePath, "*.cshtml")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .ToList();

                _logger.LogInformation("Found {Count} email templates: {Templates}", 
                    templates.Count, string.Join(", ", templates));

                return templates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available templates");
                return new List<string>();
            }
        }
    }
}