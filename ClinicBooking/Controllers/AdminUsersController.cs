using ClinicBooking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.Controllers;

[Authorize(Roles = "Admin")]
public class AdminUsersController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;

    public AdminUsersController(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles)
    {
        _users = users;
        _roles = roles;
    }

    // GET: /AdminUsers
    public async Task<IActionResult> Index()
    {
        // Ensure role exists
        if (!await _roles.RoleExistsAsync("Admin"))
            await _roles.CreateAsync(new IdentityRole("Admin"));

        var all = _users.Users.ToList();
        var admins = new List<ApplicationUser>();
        foreach (var u in all)
            if (await _users.IsInRoleAsync(u, "Admin"))
                admins.Add(u);

        ViewBag.Count = admins.Count;
        return View(admins.OrderBy(a => a.FullName ?? a.Email).ToList());
    }

    // POST: /AdminUsers/PromoteExisting
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteExisting(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["err"] = "Email is required.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _users.FindByEmailAsync(email);
        if (user == null)
        {
            TempData["err"] = $"No user found with email {email}.";
            return RedirectToAction(nameof(Index));
        }

        if (!await _roles.RoleExistsAsync("Admin"))
            await _roles.CreateAsync(new IdentityRole("Admin"));

        if (await _users.IsInRoleAsync(user, "Admin"))
        {
            TempData["msg"] = $"{email} is already an admin.";
            return RedirectToAction(nameof(Index));
        }

        await _users.AddToRoleAsync(user, "Admin");
        TempData["msg"] = $"{email} promoted to Admin.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /AdminUsers/Create  (create a brand-new admin account)
    public IActionResult Create() => View(new CreateAdminVm());

    // POST: /AdminUsers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAdminVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        if (!await _roles.RoleExistsAsync("Admin"))
            await _roles.CreateAsync(new IdentityRole("Admin"));

        var existing = await _users.FindByEmailAsync(vm.Email);
        if (existing != null)
        {
            ModelState.AddModelError("", "Email already exists.");
            return View(vm);
        }

        var user = new ApplicationUser
        {
            Email = vm.Email,
            UserName = vm.Email,
            FullName = vm.FullName,
            EmailConfirmed = true,
            IsActive = true
        };

        var create = await _users.CreateAsync(user, vm.Password);
        if (!create.Succeeded)
        {
            foreach (var e in create.Errors) ModelState.AddModelError("", e.Description);
            return View(vm);
        }

        await _users.AddToRoleAsync(user, "Admin");
        TempData["msg"] = $"Admin {vm.Email} created.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /AdminUsers/Demote/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Demote(string id)
    {
        var me = await _users.GetUserAsync(User);
        var user = await _users.FindByIdAsync(id);
        if (user == null) { TempData["err"] = "User not found."; return RedirectToAction(nameof(Index)); }

        // Don't let me demote myself
        if (me!.Id == user.Id)
        {
            TempData["err"] = "You cannot demote yourself.";
            return RedirectToAction(nameof(Index));
        }

        // Don't demote the last remaining admin
        var admins = new List<ApplicationUser>();
        foreach (var u in _users.Users)
            if (await _users.IsInRoleAsync(u, "Admin")) admins.Add(u);
        if (admins.Count <= 1)
        {
            TempData["err"] = "Cannot demote the last admin.";
            return RedirectToAction(nameof(Index));
        }

        if (await _users.IsInRoleAsync(user, "Admin"))
            await _users.RemoveFromRoleAsync(user, "Admin");

        TempData["msg"] = $"Removed Admin role from {user.Email}.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /AdminUsers/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var me = await _users.GetUserAsync(User);
        var user = await _users.FindByIdAsync(id);
        if (user == null) { TempData["err"] = "User not found."; return RedirectToAction(nameof(Index)); }

        if (me!.Id == user.Id)
        {
            TempData["err"] = "You cannot delete yourself.";
            return RedirectToAction(nameof(Index));
        }

        // If this is an admin, ensure not the last
        if (await _users.IsInRoleAsync(user, "Admin"))
        {
            var admins = new List<ApplicationUser>();
            foreach (var u in _users.Users)
                if (await _users.IsInRoleAsync(u, "Admin")) admins.Add(u);
            if (admins.Count <= 1)
            {
                TempData["err"] = "Cannot delete the last admin.";
                return RedirectToAction(nameof(Index));
            }
        }

        var res = await _users.DeleteAsync(user);
        TempData[res.Succeeded ? "msg" : "err"] = res.Succeeded ? $"Deleted {user.Email}." : string.Join("; ", res.Errors.Select(e => e.Description));
        return RedirectToAction(nameof(Index));
    }
}

public class CreateAdminVm
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "Pass123!"; // default; change on create
}
