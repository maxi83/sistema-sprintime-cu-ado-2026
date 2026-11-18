using lucho_sprintime.Data;
using lucho_sprintime.Models;
using lucho_sprintime.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lucho_sprintime.Controllers;

public class CommunityController(ApplicationDbContext db, IWebHostEnvironment environment) : Controller
{
    [HttpGet("/novedades")]
    public async Task<IActionResult> News() =>
        View(await db.NewsPosts.AsNoTracking().Where(x => x.IsPublished)
            .OrderByDescending(x => x.CreatedAt).ToListAsync());

    [HttpGet("/novedades/{id:int}")]
    public async Task<IActionResult> NewsDetail(int id)
    {
        var article = await db.NewsPosts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsPublished);
        return article is null ? NotFound() : View(article);
    }

    [HttpGet("/auspiciantes")]
    public async Task<IActionResult> Sponsors() =>
        View(await db.CommunitySponsors.AsNoTracking().Where(x => x.IsPublished)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync());

    [Authorize(Roles = "Administrator,Organizer"), HttpGet]
    public async Task<IActionResult> ManageNews() =>
        View(await db.NewsPosts.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync());

    [Authorize(Roles = "Administrator,Organizer"), HttpGet]
    public async Task<IActionResult> EditNews(int? id)
    {
        if (!id.HasValue) return View(new NewsPostInput());
        var post = await db.NewsPosts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value);
        return post is null ? NotFound() : View(new NewsPostInput
        {
            Id = post.Id, Title = post.Title, Body = post.Body,
            ImageUrl = post.ImageUrl, IsPublished = post.IsPublished
        });
    }

    [Authorize(Roles = "Administrator,Organizer"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditNews(NewsPostInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title)) ModelState.AddModelError(nameof(input.Title), "Ingresá un título.");
        if (string.IsNullOrWhiteSpace(input.Body)) ModelState.AddModelError(nameof(input.Body), "Ingresá el texto.");
        if (!ValidImage(input.ImageFile)) ModelState.AddModelError(nameof(input.ImageFile), "Elegí una imagen JPG, PNG o WebP de hasta 5 MB.");
        if (!ModelState.IsValid) return View(input);

        NewsPost post;
        if (input.Id.HasValue)
        {
            post = await db.NewsPosts.FirstOrDefaultAsync(x => x.Id == input.Id.Value) ?? new NewsPost();
            if (post.Id == 0) return NotFound();
        }
        else
        {
            post = new NewsPost { CreatedAt = DateTime.UtcNow };
            db.NewsPosts.Add(post);
        }
        post.Title = input.Title.Trim();
        post.Body = input.Body.Trim();
        post.IsPublished = input.IsPublished;
        post.UpdatedAt = DateTime.UtcNow;
        if (input.RemoveImage) post.ImageUrl = null;
        if (input.ImageFile is { } file)
        {
            var url = await SaveImage(file, "news");
            if (url is null)
            {
                ModelState.AddModelError(nameof(input.ImageFile), "El contenido del archivo no es una imagen válida.");
                return View(input);
            }
            post.ImageUrl = url;
        }
        await db.SaveChangesAsync();
        TempData["Success"] = "Novedad guardada.";
        return RedirectToAction(nameof(ManageNews));
    }

    [Authorize(Roles = "Administrator,Organizer"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNews(int id)
    {
        var post = await db.NewsPosts.FindAsync(id);
        if (post is null) return NotFound();
        db.NewsPosts.Remove(post);
        await db.SaveChangesAsync();
        TempData["Success"] = "Novedad eliminada.";
        return RedirectToAction(nameof(ManageNews));
    }

    [Authorize(Roles = "Administrator,Organizer"), HttpGet]
    public async Task<IActionResult> ManageSponsors() =>
        View(await db.CommunitySponsors.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync());

    [Authorize(Roles = "Administrator,Organizer"), HttpGet]
    public async Task<IActionResult> EditSponsor(int? id)
    {
        if (!id.HasValue) return View(new CommunitySponsorInput());
        var sponsor = await db.CommunitySponsors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value);
        return sponsor is null ? NotFound() : View(new CommunitySponsorInput
        {
            Id = sponsor.Id, Name = sponsor.Name, Description = sponsor.Description,
            LogoUrl = sponsor.LogoUrl, LinkUrl = sponsor.LinkUrl,
            DisplayOrder = sponsor.DisplayOrder, IsPublished = sponsor.IsPublished
        });
    }

    [Authorize(Roles = "Administrator,Organizer"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSponsor(CommunitySponsorInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name)) ModelState.AddModelError(nameof(input.Name), "Ingresá el nombre.");
        if (!string.IsNullOrWhiteSpace(input.LinkUrl) &&
            (!Uri.TryCreate(input.LinkUrl.Trim(), UriKind.Absolute, out var uri) ||
             uri.Scheme != Uri.UriSchemeHttps))
            ModelState.AddModelError(nameof(input.LinkUrl), "El enlace debe comenzar con https://");
        if (!ValidImage(input.LogoFile)) ModelState.AddModelError(nameof(input.LogoFile), "Elegí una imagen JPG, PNG o WebP de hasta 5 MB.");
        if (input.IsPublished && input.LogoFile is null && (input.RemoveLogo || string.IsNullOrWhiteSpace(input.LogoUrl)))
            ModelState.AddModelError(nameof(input.LogoFile), "Para publicar un auspiciante, seleccioná su logo o fotografía.");
        if (!ModelState.IsValid) return View(input);

        CommunitySponsor sponsor;
        if (input.Id.HasValue)
        {
            sponsor = await db.CommunitySponsors.FirstOrDefaultAsync(x => x.Id == input.Id.Value) ?? new CommunitySponsor();
            if (sponsor.Id == 0) return NotFound();
        }
        else
        {
            sponsor = new CommunitySponsor();
            db.CommunitySponsors.Add(sponsor);
        }
        sponsor.Name = input.Name.Trim();
        sponsor.Description = input.Description?.Trim();
        sponsor.LinkUrl = string.IsNullOrWhiteSpace(input.LinkUrl) ? null : input.LinkUrl.Trim();
        sponsor.DisplayOrder = input.DisplayOrder;
        sponsor.IsPublished = input.IsPublished;
        if (input.RemoveLogo) sponsor.LogoUrl = null;
        if (input.LogoFile is { } file)
        {
            var url = await SaveImage(file, "sponsors");
            if (url is null)
            {
                ModelState.AddModelError(nameof(input.LogoFile), "El contenido del archivo no es una imagen válida.");
                return View(input);
            }
            sponsor.LogoUrl = url;
        }
        await db.SaveChangesAsync();
        TempData["Success"] = "Auspiciante guardado.";
        return RedirectToAction(nameof(ManageSponsors));
    }

    [Authorize(Roles = "Administrator,Organizer"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSponsor(int id)
    {
        var sponsor = await db.CommunitySponsors.FindAsync(id);
        if (sponsor is null) return NotFound();
        db.CommunitySponsors.Remove(sponsor);
        await db.SaveChangesAsync();
        TempData["Success"] = "Auspiciante eliminado.";
        return RedirectToAction(nameof(ManageSponsors));
    }

    private static bool ValidImage(IFormFile? file)
    {
        if (file is null) return true;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return file.Length > 0 && file.Length <= 5 * 1024 * 1024 &&
            ext is ".jpg" or ".jpeg" or ".png" or ".webp";
    }

    private async Task<string?> SaveImage(IFormFile file, string folderName)
    {
        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = await stream.ReadAsync(header);
        var jpg = read >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff;
        var png = read >= 8 && header.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var webp = read >= 12 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" &&
                   System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP";
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!((jpg && ext is ".jpg" or ".jpeg") || (png && ext == ".png") || (webp && ext == ".webp")))
            return null;
        var folder = Path.Combine(environment.WebRootPath, "uploads", folderName);
        Directory.CreateDirectory(folder);
        var name = Guid.NewGuid().ToString("N") + (jpg ? ".jpg" : png ? ".png" : ".webp");
        stream.Position = 0;
        await using (var output = System.IO.File.Create(Path.Combine(folder, name)))
            await stream.CopyToAsync(output);
        return "/uploads/" + folderName + "/" + name;
    }
}
