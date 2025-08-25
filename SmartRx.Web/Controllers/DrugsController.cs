using Microsoft.AspNetCore.Mvc;
using SmartRx.Domain;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using SmartRx.Web.Helpers;

namespace SmartRx.Web.Controllers;

public class DrugsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DrugsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // 🔹 List all drugs
    public async Task<IActionResult> Index()
    {
        var client = CreateClient();
        var drugs = await client.GetFromJsonAsync<List<Drug>>("http://localhost:5002/api/drugs");
        return View(drugs);
    }

    // 🔹 Create (GET)
    [HttpGet]
    public IActionResult Create() => View();

    // 🔹 Create (POST)
    [HttpPost]
    public async Task<IActionResult> Create(Drug drug)
    {
        var client = CreateClient();
        var json = JsonSerializer.Serialize(drug);
        var res = await client.PostAsync("http://localhost:5002/api/drugs",
            new StringContent(json, Encoding.UTF8, "application/json"));

        if (res.IsSuccessStatusCode)
            return RedirectToAction("Index");

        ModelState.AddModelError("", "Failed to create drug. " + await res.Content.ReadAsStringAsync());
        return View(drug);
    }

    // 🔹 Edit (GET)
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var client = CreateClient();
        var drug = await client.GetFromJsonAsync<Drug>($"http://localhost:5002/api/drugs/{id}");
        if (drug == null) return NotFound();
        return View(drug);
    }

    // 🔹 Edit (POST)
    [HttpPost]
    public async Task<IActionResult> Edit(Drug drug)
    {
        var client = CreateClient();
        var json = JsonSerializer.Serialize(drug);
        var res = await client.PutAsync($"http://localhost:5002/api/drugs/{drug.Id}",
            new StringContent(json, Encoding.UTF8, "application/json"));

        if (res.IsSuccessStatusCode)
            return RedirectToAction("Index");

        ModelState.AddModelError("", "Failed to update drug. " + await res.Content.ReadAsStringAsync());
        return View(drug);
    }

    // 🔹 Delete
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var client = CreateClient();
        var res = await client.DeleteAsync($"http://localhost:5002/api/drugs/{id}");
        if (!res.IsSuccessStatusCode)
        {
            TempData["Error"] = "Failed to delete drug. " + await res.Content.ReadAsStringAsync();
        }
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Search(string? query)
    {
        var client = CreateClient();

        // Always fetch all drugs for the All Drugs tab
        var allDrugs = await client.GetFromJsonAsync<List<Drug>>("http://localhost:5002/api/drugs");

        // Fetch search results if query is provided
        List<Drug>? searchResults = null;
        if (!string.IsNullOrWhiteSpace(query))
        {
            searchResults = await client.GetFromJsonAsync<List<Drug>>(
                $"http://localhost:5002/api/drugs/search?query={Uri.EscapeDataString(query)}"
            );
        }

        ViewData["SearchResults"] = searchResults ?? new List<Drug>();
        ViewData["ActiveTab"] = "search";

        return View("Index", allDrugs);
    }


    // 🔹 Helper: attach JWT if logged in
    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient();
        var token = HttpContext.Session.GetString(SessionKeys.Jwt);
        Console.WriteLine("JWT: " + token);
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
        return client;
    }
}
