using System.Globalization;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using Reqnroll;
using Tfl.DummyJson.ApiTests.Support;

namespace Tfl.DummyJson.ApiTests.Steps;

[Binding]
public sealed class ApiSteps(ApiWorld world)
{
    private const string Username = "emilys";
    private const string Password = "emilyspass";
    private JsonElement _fullUsers;

    [Given("the DummyJSON API is available")]
    public void GivenApiAvailable()
    {
        Assert.That(world.BaseUri.Scheme, Is.EqualTo(Uri.UriSchemeHttps).Or.EqualTo(Uri.UriSchemeHttp));
    }

    [When("I authenticate as the sample user")]
    public async Task WhenAuthenticate()
    {
        await world.SendAsync(HttpMethod.Post, "auth/login", new { username = Username, password = Password, expiresInMins = 30 });
        if (world.Response?.IsSuccessStatusCode == true && world.Root.TryGetProperty("accessToken", out var token))
            world.AccessToken = token.GetString();
    }

    [Given("I am authenticated as the sample user")]
    public async Task GivenAuthenticated()
    {
        await WhenAuthenticate();
        AssertSuccess();
        Assert.That(world.AccessToken, Is.Not.Null.And.Not.Empty);
    }

    [When("I request the current authenticated user")]
    public Task WhenGetMe() => world.SendAsync(HttpMethod.Get, "auth/me", authenticated: true);

    [When("I retrieve all users and users filtered by red hair")]
    public async Task WhenGetRedHairUsers()
    {
        await world.SendAsync(HttpMethod.Get, "users?limit=0");
        _fullUsers = world.Root.GetProperty("users").Clone();
        await world.SendAsync(HttpMethod.Get, "users/filter?key=hair.color&value=Red&limit=0");
    }

    [When("I add a user with a complete address")]
    public Task WhenAddUser()
    {
        var payload = new
        {
            firstName = "Alex",
            lastName = "Taylor",
            age = 34,
            gender = "other",
            hair = new { color = "Red", type = "Wavy" },
            address = new
            {
                address = "10 Example Street",
                city = "London",
                state = "Greater London",
                stateCode = "GL",
                postalCode = "SE10 0AA",
                coordinates = new { lat = 51.5007, lng = -0.0033 },
                country = "United Kingdom"
            }
        };
        return world.SendAsync(HttpMethod.Post, "users/add", payload);
    }

    [When("I search for {string}")]
    public Task WhenSearchProduct(string term) => world.SendAsync(HttpMethod.Get, $"products/search?q={Uri.EscapeDataString(term)}");

    [When("I request one product starting at offset {int}")]
    public Task WhenGetPagedProduct(int offset) => world.SendAsync(HttpMethod.Get, $"products?limit=1&skip={offset}");

    [When("I update product {int} with title {string}")]
    public Task WhenUpdateProduct(int id, string title) => world.SendAsync(HttpMethod.Put, $"products/{id}", new { title });

    [When("I add five products to a cart")]
    public Task WhenAddCart() => world.SendAsync(HttpMethod.Post, "carts/add", new
    {
        userId = 1,
        products = new[]
        {
            new { id = 1, quantity = 1 },
            new { id = 2, quantity = 1 },
            new { id = 3, quantity = 1 },
            new { id = 4, quantity = 1 },
            new { id = 5, quantity = 1 }
        }
    });

    [When("I delete cart {int}")]
    public Task WhenDeleteCart(int id) => world.SendAsync(HttpMethod.Delete, $"carts/{id}");

    [Then("the response is successful")]
    public void ThenResponseSuccessful() => AssertSuccess();

    [Then("the access token is stored")]
    public void ThenTokenStored() => Assert.That(world.AccessToken, Is.Not.Null.And.Not.Empty);

    [Then("the authenticated user has id {int}, first name {string}, and last name {string}")]
    public void ThenAuthUser(int id, string firstName, string lastName)
    {
        Assert.That(world.Root.GetProperty("id").GetInt32(), Is.EqualTo(id));
        Assert.That(world.Root.GetProperty("firstName").GetString(), Is.EqualTo(firstName));
        Assert.That(world.Root.GetProperty("lastName").GetString(), Is.EqualTo(lastName));
    }

    [Then("the red hair filter returns the same users as the full user list")]
    public void ThenRedHairCountMatches()
    {
        var expected = _fullUsers.EnumerateArray()
            .Where(user => user.GetProperty("hair").GetProperty("color").GetString()?.Equals("Red", StringComparison.OrdinalIgnoreCase) == true)
            .Select(user => user.GetProperty("id").GetInt32()).Order().ToArray();
        var filtered = world.Root.GetProperty("users").EnumerateArray()
            .Select(user => user.GetProperty("id").GetInt32()).Order().ToArray();
        Assert.That(world.Root.GetProperty("total").GetInt32(), Is.EqualTo(expected.Length), "The filtered total reports the number of red-haired users.");
        Assert.That(filtered, Is.EqualTo(expected), $"Expected {expected.Length} users with red hair.");
    }

