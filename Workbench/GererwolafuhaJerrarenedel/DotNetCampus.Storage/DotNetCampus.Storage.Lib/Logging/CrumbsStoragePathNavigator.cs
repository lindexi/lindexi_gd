using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Storage.Lib.Logging;

/// <summary>
/// 面包屑的存储路径导航
/// </summary>
class CrumbsStoragePathNavigator
{
    private readonly List<string> _crumbs = new List<string>();

    public void Push(string crumb) => _crumbs.Add(crumb);
    public void Pop() => _crumbs.RemoveAt(_crumbs.Count - 1);

    public void Restart(string root)
    {
        _crumbs.Clear();
        _crumbs.Add(root);
    }

    public string GetCurrentPath()
    {
        return string.Join("->", _crumbs);
    }
}