using Cause.SecurityManagement.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Cause.SecurityManagement.Models.Configuration;
using AwesomeAssertions;

namespace Cause.SecurityManagement.Tests.Controllers;

[TestFixture]
public class KeycloakConfigurationControllerTests
{
    private readonly string someUrl = "a really cool URL.";
    private readonly string someClientId = "a super client.";
    private readonly string someRealm = "a really bad realm...";
    private IOptions<KeycloakConfiguration> configuration;
    private KeycloakConfigurationController controller;

    [SetUp]
    public void Initialize()
    {
        configuration = Options.Create(new KeycloakConfiguration
        {
            Url = someUrl,
            ClientId = someClientId,
            Realm = someRealm
        });

        controller = new KeycloakConfigurationController(configuration);
    }

    [Test]
    public void KeycloakIsConfigured_WhenGettingConfiguration_ShouldReturnDtoConfigured()
    {
        var result = controller.GetKeycloakConfiguration();

        var expectedResult = new KeycloakConfigurationForWeb(someUrl, someRealm, someClientId);
        (result.Result as OkObjectResult)?.Value.Should().BeEquivalentTo(expectedResult);
    }

    [Test]
    public void KeycloakNotConfigured_WhenGettingConfiguration_ShouldBeUnauthorized()
    {
        controller = new KeycloakConfigurationController();
        
        var result = controller.GetKeycloakConfiguration();

        (result.Result as UnauthorizedObjectResult)?.Value.Should().Be("Keycloak not configured.");
    }
}