    [Then("the created user response matches the submitted details")]
    public void ThenUserCreated()
    {
        var user = world.Root;
        Assert.That(user.GetProperty("firstName").GetString(), Is.EqualTo("Alex"));
        Assert.That(user.GetProperty("lastName").GetString(), Is.EqualTo("Taylor"));
        Assert.That(user.GetProperty("age").GetInt32(), Is.EqualTo(34));
        Assert.That(user.GetProperty("gender").GetString(), Is.EqualTo("other"));
        Assert.That(user.GetProperty("hair").GetProperty("color").GetString(), Is.EqualTo("Red"));
        Assert.That(user.GetProperty("hair").GetProperty("type").GetString(), Is.EqualTo("Wavy"));
        var address = user.GetProperty("address");
        Assert.That(address.GetProperty("address").GetString(), Is.EqualTo("10 Example Street"));
        Assert.That(address.GetProperty("city").GetString(), Is.EqualTo("London"));
        Assert.That(address.GetProperty("state").GetString(), Is.EqualTo("Greater London"));
        Assert.That(address.GetProperty("stateCode").GetString(), Is.EqualTo("GL"));
        Assert.That(address.GetProperty("postalCode").GetString(), Is.EqualTo("SE10 0AA"));
        Assert.That(address.GetProperty("country").GetString(), Is.EqualTo("United Kingdom"));
        Assert.That(address.GetProperty("coordinates").GetProperty("lat").GetDouble(), Is.EqualTo(51.5007).Within(0.0001));
        Assert.That(address.GetProperty("coordinates").GetProperty("lng").GetDouble(), Is.EqualTo(-0.0033).Within(0.0001));
    }

    [Then("the iPhone 13 Pro price is {double}")]
    public void ThenIphonePrice(double expectedPrice)
    {
        var product = world.Root.GetProperty("products").EnumerateArray()
            .Single(item => item.GetProperty("title").GetString()?.Contains("iPhone 13 Pro", StringComparison.OrdinalIgnoreCase) == true);
        Assert.That(product.GetProperty("price").GetDouble(), Is.EqualTo(expectedPrice).Within(0.001));
    }

    [Then("the response contains only product {int} {string} priced {double}")]
    public void ThenOnlyProduct(int id, string title, double price)
    {
        var products = world.Root.GetProperty("products").EnumerateArray().ToArray();
        Assert.That(products, Has.Length.EqualTo(1));
        Assert.That(products[0].GetProperty("id").GetInt32(), Is.EqualTo(id));
        Assert.That(products[0].GetProperty("title").GetString(), Is.EqualTo(title));
        Assert.That(products[0].GetProperty("price").GetDouble(), Is.EqualTo(price).Within(0.001));
        Assert.That(world.Root.GetProperty("limit").GetInt32(), Is.EqualTo(1));
        Assert.That(world.Root.GetProperty("skip").GetInt32(), Is.EqualTo(14));
    }

    [Then("the updated product title is {string}")]
    public void ThenProductUpdated(string title)
    {
        Assert.That(world.Root.GetProperty("id").GetInt32(), Is.EqualTo(121));
        Assert.That(world.Root.GetProperty("title").GetString(), Is.EqualTo(title));
    }

    [Then("the cart contains five products and a valid aggregate discount percentage")]
    public void ThenCartDiscount()
    {
        var products = world.Root.GetProperty("products").EnumerateArray().ToArray();
        Assert.That(products, Has.Length.EqualTo(5));
        var total = world.Root.GetProperty("total").GetDecimal();
        var discountedTotal = world.Root.GetProperty("discountedTotal").GetDecimal();
        Assert.That(total, Is.GreaterThan(0));
        Assert.That(discountedTotal, Is.GreaterThanOrEqualTo(0).And.LessThanOrEqualTo(total));
        var discountPercentage = (total - discountedTotal) / total * 100m;
        Assert.That(discountPercentage, Is.InRange(0m, 100m));
        TestContext.Out.WriteLine($"Aggregate discount percentage: {discountPercentage.ToString("F2", CultureInfo.InvariantCulture)}%");
    }

    [Then("the cart deletion is marked as simulated and successful")]
    public void ThenCartDeleted()
    {
        Assert.That(world.Root.GetProperty("id").GetInt32(), Is.EqualTo(1));
        Assert.That(world.Root.GetProperty("isDeleted").GetBoolean(), Is.True);
        Assert.That(world.Root.TryGetProperty("deletedOn", out var deletedOn), Is.True);
        Assert.That(deletedOn.ValueKind, Is.Not.EqualTo(JsonValueKind.Null));
    }

    private void AssertSuccess()
    {
        Assert.That(world.Response, Is.Not.Null);
        Assert.That(world.Response!.StatusCode, Is.EqualTo(HttpStatusCode.OK).Or.EqualTo(HttpStatusCode.Created));
    }
}
