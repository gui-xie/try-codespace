using System.Linq;
using Senlinz.Permissions.Generation;
using Xunit;

namespace Senlinz.Permissions.Tests;

public sealed class ParserTests
{
    [Fact]
    public void Parses_minimal_permission_file()
    {
        var result = PermissionJsonParser.Parse(
            """
            {
              "version": 1,
              "permissions": [
                { "code": "users.read" }
              ]
            }
            """);

        Assert.False(result.HasErrors);
        var permission = Assert.Single(result.Document!.Permissions);
        Assert.Equal("users.read", permission.Code);
    }

    [Fact]
    public void Parses_full_permission_file_deterministically()
    {
        var result = PermissionJsonParser.Parse(
            """
            {
              "version": 1,
              "groups": [
                { "code": "users", "name": "Users", "order": 10 },
                { "code": "orders", "name": "Orders", "order": 1 }
              ],
              "permissions": [
                { "code": "users.create", "group": "users", "requires": ["users.read"], "tags": ["backend"], "order": 10 },
                { "code": "users.read", "group": "users", "name": "View users", "description": "Allows viewing users.", "labelKey": "permissions.users.read.label" }
              ]
            }
            """);

        Assert.False(result.HasErrors);
        Assert.Equal(new[] { "orders", "users" }, result.Document!.Groups.Select(group => group.Code));
        Assert.Equal(new[] { "users.create", "users.read" }, result.Document.Permissions.Select(permission => permission.Code));
    }

    [Fact]
    public void Reports_invalid_json()
    {
        var result = PermissionJsonParser.Parse("{", "permission.json");

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "SP001");
    }

    [Fact]
    public void Reports_missing_required_root_properties()
    {
        var result = PermissionJsonParser.Parse("""{ "version": 1 }""");

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "SP002");
    }

    [Fact]
    public void Reports_invalid_permission_code()
    {
        var result = PermissionJsonParser.Parse(
            """
            {
              "version": 1,
              "permissions": [
                { "code": "Users.Read" }
              ]
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "SP003");
    }

    [Fact]
    public void Reports_duplicate_permission_codes()
    {
        var result = PermissionJsonParser.Parse(
            """
            {
              "version": 1,
              "permissions": [
                { "code": "users.read" },
                { "code": "users.read" }
              ]
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "SP004");
    }

    [Fact]
    public void Warns_for_unknown_group()
    {
        var result = PermissionJsonParser.Parse(
            """
            {
              "version": 1,
              "permissions": [
                { "code": "users.read", "group": "users" }
              ]
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "SP006");
    }

    [Fact]
    public void Reports_unknown_required_permission()
    {
        var result = PermissionJsonParser.Parse(
            """
            {
              "version": 1,
              "permissions": [
                { "code": "users.create", "requires": ["users.read"] }
              ]
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "SP011");
    }

    [Fact]
    public void Reports_circular_required_permission()
    {
        var result = PermissionJsonParser.Parse(
            """
            {
              "version": 1,
              "permissions": [
                { "code": "users.read", "requires": ["users.create"] },
                { "code": "users.create", "requires": ["users.read"] }
              ]
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "SP012");
    }

    [Fact]
    public void Reports_identifier_prefix_collision()
    {
        var result = PermissionJsonParser.Parse(
            """
            {
              "version": 1,
              "permissions": [
                { "code": "users.read" },
                { "code": "users.read.detail" }
              ]
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "SP005");
    }
}
