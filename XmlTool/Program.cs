using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();   // ⭐ VERY IMPORTANT
app.UseStaticFiles();    // ⭐ VERY IMPORTANT

app.MapPost("/formatxml", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var xml = await reader.ReadToEndAsync();

    try
    {
        var doc = XDocument.Parse(xml);
        return Results.Text(doc.ToString(), "text/plain");
    }
    catch (Exception ex)
    {
        return Results.Text("Invalid XML: " + ex.Message, "text/plain");
    }
});
app.MapPost("/comparexml", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();

    var parts = body.Split("|||");

    if (parts.Length != 2)
        return Results.Text("Invalid Input", "text/plain");

    try
    {
        var docA = XDocument.Parse(parts[0]);
        var docB = XDocument.Parse(parts[1]);

        var diffs = new List<string>();

        CompareElements(docA.Root, docB.Root, diffs, "/");

        if (diffs.Count == 0)
        {
            return Results.Json(new
            {
                xmlA = docA.ToString(),
                xmlB = docB.ToString(),
                diffs = new List<string> { "XML are identical" }
            });
        }

        return Results.Json(new
        {
            xmlA = docA.ToString(),
            xmlB = docB.ToString(),
            diffs = diffs
        });
    }
    catch (Exception ex)
    {
        return Results.Text("Error: " + ex.Message, "text/plain");
    }
});

void CompareElements(XElement a, XElement b, List<string> diffs, string path)
{
    if (a.Name != b.Name)
        diffs.Add($"Node name mismatch at {path}: {a.Name} vs {b.Name}");

    if (a.Value != b.Value && !a.HasElements && !b.HasElements)
        diffs.Add($"Value changed at {path}/{a.Name}: {a.Value} vs {b.Value}");

    var aChildren = a.Elements().ToList();
    var bChildren = b.Elements().ToList();

    int max = Math.Max(aChildren.Count, bChildren.Count);

    for (int i = 0; i < max; i++)
    {
        if (i >= aChildren.Count)
        {
            diffs.Add($"Extra node in B: {bChildren[i].Name}");
            continue;
        }

        if (i >= bChildren.Count)
        {
            diffs.Add($"Missing node in B: {aChildren[i].Name}");
            continue;
        }

        CompareElements(aChildren[i], bChildren[i], diffs, path + "/" + a.Name);
    }
}

app.Run();