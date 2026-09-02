using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TablerAuth.Application.IdentityProviders;
using TablerAuth.Domain.Identity;
using TablerAuth.Web.Models;

namespace TablerAuth.Web.Controllers;

[Authorize(Policy = AppPolicies.ManageProviders)]
public class IdentityProvidersController : Controller
{
    private readonly IIdentityProviderService _identityProviders;

    public IdentityProvidersController(IIdentityProviderService identityProviders)
    {
        _identityProviders = identityProviders;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        SetHeader(
            "Giriş sağlayıcıları",
            "Ek giriş düğmelerini — Google, GitHub, LinkedIn veya genel bir OpenID sağlayıcısı — açın veya kapatın.");

        var providers = await _identityProviders.ListAsync(cancellationToken);
        return View(providers);
    }

    [HttpGet]
    public IActionResult Create()
    {
        SetHeader(
            "Yeni sağlayıcı",
            "Bu bir giriş düğmesi ekler. Gizli User Secrets / ortam / Key Vault’ta kalır — burada yalnızca anahtar adı saklanır.");

        return View("Form", new IdentityProviderFormViewModel
        {
            Kinds = _identityProviders.ListKinds(),
            SortOrder = 10
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var input = await _identityProviders.FindAsync(id, cancellationToken);
        if (input is null)
        {
            return NotFound();
        }

        SetHeader(
            $"{input.DisplayName} düzenle",
            "Gizli User Secrets / ortam / Key Vault’ta kalır — burada yalnızca anahtar adı saklanır.");

        var model = IdentityProviderFormViewModel.FromInput(input);
        model.Kinds = _identityProviders.ListKinds();
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(IdentityProviderFormViewModel model, CancellationToken cancellationToken)
    {
        model.Kinds = _identityProviders.ListKinds();

        if (!ModelState.IsValid)
        {
            SetHeader(model.IsNew ? "Yeni sağlayıcı" : $"{model.DisplayName} düzenle", null);
            return View("Form", model);
        }

        var result = await _identityProviders.SaveAsync(model.ToInput(), cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            SetHeader(model.IsNew ? "Yeni sağlayıcı" : $"{model.DisplayName} düzenle", null);
            return View("Form", model);
        }

        TempData["StatusMessage"] = model.IsNew
            ? $"{model.DisplayName} eklendi. Giriş sayfasında göstermek için açın."
            : $"{model.DisplayName} güncellendi.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetEnabled(int id, bool enabled, CancellationToken cancellationToken)
    {
        var provider = await _identityProviders.FindAsync(id, cancellationToken);
        if (provider is null)
        {
            return NotFound();
        }

        await _identityProviders.SetEnabledAsync(id, enabled, cancellationToken);

        TempData["StatusMessage"] = enabled
            ? $"{provider.DisplayName} açık. Giriş sayfasında “{provider.DisplayName} ile giriş yap” görünecek."
            : $"{provider.DisplayName} kapalı. Düğmesi giriş sayfasında gizlendi.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var provider = await _identityProviders.FindAsync(id, cancellationToken);
        if (provider is null)
        {
            return NotFound();
        }

        await _identityProviders.DeleteAsync(id, cancellationToken);

        TempData["StatusMessage"] = $"{provider.DisplayName} bu listeden kaldırıldı.";

        return RedirectToAction(nameof(Index));
    }

    private void SetHeader(string title, string? description)
    {
        ViewData["Title"] = title;
        ViewData["Pretitle"] = "Yönetim";

        if (description is not null)
        {
            ViewData["Description"] = description;
        }
    }
}
