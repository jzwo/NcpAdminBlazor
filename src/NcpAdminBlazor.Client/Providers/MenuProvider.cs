using MudBlazor;
using NcpAdminBlazor.Client.Pages;
using NcpAdminBlazor.Client.Pages.Applications.Email;
using NcpAdminBlazor.Client.Pages.Applications.Role;
using NcpAdminBlazor.Client.Pages.Applications.User;
using NcpAdminBlazor.Client.Pages.Personal;

namespace NcpAdminBlazor.Client.Providers;

public partial class MenuProvider
{
    private readonly List<MenuItem> _menuItems;

    public IReadOnlyList<MenuItem> MenuItems => _menuItems.AsReadOnly();

    public MenuProvider()
    {
        var builder = new MenuBuilder();

        builder
            .AddLink("首页", Home.PageUri, Icons.Material.Outlined.Airplay)
            .AddGroup("系统管理", Icons.Material.Outlined.Settings, system =>
            {
                system.AddLink("用户管理", UserList.PageUri, Icons.Material.Outlined.ManageAccounts);
                system.AddLink("角色管理", RoleList.PageUri, Icons.Material.Outlined.AdminPanelSettings);
            })
            .AddGroup("App Examples", @Icons.Material.Outlined.DynamicFeed, products =>
            {
                products.AddLink("仪表盘", Dashboard.PageUri, Icons.Material.Outlined.Dashboard);
                products.AddLink("Email", Email.PageUri, Icons.Material.Outlined.Email);
                products.AddLink("AI Chat", "/chat", Icons.Material.Outlined.Chat);
            });

        _menuItems = builder.Items;
    }

    /// <summary>
    /// 根据 href 查找最匹配的菜单项
    /// </summary>
    public MenuItem? FindMenuItemByHref(string href)
    {
        return FindMenuItemByHref(MenuItems, href);
    }

    /// <summary>
    /// 生成路由的备用标题（当菜单中找不到时使用）
    /// </summary>
    public static string GenerateFallbackTitle(string route)
    {
        var lastSegment = route.Split('/').LastOrDefault(s => !string.IsNullOrEmpty(s));
        // 将 "PageName" 转换为 "Page Name"
        return MyRegex().Replace(lastSegment ?? "Page", " $1").Trim();
    }

    private static MenuItem? FindMenuItemByHref(IEnumerable<MenuItem> items, string href)
    {
        MenuItem? bestMatch = null;
        foreach (var item in items)
        {
            if (item.Href != null && href.StartsWith(item.Href, StringComparison.OrdinalIgnoreCase) &&
                (bestMatch == null || item.Href.Length > bestMatch.Href!.Length))
            {
                bestMatch = item;
            }

            if (item.ChildItems is null || item.ChildItems.Count == 0) continue;
            var childMatch = FindMenuItemByHref(item.ChildItems, href);
            if (childMatch != null && (bestMatch == null || childMatch.Href!.Length > bestMatch.Href!.Length))
            {
                bestMatch = childMatch;
            }
        }

        return bestMatch;
    }

    [System.Text.RegularExpressions.GeneratedRegex("([A-Z])")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}

public class MenuBuilder(MenuItem? parent = null)
{
    public List<MenuItem> Items { get; } = [];

    public MenuBuilder AddLink(string title, string href, string? icon = null)
    {
        var linkItem = new MenuItem
        {
            Title = title,
            Href = href,
            Icon = icon,
            Parent = parent
        };
        Items.Add(linkItem);
        return this;
    }

    public MenuBuilder AddGroup(string title, string? icon, Action<MenuBuilder> configure)
    {
        var groupItem = new MenuItem
        {
            Title = title,
            Icon = icon,
            Parent = parent
        };

        var groupBuilder = new MenuBuilder(groupItem);

        configure(groupBuilder);
        groupItem.ChildItems = groupBuilder.Items;
        Items.Add(groupItem);

        return this;
    }
}