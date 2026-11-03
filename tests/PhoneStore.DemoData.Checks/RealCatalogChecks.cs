using System.Security.Cryptography;
using System.Xml.Linq;
using PhoneStore.DemoData.Cli;
using PhoneStore.DemoData.Generation;
using PhoneStore.DemoData.Verification;

namespace PhoneStore.DemoData.Checks;

internal static class RealCatalogChecks
{
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    public static void Run(Action<string, Action> check)
    {
        var date = new DateOnly(2026, 11, 3);
        var options = new Options(2611, date, "full", false, "unused", "unused") { Catalog = "real" };
        check("real catalog: 50 sourced models, 125 supported storage/color variants, 10 brands; small 6/15", () =>
        {
            foreach (var scale in new[] { "full", "small" })
            {
                var plan = DemoPlan.Create(options with { Scale = scale });
                Require(plan.Catalog == "real" && plan.Brands.Length == 10 && plan.Products.Length == (scale == "full" ? 50 : 6) && plan.Products.Sum(x => x.Variants.Length) == (scale == "full" ? 125 : 15), "Real catalog scale mismatch.");
                Require(plan.Products.Select(x => x.Name).Distinct().Count() == plan.Products.Length && plan.Products.SelectMany(x => x.Variants).Select(x => x.Sku).Distinct().Count() == plan.Products.Sum(x => x.Variants.Length), "Real names/SKUs repeated.");
                foreach (var p in plan.Products)
                {
                    var fact = RealCatalog.Products.Single(x => x.Code == p.Code);
                    Require(p.Name == fact.Name && plan.Brands[p.Brand] == fact.Brand && p.Specifications.Split('\n').Length == 9, "Real fact mapping mismatch.");
                    Require(p.Variants.All(v => fact.Storage.Contains(v.StorageGb) && fact.Colors.Contains(v.Color)) && p.Variants.Select(v => (v.StorageGb, v.Color)).Distinct().Count() == p.Variants.Length, "Invented or duplicate storage/color combination.");
                    Require(p.Name.Length <= 200 && p.Variants.All(v => v.Sku.Length <= 50 && v.Color.Length <= 50) && !p.Specifications.Contains("VX-") && !p.Specifications.Contains("Vela OS"), "Fictional/overlong real specification.");
                }
                Require(BusinessManifest.Hash(plan) == BusinessManifest.Hash(DemoPlan.Create(options with { Scale = scale })), "Real plan not deterministic.");
                Require(SalesTimeline.Create(plan).Sales.Count == (scale == "full" ? 601 : 60), "Real catalog chronological order quota.");
            }
        });
        check("real photos: 150 offline embedded source photographs, digest verified, no scripts/external image dependencies", () =>
        {
            var plan = DemoPlan.Create(options);
            var assets = SvgAssets.Render(plan);
            Require(assets.Count == 150 && assets.Values.Distinct().Count() == 150, "Real asset scale/uniqueness.");
            foreach (var product in RealCatalog.Products)
            {
                Require(product.Photos.Select(x => x.WrapperSha256).Distinct().Count() == 3, "Repeated image view.");
                foreach (var photo in product.Photos)
                {
                    Require(photo.Width >= 200 && photo.Height >= 200 && new Uri(photo.SourceUrl).Scheme == "https", "Photo source/size.");
                    var document = XDocument.Parse(assets[photo.File]);
                    XNamespace ns = "http://www.w3.org/2000/svg";
                    var image = document.Root!.Elements(ns + "image").Single();
                    var href = image.Attribute("href")!.Value;
                    Require(href.StartsWith("data:image/", StringComparison.Ordinal) && href.Contains(";base64,", StringComparison.Ordinal), "Photo depends on remote host.");
                    var bytes = Convert.FromBase64String(href[(href.IndexOf(',') + 1)..]);
                    Require(Convert.ToHexString(SHA256.HashData(bytes)).Equals(photo.Sha256, StringComparison.OrdinalIgnoreCase), "Original photograph bytes changed.");
                    Require(!document.Descendants().Any(x => x.Name.LocalName is "script" or "foreignObject") && document.Root.Element(ns + "title")!.Value.Contains(product.Name, StringComparison.Ordinal), "Unsafe/incorrect photo wrapper.");
                }
            }
        });
        check("real catalog CLI: explicit profile, invalid profile and pre-release history refused; original profile retained", () =>
        {
            string[] args = ["--seed-demo-large", "--confirm", "--images-dir", ".artifacts/images", "--output", ".artifacts/report.json"];
            Require(Options.Parse([.. args, "--catalog", "real"], date).Catalog == "real" && Options.Parse(args, date).Catalog == "fictional", "Catalog CLI routing.");
            foreach (var extra in new[] { new[] { "--catalog", "unknown" }, new[] { "--catalog", "real", "--as-of", "2026-09-30" } })
            {
                var refused = false;
                try { Options.Parse([.. args, .. extra], date); } catch (ArgumentException) { refused = true; }
                Require(refused, "Unsupported real profile accepted.");
            }
            Require(DemoPlan.Create(options with { Catalog = "fictional" }).Products[0].Code == "DM001", "Historical fictional profile changed.");
        });
    }
}
